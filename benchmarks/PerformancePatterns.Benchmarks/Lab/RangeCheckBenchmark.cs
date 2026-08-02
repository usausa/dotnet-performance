namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// BIT-01 study: the two comparisons of min <= v && v <= max vs a single unsigned comparison
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RangeCheckBenchmark
{
    private const int Min = 100;

    private const int Max = 900;

    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        // About half the values are in range, in pseudo-random order (to create branch mispredictions)
        values = new int[1024];
        var state = 12345u;
        for (var i = 0; i < values.Length; i++)
        {
            state = (state * 1664525u) + 1013904223u;
            values[i] = (int)(state % 2000) - 250;
        }
    }

    [Benchmark(Baseline = true)]
    public int TwoComparisons()
    {
        var count = 0;
        foreach (var value in values)
        {
            if ((value >= Min) && (value <= Max))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int UnsignedSingleComparison()
    {
        var count = 0;
        foreach (var value in values)
        {
            if ((uint)(value - Min) <= Max - Min)
            {
                count++;
            }
        }

        return count;
    }
}
