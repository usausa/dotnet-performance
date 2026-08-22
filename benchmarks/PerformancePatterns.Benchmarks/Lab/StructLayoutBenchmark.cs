namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 7-4: struct layout itself (field order, padding, LayoutKind).
// MEM-02 covers "array of structs + ref access" and MEM-04 covers "pass by in above 16 bytes", but nothing
// in the catalog covers making the struct smaller in the first place, which is upstream of both.
// The three types below hold the same four fields; only the declared order and LayoutKind differ.
//   Padded    : long, int, long, int in that order under Sequential -> 32 bytes (8 bytes of padding)
//   Packed    : the same fields reordered wide-first -> 24 bytes
//   Auto      : the padded declaration order, but the runtime is allowed to reorder -> expected 24 bytes
// Question A: does the 25% smaller footprint show up in traversal, and does it need a random access shape to show?
// Question B: does it show up at the call boundary (MEM-04 measures size, this measures the same size reached
//             by layout rather than by removing fields)?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StructLayoutBenchmark
{
    private const int Count = 16384;

    private const int CallCount = 64;

    private LayoutPadded[] padded = default!;

    private LayoutPacked[] packed = default!;

    private LayoutAuto[] auto = default!;

    private int[] order = default!;

    [GlobalSetup]
    public void Setup()
    {
        padded = new LayoutPadded[Count];
        packed = new LayoutPacked[Count];
        auto = new LayoutAuto[Count];
        for (var i = 0; i < Count; i++)
        {
            padded[i] = new LayoutPadded { Id = i, Kind = i & 7, Value = i * 2, Flags = i & 3 };
            packed[i] = new LayoutPacked { Id = i, Kind = i & 7, Value = i * 2, Flags = i & 3 };
            auto[i] = new LayoutAuto { Id = i, Kind = i & 7, Value = i * 2, Flags = i & 3 };
        }

        // Fixed pseudo-random visiting order so the traversal cannot be served purely by the prefetcher
        order = new int[Count];
        var state = 12345U;
        for (var i = 0; i < Count; i++)
        {
            state = (state * 1664525U) + 1013904223U;
            order[i] = (int)(state % Count);
        }
    }

    // --- Question A: sequential traversal ---

    [Benchmark(Baseline = true)]
    public long SequentialPadded()
    {
        var items = padded;
        var total = 0L;
        for (var i = 0; i < items.Length; i++)
        {
            ref var item = ref items[i];
            total += item.Id + item.Kind + item.Value + item.Flags;
        }

        return total;
    }

    [Benchmark]
    public long SequentialPacked()
    {
        var items = packed;
        var total = 0L;
        for (var i = 0; i < items.Length; i++)
        {
            ref var item = ref items[i];
            total += item.Id + item.Kind + item.Value + item.Flags;
        }

        return total;
    }

    [Benchmark]
    public long SequentialAuto()
    {
        var items = auto;
        var total = 0L;
        for (var i = 0; i < items.Length; i++)
        {
            ref var item = ref items[i];
            total += item.Id + item.Kind + item.Value + item.Flags;
        }

        return total;
    }

    // --- Question A: scattered traversal, where the smaller footprint should matter more ---

    [Benchmark]
    public long ScatteredPadded()
    {
        var items = padded;
        var indexes = order;
        var total = 0L;
        for (var i = 0; i < indexes.Length; i++)
        {
            ref var item = ref items[indexes[i]];
            total += item.Id + item.Kind + item.Value + item.Flags;
        }

        return total;
    }

    [Benchmark]
    public long ScatteredPacked()
    {
        var items = packed;
        var indexes = order;
        var total = 0L;
        for (var i = 0; i < indexes.Length; i++)
        {
            ref var item = ref items[indexes[i]];
            total += item.Id + item.Kind + item.Value + item.Flags;
        }

        return total;
    }

    [Benchmark]
    public long ScatteredAuto()
    {
        var items = auto;
        var indexes = order;
        var total = 0L;
        for (var i = 0; i < indexes.Length; i++)
        {
            ref var item = ref items[indexes[i]];
            total += item.Id + item.Kind + item.Value + item.Flags;
        }

        return total;
    }

    // --- Question B: the same size difference at the call boundary ---

    [Benchmark(OperationsPerInvoke = CallCount)]
    public long ByValuePadded()
    {
        var value = padded[0];
        var total = 0L;
        for (var i = 0; i < CallCount; i++)
        {
            total += ConsumePadded(value);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = CallCount)]
    public long ByValuePacked()
    {
        var value = packed[0];
        var total = 0L;
        for (var i = 0; i < CallCount; i++)
        {
            total += ConsumePacked(value);
        }

        return total;
    }

    public static void Verify()
    {
        var paddedSize = Unsafe.SizeOf<LayoutPadded>();
        var packedSize = Unsafe.SizeOf<LayoutPacked>();
        var autoSize = Unsafe.SizeOf<LayoutAuto>();
        if ((paddedSize != 32) || (packedSize != 24) || (autoSize != packedSize))
        {
            throw new InvalidOperationException($"Verify failed. StructLayout sizes. padded=[{paddedSize}] packed=[{packedSize}] auto=[{autoSize}]");
        }

        var benchmark = new StructLayoutBenchmark();
        benchmark.Setup();

        var expected = benchmark.SequentialPadded();
        if ((benchmark.SequentialPacked() != expected) || (benchmark.SequentialAuto() != expected))
        {
            throw new InvalidOperationException("Verify failed. StructLayout sequential.");
        }

        var scattered = benchmark.ScatteredPadded();
        if ((benchmark.ScatteredPacked() != scattered) || (benchmark.ScatteredAuto() != scattered))
        {
            throw new InvalidOperationException("Verify failed. StructLayout scattered.");
        }

        if (benchmark.ByValuePadded() != benchmark.ByValuePacked())
        {
            throw new InvalidOperationException("Verify failed. StructLayout by value.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ConsumePadded(LayoutPadded value) => value.Id + value.Kind + value.Value + value.Flags;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ConsumePacked(LayoutPacked value) => value.Id + value.Kind + value.Value + value.Flags;
}

// Declaration order alternates wide and narrow, so Sequential has to pad: 8 + (4 + 4 pad) + 8 + (4 + 4 pad) = 32
[StructLayout(LayoutKind.Sequential)]
internal struct LayoutPadded
{
    public long Id;

    public int Kind;

    public long Value;

    public int Flags;
}

// The same fields, wide first: 8 + 8 + 4 + 4 = 24
[StructLayout(LayoutKind.Sequential)]
internal struct LayoutPacked
{
    public long Id;

    public long Value;

    public int Kind;

    public int Flags;
}

// The padded declaration order, but the runtime is free to reorder
[StructLayout(LayoutKind.Auto)]
internal struct LayoutAuto
{
    public long Id;

    public int Kind;

    public long Value;

    public int Flags;
}
