#if NET11_0_OR_GREATER
namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class WhiteSpaceSearchBenchmark
{
    private static readonly SearchValues<char> WhiteSpaceValues = SearchValues.Create(CreateWhiteSpaceSet());

    private string text = default!;

    private string[] fields = default!;

    [GlobalSetup]
    public void Setup()
    {
        var chars = new char[1024];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('a' + (i % 26));
        }

        chars[1000] = ' ';
        text = new string(chars);

        fields = new string[32];
        for (var i = 0; i < fields.Length; i++)
        {
            fields[i] = (i % 4 == 0 ? " " : string.Empty) + "Field" + i.ToString(System.Globalization.CultureInfo.InvariantCulture) + "  ";
        }
    }

    [Benchmark(Baseline = true)]
    public int SearchValuesIndexOf() => text.AsSpan().IndexOfAny(WhiteSpaceValues);

    [Benchmark]
    public int ApiIndexOfAnyWhiteSpace() => text.AsSpan().IndexOfAnyWhiteSpace();

    [Benchmark]
    public int ScalarLeadingTrim()
    {
        var total = 0;
        foreach (var field in fields)
        {
            total += CountLeadingScalar(field.AsSpan());
        }

        return total;
    }

    [Benchmark]
    public int SearchValuesLeadingTrim()
    {
        var total = 0;
        foreach (var field in fields)
        {
            total += CountLeading(field.AsSpan().IndexOfAnyExcept(WhiteSpaceValues), field.Length);
        }

        return total;
    }

    [Benchmark]
    public int ApiLeadingTrim()
    {
        var total = 0;
        foreach (var field in fields)
        {
            total += CountLeading(field.AsSpan().IndexOfAnyExceptWhiteSpace(), field.Length);
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new WhiteSpaceSearchBenchmark();
        benchmark.Setup();
        if ((benchmark.SearchValuesIndexOf() != 1000) ||
            (benchmark.ApiIndexOfAnyWhiteSpace() != 1000) ||
            (benchmark.ScalarLeadingTrim() != 8) ||
            (benchmark.SearchValuesLeadingTrim() != 8) ||
            (benchmark.ApiLeadingTrim() != 8))
        {
            throw new InvalidOperationException("Verify failed. WhiteSpaceSearch");
        }

        foreach (var c in CreateWhiteSpaceSet())
        {
            var probe = "ab" + c;
            if ((probe.AsSpan().IndexOfAnyWhiteSpace() != 2) || (probe.AsSpan().IndexOfAny(WhiteSpaceValues) != 2))
            {
                throw new InvalidOperationException($"Verify failed. WhiteSpaceSearch char=[{(int)c:X4}]");
            }
        }
    }

    private static int CountLeadingScalar(ReadOnlySpan<char> value)
    {
        var i = 0;
        while ((i < value.Length) && char.IsWhiteSpace(value[i]))
        {
            i++;
        }

        return i;
    }

    private static int CountLeading(int index, int length) => index < 0 ? length : index;

    private static char[] CreateWhiteSpaceSet()
    {
        var set = new List<char>();
        for (var c = 0; c <= char.MaxValue; c++)
        {
            if (char.IsWhiteSpace((char)c))
            {
                set.Add((char)c);
            }
        }

        return [.. set];
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class GZipEncoderBenchmark : IDisposable
{
    private const int CompressionLevel = 6;

    private readonly MemoryStream reused = new();

    private byte[] payload = default!;

    [GlobalSetup]
    public void Setup()
    {
        var words = new[] { "alpha", "beta", "gamma", "delta", "epsilon", "zeta", "eta", "theta" };
        var builder = new System.Text.StringBuilder();
        var state = 12345U;
        while (builder.Length < 64 * 1024)
        {
            state = (state * 1664525U) + 1013904223U;
            builder.Append(words[state >> 29]).Append(' ').Append(state % 1000U);
            builder.Append(' ');
        }

        payload = System.Text.Encoding.ASCII.GetBytes(builder.ToString(0, 64 * 1024));
    }

    [GlobalCleanup]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [Benchmark(Baseline = true)]
    public long GZipStreamToNewStream()
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CreateOptions(), leaveOpen: true))
        {
            gzip.Write(payload);
        }

        return output.Length;
    }

    [Benchmark]
    public long GZipStreamToReusedStream()
    {
        reused.SetLength(0);
        using (var gzip = new GZipStream(reused, CreateOptions(), leaveOpen: true))
        {
            gzip.Write(payload);
        }

        return reused.Length;
    }

    [Benchmark]
    public long GZipEncoderToPooledBuffer()
    {
        var buffer = ArrayPool<byte>.Shared.Rent((int)GZipEncoder.GetMaxCompressedLength(payload.Length));
        try
        {
            using var encoder = new GZipEncoder(CreateOptions());
            var status = encoder.Compress(payload, buffer, out _, out var written, isFinalBlock: true);
            return status == OperationStatus.Done ? written : -1;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static void Verify()
    {
        using var benchmark = new GZipEncoderBenchmark();
        benchmark.Setup();
        using var stream = new MemoryStream();
        using (var gzip = new GZipStream(stream, CreateOptions(), leaveOpen: true))
        {
            gzip.Write(benchmark.payload);
        }

        var buffer = new byte[GZipEncoder.GetMaxCompressedLength(benchmark.payload.Length)];
        using var encoder = new GZipEncoder(CreateOptions());
        var status = encoder.Compress(benchmark.payload, buffer, out var consumed, out var written, isFinalBlock: true);
        if ((status != OperationStatus.Done) ||
            (consumed != benchmark.payload.Length) ||
            !Decompress(stream.ToArray()).AsSpan().SequenceEqual(benchmark.payload) ||
            !Decompress(buffer.AsSpan(0, written).ToArray()).AsSpan().SequenceEqual(benchmark.payload) ||
            (benchmark.GZipStreamToNewStream() != benchmark.GZipStreamToReusedStream()) ||
            (benchmark.GZipEncoderToPooledBuffer() <= 0))
        {
            throw new InvalidOperationException("Verify failed. GZipEncoder");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            reused.Dispose();
        }
    }

    private static ZLibCompressionOptions CreateOptions() => new() { CompressionLevel = CompressionLevel };

    private static byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class VectorLaneApiBenchmark
{
    private const int Count = 1024;

    private static readonly Vector128<byte> ReverseMask = Vector128.Create(
        (byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12);

    private uint[] source = default!;

    private uint[] destination = default!;

    private byte[] left = default!;

    private byte[] right = default!;

    private byte[] interleaved = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new uint[Count];
        destination = new uint[Count];
        left = new byte[Count];
        right = new byte[Count];
        interleaved = new byte[Count * 2];
        var state = 12345U;
        for (var i = 0; i < Count; i++)
        {
            state = (state * 1664525U) + 1013904223U;
            source[i] = state;
            left[i] = unchecked((byte)state);
            right[i] = unchecked((byte)(state >> 8));
        }
    }

    [Benchmark(Baseline = true)]
    public uint Ssse3ShuffleReverse()
    {
        ref var srcHead = ref MemoryMarshal.GetArrayDataReference(source);
        ref var dstHead = ref MemoryMarshal.GetArrayDataReference(destination);
        for (nuint i = 0; i < Count; i += (nuint)Vector128<uint>.Count)
        {
            var loaded = Vector128.LoadUnsafe(ref srcHead, i);
            Ssse3.Shuffle(loaded.AsByte(), ReverseMask).AsUInt32().StoreUnsafe(ref dstHead, i);
        }

        return destination[^1];
    }

    [Benchmark]
    public uint Vector128ShuffleReverse()
    {
        ref var srcHead = ref MemoryMarshal.GetArrayDataReference(source);
        ref var dstHead = ref MemoryMarshal.GetArrayDataReference(destination);
        for (nuint i = 0; i < Count; i += (nuint)Vector128<uint>.Count)
        {
            var loaded = Vector128.LoadUnsafe(ref srcHead, i);
            Vector128.Shuffle(loaded.AsByte(), ReverseMask).AsUInt32().StoreUnsafe(ref dstHead, i);
        }

        return destination[^1];
    }

    [Benchmark]
    public uint LaneReverseApi()
    {
        ref var srcHead = ref MemoryMarshal.GetArrayDataReference(source);
        ref var dstHead = ref MemoryMarshal.GetArrayDataReference(destination);
        for (nuint i = 0; i < Count; i += (nuint)Vector128<uint>.Count)
        {
            var loaded = Vector128.LoadUnsafe(ref srcHead, i);
            Vector128.Reverse(Vector128.Reverse(loaded.AsByte()).AsUInt32()).StoreUnsafe(ref dstHead, i);
        }

        return destination[^1];
    }

    [Benchmark]
    public byte Sse2UnpackInterleave()
    {
        ref var leftHead = ref MemoryMarshal.GetArrayDataReference(left);
        ref var rightHead = ref MemoryMarshal.GetArrayDataReference(right);
        ref var dstHead = ref MemoryMarshal.GetArrayDataReference(interleaved);
        for (nuint i = 0; i < Count; i += (nuint)Vector128<byte>.Count)
        {
            var x = Vector128.LoadUnsafe(ref leftHead, i);
            var y = Vector128.LoadUnsafe(ref rightHead, i);
            Sse2.UnpackLow(x, y).StoreUnsafe(ref dstHead, i * 2);
            Sse2.UnpackHigh(x, y).StoreUnsafe(ref dstHead, (i * 2) + (nuint)Vector128<byte>.Count);
        }

        return interleaved[^1];
    }

    [Benchmark]
    public byte ZipInterleave()
    {
        ref var leftHead = ref MemoryMarshal.GetArrayDataReference(left);
        ref var rightHead = ref MemoryMarshal.GetArrayDataReference(right);
        ref var dstHead = ref MemoryMarshal.GetArrayDataReference(interleaved);
        for (nuint i = 0; i < Count; i += (nuint)Vector128<byte>.Count)
        {
            var x = Vector128.LoadUnsafe(ref leftHead, i);
            var y = Vector128.LoadUnsafe(ref rightHead, i);
            Vector128.ZipLower(x, y).StoreUnsafe(ref dstHead, i * 2);
            Vector128.ZipUpper(x, y).StoreUnsafe(ref dstHead, (i * 2) + (nuint)Vector128<byte>.Count);
        }

        return interleaved[^1];
    }

    public static void Verify()
    {
        var benchmark = new VectorLaneApiBenchmark();
        benchmark.Setup();
        var expected = benchmark.source.Select(BinaryPrimitives.ReverseEndianness).ToArray();
        foreach (var reverse in new Func<uint>[] { benchmark.Ssse3ShuffleReverse, benchmark.Vector128ShuffleReverse, benchmark.LaneReverseApi })
        {
            Array.Clear(benchmark.destination);
            _ = reverse();
            if (!benchmark.destination.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidOperationException("Verify failed. VectorLaneApi reverse");
            }
        }

        var expectedInterleaved = new byte[Count * 2];
        for (var i = 0; i < Count; i++)
        {
            expectedInterleaved[i * 2] = benchmark.left[i];
            expectedInterleaved[(i * 2) + 1] = benchmark.right[i];
        }

        foreach (var interleave in new Func<byte>[] { benchmark.Sse2UnpackInterleave, benchmark.ZipInterleave })
        {
            Array.Clear(benchmark.interleaved);
            _ = interleave();
            if (!benchmark.interleaved.AsSpan().SequenceEqual(expectedInterleaved))
            {
                throw new InvalidOperationException("Verify failed. VectorLaneApi interleave");
            }
        }
    }
}
#endif
