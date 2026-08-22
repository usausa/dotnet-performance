namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 7-5: false sharing and cache line padding.
// The CON family covers Interlocked (CON-01) and reference counting (CON-02) but has nothing about the layout
// of the counters themselves. Per-worker statistics counters living in one shared array is the classic shape.
// Question A: how large is the penalty when N workers write to adjacent slots of a long[]?
// Question B: is 64 bytes of padding enough, or is 128 needed (the BCL's own PaddingHelpers uses 128 because of
//             adjacent cache line prefetching)?
// Question C: does the answer change when the writes are already interlocked (does the lock prefix mask it)?
// Note: time scales with Workers, so ratios are only meaningful inside one Workers value.
//       A strided long[] (stride 8) is the allocation-shaped equivalent of Padded64 and is expected to match it.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class FalseSharingBenchmark
{
    private const int Iterations = 50000;

    private long[] adjacent = default!;

    private Padded64[] padded64 = default!;

    private Padded128[] padded128 = default!;

    private Action<int> adjacentVolatileWorker = default!;

    private Action<int> padded64VolatileWorker = default!;

    private Action<int> padded128VolatileWorker = default!;

    private Action<int> adjacentInterlockedWorker = default!;

    private Action<int> padded128InterlockedWorker = default!;

    [Params(2, 4, 8)]
    public int Workers { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        adjacent = new long[Workers];
        padded64 = new Padded64[Workers];
        padded128 = new Padded128[Workers];

        var adjacentLocal = adjacent;
        var padded64Local = padded64;
        var padded128Local = padded128;

        // Delegates are built once so that no per invocation allocation is charged to any single variant
        adjacentVolatileWorker = index =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                Volatile.Write(ref adjacentLocal[index], Volatile.Read(ref adjacentLocal[index]) + 1);
            }
        };
        padded64VolatileWorker = index =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                Volatile.Write(ref padded64Local[index].Value, Volatile.Read(ref padded64Local[index].Value) + 1);
            }
        };
        padded128VolatileWorker = index =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                Volatile.Write(ref padded128Local[index].Value, Volatile.Read(ref padded128Local[index].Value) + 1);
            }
        };
        adjacentInterlockedWorker = index =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                Interlocked.Increment(ref adjacentLocal[index]);
            }
        };
        padded128InterlockedWorker = index =>
        {
            for (var i = 0; i < Iterations; i++)
            {
                Interlocked.Increment(ref padded128Local[index].Value);
            }
        };
    }

    // --- Question A / B: plain (non atomic) per worker counters ---

    [Benchmark(Baseline = true)]
    public long AdjacentVolatile()
    {
        Parallel.For(0, Workers, adjacentVolatileWorker);
        return Volatile.Read(ref adjacent[0]);
    }

    [Benchmark]
    public long Padded64Volatile()
    {
        Parallel.For(0, Workers, padded64VolatileWorker);
        return Volatile.Read(ref padded64[0].Value);
    }

    [Benchmark]
    public long Padded128Volatile()
    {
        Parallel.For(0, Workers, padded128VolatileWorker);
        return Volatile.Read(ref padded128[0].Value);
    }

    // --- Question C: the same layouts under interlocked writes ---

    [Benchmark]
    public long AdjacentInterlocked()
    {
        Parallel.For(0, Workers, adjacentInterlockedWorker);
        return Volatile.Read(ref adjacent[0]);
    }

    [Benchmark]
    public long Padded128Interlocked()
    {
        Parallel.For(0, Workers, padded128InterlockedWorker);
        return Volatile.Read(ref padded128[0].Value);
    }

    public static void Verify()
    {
        if (Unsafe.SizeOf<Padded64>() != 64)
        {
            throw new InvalidOperationException($"Verify failed. FalseSharing Padded64 size=[{Unsafe.SizeOf<Padded64>()}]");
        }

        if (Unsafe.SizeOf<Padded128>() != 128)
        {
            throw new InvalidOperationException($"Verify failed. FalseSharing Padded128 size=[{Unsafe.SizeOf<Padded128>()}]");
        }

        var benchmark = new FalseSharingBenchmark { Workers = 4 };
        benchmark.Setup();
        benchmark.AdjacentVolatile();
        benchmark.Padded64Volatile();
        benchmark.Padded128Volatile();
        benchmark.AdjacentInterlocked();
        benchmark.Padded128Interlocked();

        // Every worker owns exactly one slot, so no update may be lost
        for (var i = 0; i < 4; i++)
        {
            if ((benchmark.adjacent[i] != (Iterations * 2)) ||
                (benchmark.padded64[i].Value != Iterations) ||
                (benchmark.padded128[i].Value != (Iterations * 2)))
            {
                throw new InvalidOperationException($"Verify failed. FalseSharing counters. index=[{i}]");
            }
        }
    }
}

[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct Padded64
{
    [FieldOffset(0)]
    public long Value;
}

// 128 bytes because adjacent line prefetching pulls cache line pairs; this is what the BCL pads to
[StructLayout(LayoutKind.Explicit, Size = 128)]
internal struct Padded128
{
    [FieldOffset(0)]
    public long Value;
}
