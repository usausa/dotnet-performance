namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-09 検証: params T[] と params ReadOnlySpan<T> の呼び出しコスト
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ParamsSpanBenchmark
{
    private string a = default!;

    private string b = default!;

    private string c = default!;

    [GlobalSetup]
    public void Setup()
    {
        a = new string("alpha".AsSpan());
        b = new string("beta".AsSpan());
        c = new string("gamma".AsSpan());
    }

    [Benchmark(Baseline = true)]
    public int ParamsArray() => TotalLengthArray(a, b, c);

    [Benchmark]
    public int ParamsSpan() => TotalLengthSpan(a, b, c);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TotalLengthArray(params string[] values)
    {
        var total = 0;
        foreach (var value in values)
        {
            total += value.Length;
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TotalLengthSpan(params ReadOnlySpan<string> values)
    {
        var total = 0;
        foreach (var value in values)
        {
            total += value.Length;
        }

        return total;
    }
}
