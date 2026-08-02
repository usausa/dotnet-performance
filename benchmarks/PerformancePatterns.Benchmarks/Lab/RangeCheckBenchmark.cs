namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// BIT-01 検証: min <= v && v <= max の 2 比較 vs 符号なし 1 比較
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
        // 約半数が範囲内・順序は疑似ランダム(分岐予測ミスの機会を作る)
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
