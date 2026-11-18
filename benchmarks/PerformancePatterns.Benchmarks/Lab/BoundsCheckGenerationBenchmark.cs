namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class SampledHashIndexBenchmark
{
    private string[] names = default!;

    [GlobalSetup]
    public void Setup()
    {
        names = new string[64];
        for (var i = 0; i < names.Length; i++)
        {
            names[i] = new string((char)('a' + (i % 26)), 1 + (i % 17)) + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    [Benchmark(Baseline = true)]
    public int ManualRef()
    {
        var total = 0;
        foreach (var name in names)
        {
            total += HashManualRef(name.AsSpan());
        }

        return total;
    }

    [Benchmark]
    public int Indexed()
    {
        var total = 0;
        foreach (var name in names)
        {
            total += HashIndexed(name.AsSpan());
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new SampledHashIndexBenchmark();
        benchmark.Setup();
        if ((benchmark.ManualRef() != benchmark.Indexed()) || (HashIndexed(default) != HashManualRef(default)))
        {
            throw new InvalidOperationException("Verify failed. SampledHashIndex");
        }

        foreach (var name in benchmark.names)
        {
            if (HashIndexed(name.AsSpan()) != PerformancePatterns.Col.SampledNameTable<int>.CalculateHash(name.AsSpan()))
            {
                throw new InvalidOperationException($"Verify failed. SampledHashIndex name=[{name}]");
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int HashManualRef(ReadOnlySpan<char> value)
    {
        var length = value.Length;
        if (length == 0)
        {
            return 0;
        }

        ref var head = ref MemoryMarshal.GetReference(value);
        var first = Unsafe.Add(ref head, 0);
        var middle = Unsafe.Add(ref head, length >> 1);
        var last = Unsafe.Add(ref head, length - 1);
        return (length << 16) ^ (first << 8) ^ (middle << 4) ^ last;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int HashIndexed(ReadOnlySpan<char> value)
    {
        var length = value.Length;
        if (length == 0)
        {
            return 0;
        }

        return (length << 16) ^ (value[0] << 8) ^ (value[length >> 1] << 4) ^ value[length - 1];
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class SliceTailReadBenchmark
{
    private byte[] buffer = default!;

    private int[] lengths = default!;

    [GlobalSetup]
    public void Setup()
    {
        buffer = new byte[256];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = unchecked((byte)((i * 31) + 7));
        }

        lengths = new int[64];
        for (var i = 0; i < lengths.Length; i++)
        {
            lengths[i] = 4 + ((i * 37) % 250);
        }
    }

    [Benchmark(Baseline = true)]
    public uint SliceTail()
    {
        var total = 0U;
        foreach (var length in lengths)
        {
            total += ReadTailSlice(buffer.AsSpan(0, length));
        }

        return total;
    }

    [Benchmark]
    public uint RefTail()
    {
        var total = 0U;
        foreach (var length in lengths)
        {
            total += ReadTailRef(buffer.AsSpan(0, length));
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new SliceTailReadBenchmark();
        benchmark.Setup();
        if ((benchmark.SliceTail() != benchmark.RefTail()) || (ReadTailSlice([1, 2, 3]) != 0) || (ReadTailRef([1, 2, 3]) != 0))
        {
            throw new InvalidOperationException("Verify failed. SliceTailRead");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ReadTailSlice(ReadOnlySpan<byte> data)
    {
        if (data.Length < sizeof(uint))
        {
            return 0;
        }

        return BinaryPrimitives.ReadUInt32LittleEndian(data[^sizeof(uint)..]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint ReadTailRef(ReadOnlySpan<byte> data)
    {
        if (data.Length < sizeof(uint))
        {
            return 0;
        }

        var value = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(data), data.Length - sizeof(uint)));
        return BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class SliceAdvanceLoopBenchmark
{
    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[1027];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (i * 7) - 300;
        }
    }

    [Benchmark(Baseline = true)]
    public int IndexScalar()
    {
        ReadOnlySpan<int> span = values;
        var total = 0;
        var i = 0;
        for (; i <= span.Length - 4; i += 4)
        {
            total += span[i] + span[i + 1] + span[i + 2] + span[i + 3];
        }

        for (; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public int SliceAdvanceScalar()
    {
        ReadOnlySpan<int> data = values;
        var total = 0;
        while (data.Length >= 4)
        {
            total += data[0] + data[1] + data[2] + data[3];
            data = data[4..];
        }

        foreach (var value in data)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public int IndexVector()
    {
        ReadOnlySpan<int> span = values;
        var accumulator = Vector128<int>.Zero;
        var i = 0;
        for (; i <= span.Length - Vector128<int>.Count; i += Vector128<int>.Count)
        {
            accumulator += Vector128.Create(span.Slice(i, Vector128<int>.Count));
        }

        var total = Vector128.Sum(accumulator);
        for (; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public int SliceAdvanceVector()
    {
        ReadOnlySpan<int> data = values;
        var accumulator = Vector128<int>.Zero;
        while (data.Length >= Vector128<int>.Count)
        {
            accumulator += Vector128.Create(data);
            data = data[Vector128<int>.Count..];
        }

        var total = Vector128.Sum(accumulator);
        foreach (var value in data)
        {
            total += value;
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new SliceAdvanceLoopBenchmark();
        benchmark.Setup();
        var expected = benchmark.values.Sum();
        if ((benchmark.IndexScalar() != expected) ||
            (benchmark.SliceAdvanceScalar() != expected) ||
            (benchmark.IndexVector() != expected) ||
            (benchmark.SliceAdvanceVector() != expected))
        {
            throw new InvalidOperationException("Verify failed. SliceAdvanceLoop");
        }
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class BitDerivedIndexBenchmark
{
    private uint[] values = default!;

    private ushort[] pairs = default!;

    private static ReadOnlySpan<byte> Log2Table =>
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32,
    ];

    private static ReadOnlySpan<byte> CountTable =>
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
        17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33,
    ];

    private static ReadOnlySpan<byte> SixBitTable =>
    [
        65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80,
        81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 97, 98, 99, 100, 101, 102,
        103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118,
        119, 120, 121, 122, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 43, 47,
    ];

    [GlobalSetup]
    public void Setup()
    {
        values = new uint[1024];
        pairs = new ushort[1024];
        var state = 2463534242U;
        for (var i = 0; i < values.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            values[i] = state >> (i % 32);
        }

        for (var i = 0; i < pairs.Length; i++)
        {
            pairs[i] = unchecked((ushort)(values[i] >> 7));
        }
    }

    [Benchmark(Baseline = true)]
    public int Log2Unsafe()
    {
        var total = 0;
        ref var table = ref MemoryMarshal.GetReference(Log2Table);
        foreach (var value in values)
        {
            total += Unsafe.Add(ref table, BitOperations.Log2(value));
        }

        return total;
    }

    [Benchmark]
    public int Log2Indexed()
    {
        var total = 0;
        var table = Log2Table;
        foreach (var value in values)
        {
            total += table[BitOperations.Log2(value)];
        }

        return total;
    }

    [Benchmark]
    public int LeadingZeroCountIndexed()
    {
        var total = 0;
        var table = CountTable;
        foreach (var value in values)
        {
            total += table[BitOperations.LeadingZeroCount(value)];
        }

        return total;
    }

    [Benchmark]
    public int PopCountIndexed()
    {
        var total = 0;
        var table = CountTable;
        foreach (var value in values)
        {
            total += table[BitOperations.PopCount(value)];
        }

        return total;
    }

    [Benchmark]
    public int OrComposedIndexed()
    {
        var total = 0;
        var table = SixBitTable;
        foreach (var pair in pairs)
        {
            total += table[(((pair >> 8) & 0x3) << 4) | ((pair & 0xFF) >> 4)];
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new BitDerivedIndexBenchmark();
        benchmark.Setup();
        var log2 = 0;
        var lzcnt = 0;
        var popcnt = 0;
        foreach (var value in benchmark.values)
        {
            log2 += BitOperations.Log2(value) + 1;
            lzcnt += BitOperations.LeadingZeroCount(value) + 1;
            popcnt += BitOperations.PopCount(value) + 1;
        }

        var sixBit = 0;
        foreach (var pair in benchmark.pairs)
        {
            sixBit += SixBitTable[(((pair >> 8) & 0x3) * 16) + ((pair & 0xFF) / 16)];
        }

        if ((benchmark.Log2Unsafe() != log2) ||
            (benchmark.Log2Indexed() != log2) ||
            (benchmark.LeadingZeroCountIndexed() != lzcnt) ||
            (benchmark.PopCountIndexed() != popcnt) ||
            (benchmark.OrComposedIndexed() != sixBit))
        {
            throw new InvalidOperationException("Verify failed. BitDerivedIndex");
        }
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class UnsignedGuardOffsetBenchmark
{
    private int[] values = default!;

    private int[] indexes = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[512];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (i * i) % 1009;
        }

        indexes = new int[1024];
        var state = 12345U;
        for (var i = 0; i < indexes.Length; i++)
        {
            state = (state * 1664525U) + 1013904223U;
            indexes[i] = (int)(state % 600U) - 40;
        }
    }

    [Benchmark(Baseline = true)]
    public int SignedGuard()
    {
        ReadOnlySpan<int> span = values;
        var total = 0;
        foreach (var i in indexes)
        {
            if ((i > 0) && (i < span.Length))
            {
                total += span[i] - span[i - 1];
            }
        }

        return total;
    }

    [Benchmark]
    public int UnsignedGuard()
    {
        ReadOnlySpan<int> span = values;
        var total = 0;
        foreach (var i in indexes)
        {
            if (((uint)i < (uint)span.Length) && (i != 0))
            {
                total += span[i] - span[i - 1];
            }
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new UnsignedGuardOffsetBenchmark();
        benchmark.Setup();
        if (benchmark.SignedGuard() != benchmark.UnsignedGuard())
        {
            throw new InvalidOperationException("Verify failed. UnsignedGuardOffset");
        }
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class CheckedAfterGuardBenchmark
{
    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[1024];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (i * 97) % 300;
        }
    }

    [Benchmark(Baseline = true)]
    public long UncheckedMultiply()
    {
        var total = 0L;
        foreach (var value in values)
        {
            if ((uint)value <= 100_000U)
            {
                total += value * 10;
            }
        }

        return total;
    }

    [Benchmark]
    public long CheckedMultiply()
    {
        var total = 0L;
        foreach (var value in values)
        {
            if ((uint)value <= 100_000U)
            {
                total += checked(value * 10);
            }
        }

        return total;
    }

    [Benchmark]
    public long UncheckedNarrow()
    {
        var total = 0L;
        foreach (var value in values)
        {
            if ((uint)value <= byte.MaxValue)
            {
                total += unchecked((byte)value);
            }
        }

        return total;
    }

    [Benchmark]
    public long CheckedNarrow()
    {
        var total = 0L;
        foreach (var value in values)
        {
            if ((uint)value <= byte.MaxValue)
            {
                total += checked((byte)value);
            }
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new CheckedAfterGuardBenchmark();
        benchmark.Setup();
        if ((benchmark.UncheckedMultiply() != benchmark.CheckedMultiply()) || (benchmark.UncheckedNarrow() != benchmark.CheckedNarrow()))
        {
            throw new InvalidOperationException("Verify failed. CheckedAfterGuard");
        }
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class FixedLengthParseBenchmark
{
    private string[] inputs = default!;

    [GlobalSetup]
    public void Setup()
    {
        inputs = new string[64];
        for (var i = 0; i < inputs.Length; i++)
        {
            inputs[i] = (20000101 + (i * 10101)).ToString(System.Globalization.CultureInfo.InvariantCulture) + (i % 3 == 0 ? "Z" : string.Empty);
        }
    }

    [Benchmark(Baseline = true)]
    public int LengthGuard()
    {
        var total = 0;
        foreach (var input in inputs)
        {
            total += ParseWithLengthGuard(input.AsSpan());
        }

        return total;
    }

    [Benchmark]
    public int ExactSlice()
    {
        var total = 0;
        foreach (var input in inputs)
        {
            total += ParseWithExactSlice(input.AsSpan());
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new FixedLengthParseBenchmark();
        benchmark.Setup();
        if ((benchmark.LengthGuard() != benchmark.ExactSlice()) ||
            (ParseWithLengthGuard("2026111") != -1) ||
            (ParseWithExactSlice("2026111") != -1) ||
            (ParseWithExactSlice("20261118") != 20261118))
        {
            throw new InvalidOperationException("Verify failed. FixedLengthParse");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ParseWithLengthGuard(ReadOnlySpan<char> text)
    {
        if (text.Length < 8)
        {
            return -1;
        }

        return ((text[0] - '0') * 10_000_000) + ((text[1] - '0') * 1_000_000) + ((text[2] - '0') * 100_000) + ((text[3] - '0') * 10_000) +
            ((text[4] - '0') * 1_000) + ((text[5] - '0') * 100) + ((text[6] - '0') * 10) + (text[7] - '0');
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ParseWithExactSlice(ReadOnlySpan<char> text)
    {
        if (text.Length < 8)
        {
            return -1;
        }

        text = text[..8];
        return ((text[0] - '0') * 10_000_000) + ((text[1] - '0') * 1_000_000) + ((text[2] - '0') * 100_000) + ((text[3] - '0') * 10_000) +
            ((text[4] - '0') * 1_000) + ((text[5] - '0') * 100) + ((text[6] - '0') * 10) + (text[7] - '0');
    }
}
