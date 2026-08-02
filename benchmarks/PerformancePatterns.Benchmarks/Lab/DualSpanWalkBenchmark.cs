namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-01 study: walking several Spans at once (for a single Span the plain for loop is optimal = R-02)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class DualSpanWalkBenchmark
{
    private int[] first = default!;

    private int[] second = default!;

    [GlobalSetup]
    public void Setup()
    {
        first = new int[1024];
        second = new int[1024];
        for (var i = 0; i < 1024; i++)
        {
            first[i] = i;
            second[i] = i * 3;
        }
    }

    // Naive form: the bounds check on second[i] remains
    [Benchmark(Baseline = true)]
    public long Indexed()
    {
        ReadOnlySpan<int> a = first;
        ReadOnlySpan<int> b = second;
        var total = 0L;
        for (var i = 0; i < a.Length; i++)
        {
            total += a[i] + b[i];
        }

        return total;
    }

    // Slice both up front so the JIT eliminates both bounds checks (the safe alternative)
    [Benchmark]
    public long IndexedPreSliced()
    {
        ReadOnlySpan<int> a = first;
        var b = second.AsSpan(0, a.Length);
        var total = 0L;
        for (var i = 0; i < a.Length; i++)
        {
            total += a[i] + b[i];
        }

        return total;
    }

    // Manual ref walk (the primary MEM-01 form)
    [Benchmark]
    public long RefWalk()
    {
        ReadOnlySpan<int> a = first;
        ReadOnlySpan<int> b = second;
        ref var ra = ref MemoryMarshal.GetReference(a);
        ref var rb = ref MemoryMarshal.GetReference(b);
        var total = 0L;
        for (var i = 0; i < a.Length; i++)
        {
            total += Unsafe.Add(ref ra, i) + Unsafe.Add(ref rb, i);
        }

        return total;
    }
}
