namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 7-2: the positive use of ref fields (C# 11), the shape R-12 points at but never measured.
// R-12 rejected the ref field cursor for whole-element iteration (1.21x slower than Span + for).
// Question: does it win for field-granular structured reads, where every step reads a different width
//           and the index arithmetic cannot be turned into a single counted loop?
// Record layout: [byte tag][ushort length][length bytes payload]
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RefFieldStructReadBenchmark
{
    private const int RecordCount = 512;

    private byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        buffer = BuildBuffer(RecordCount);
    }

    // Index arithmetic written out at the call site: what most parsers actually look like
    [Benchmark(Baseline = true)]
    public long ParseInlineIndex()
    {
        var span = buffer.AsSpan();
        var checksum = 0L;
        var position = 0;
        while (position < span.Length)
        {
            var tag = span[position];
            var length = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(position + 1, 2));
            var payload = span.Slice(position + 3, length);
            checksum += tag + length + payload[0] + payload[^1];
            position += 3 + length;
        }

        return checksum;
    }

    // Cursor type holding "span + position"
    [Benchmark]
    public long ParseSpanReader()
    {
        var reader = new FieldSpanReader(buffer);
        var checksum = 0L;
        while (reader.TryReadHeader(out var tag, out var length))
        {
            var payload = reader.ReadPayload(length);
            checksum += tag + length + payload[0] + payload[^1];
        }

        return checksum;
    }

    // Cursor type that re-slices instead of carrying an index
    [Benchmark]
    public long ParseSliceReader()
    {
        var reader = new FieldSliceReader(buffer);
        var checksum = 0L;
        while (reader.TryReadHeader(out var tag, out var length))
        {
            var payload = reader.ReadPayload(length);
            checksum += tag + length + payload[0] + payload[^1];
        }

        return checksum;
    }

    // Cursor type holding "ref current + ref end" (the R-12 shape)
    [Benchmark]
    public long ParseRefFieldReader()
    {
        var reader = new FieldRefReader(buffer);
        var checksum = 0L;
        while (reader.TryReadHeader(out var tag, out var length))
        {
            var payload = reader.ReadPayload(length);
            checksum += tag + length + payload[0] + payload[^1];
        }

        return checksum;
    }

    public static void Verify()
    {
        var benchmark = new RefFieldStructReadBenchmark();
        benchmark.Setup();

        var expected = benchmark.ParseInlineIndex();
        var results = new[]
        {
            ("SpanReader", benchmark.ParseSpanReader()),
            ("SliceReader", benchmark.ParseSliceReader()),
            ("RefFieldReader", benchmark.ParseRefFieldReader())
        };

        foreach (var (name, actual) in results)
        {
            if (actual != expected)
            {
                throw new InvalidOperationException($"Verify failed. RefFieldStructRead. name=[{name}] expected=[{expected}] actual=[{actual}]");
            }
        }
    }

    private static byte[] BuildBuffer(int recordCount)
    {
        var size = 0;
        for (var i = 0; i < recordCount; i++)
        {
            size += 3 + PayloadLength(i);
        }

        var bytes = new byte[size];
        var span = bytes.AsSpan();
        var position = 0;
        for (var i = 0; i < recordCount; i++)
        {
            var length = PayloadLength(i);
            span[position] = (byte)(i & 0x7F);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(position + 1, 2), (ushort)length);
            for (var j = 0; j < length; j++)
            {
                span[position + 3 + j] = (byte)((i + j) & 0xFF);
            }

            position += 3 + length;
        }

        return bytes;
    }

    private static int PayloadLength(int index) => 1 + (index % 16);
}

// Cursor built on "span + position"
internal ref struct FieldSpanReader
{
    private readonly ReadOnlySpan<byte> source;

    private int position;

    public FieldSpanReader(ReadOnlySpan<byte> source)
    {
        this.source = source;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadHeader(out byte tag, out int length)
    {
        if (position >= source.Length)
        {
            tag = 0;
            length = 0;
            return false;
        }

        tag = source[position];
        length = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(position + 1, 2));
        position += 3;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadPayload(int length)
    {
        var payload = source.Slice(position, length);
        position += length;
        return payload;
    }
}

// Cursor that re-slices the remaining span instead of carrying an index
internal ref struct FieldSliceReader
{
    private ReadOnlySpan<byte> remaining;

    public FieldSliceReader(ReadOnlySpan<byte> source)
    {
        remaining = source;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadHeader(out byte tag, out int length)
    {
        if (remaining.IsEmpty)
        {
            tag = 0;
            length = 0;
            return false;
        }

        tag = remaining[0];
        length = BinaryPrimitives.ReadUInt16LittleEndian(remaining.Slice(1, 2));
        remaining = remaining[3..];
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadPayload(int length)
    {
        var payload = remaining[..length];
        remaining = remaining[length..];
        return payload;
    }
}

// Cursor built on ref fields (C# 11): the position is the ref itself.
// Note: the unaligned reads below are little-endian by machine order, matching BinaryPrimitives on x64.
internal ref struct FieldRefReader
{
    private readonly ref byte end;

    private ref byte current;

    public FieldRefReader(ReadOnlySpan<byte> source)
    {
        current = ref MemoryMarshal.GetReference(source);
        end = ref Unsafe.Add(ref current, source.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadHeader(out byte tag, out int length)
    {
        if (!Unsafe.IsAddressLessThan(ref current, ref end))
        {
            tag = 0;
            length = 0;
            return false;
        }

        tag = current;
        length = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref current, 1));
        current = ref Unsafe.Add(ref current, 3);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadPayload(int length)
    {
        var payload = MemoryMarshal.CreateReadOnlySpan(ref current, length);
        current = ref Unsafe.Add(ref current, length);
        return payload;
    }
}
