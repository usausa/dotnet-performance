namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-02 study: GetArrayDataReference — a sequential walk (anti-pattern check) and random access with a guaranteed range
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ArrayDataReferenceBenchmark
{
    private int[] data = default!;

    private int[] indexes = default!;

    [GlobalSetup]
    public void Setup()
    {
        data = new int[1024];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = i;
        }

        // Pseudo-random indices kept in range by construction with a mask (the BIT-02 shape)
        indexes = new int[1024];
        var state = 12345u;
        for (var i = 0; i < indexes.Length; i++)
        {
            state = (state * 1664525u) + 1013904223u;
            indexes[i] = (int)(state & 1023);
        }
    }

    [Benchmark(Baseline = true)]
    public long SequentialFor()
    {
        var total = 0L;
        for (var i = 0; i < data.Length; i++)
        {
            total += data[i];
        }

        return total;
    }

    [Benchmark]
    public long SequentialRefWalk()
    {
        ref var head = ref MemoryMarshal.GetArrayDataReference(data);
        var total = 0L;
        for (var i = 0; i < data.Length; i++)
        {
            total += Unsafe.Add(ref head, i);
        }

        return total;
    }

    [Benchmark]
    public long RandomIndexed()
    {
        var total = 0L;
        foreach (var index in indexes)
        {
            total += data[index];
        }

        return total;
    }

    [Benchmark]
    public long RandomRefAdd()
    {
        ref var head = ref MemoryMarshal.GetArrayDataReference(data);
        var total = 0L;
        foreach (var index in indexes)
        {
            total += Unsafe.Add(ref head, index);
        }

        return total;
    }
}
