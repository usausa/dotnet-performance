namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-05 検証: 型保証済み参照の castclass vs Unsafe.As vs is パターン
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class UnsafeAsCastBenchmark
{
    private object[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        // 型不変条件: 全要素 string(構成的に保証)
        values = new object[1024];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = "v" + (i & 15);
        }
    }

    [Benchmark(Baseline = true)]
    public int CastClass()
    {
        var total = 0;
        foreach (var value in values)
        {
            total += ((string)value).Length;
        }

        return total;
    }

    [Benchmark]
    public int IsPattern()
    {
        var total = 0;
        foreach (var value in values)
        {
            if (value is string text)
            {
                total += text.Length;
            }
        }

        return total;
    }

    [Benchmark]
    public int UnsafeAs()
    {
        var total = 0;
        foreach (var value in values)
        {
            total += Unsafe.As<string>(value).Length;
        }

        return total;
    }
}
