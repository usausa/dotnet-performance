namespace PerformancePatterns.Benchmarks.Lab;

using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// BUF-07 検証: StringBuilder を毎回確保 vs [ThreadStatic] 1 要素プール
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ObjectPoolBenchmark
{
    private const int Capacity = 256;

    [ThreadStatic]
    private static StringBuilder? cachedBuilder;

    private string name = default!;

    private int id;

    [GlobalSetup]
    public void Setup()
    {
        name = new string("customer".AsSpan());
        id = 12345;
    }

    [Benchmark(Baseline = true)]
    public string NewEveryTime()
    {
        var builder = new StringBuilder(Capacity);
        builder.Append("key:").Append(name).Append(':').Append(id);
        return builder.ToString();
    }

    [Benchmark]
    public string ThreadStaticPool()
    {
        var builder = Rent();
        builder.Append("key:").Append(name).Append(':').Append(id);
        var result = builder.ToString();
        Return(builder);
        return result;
    }

    private static StringBuilder Rent()
    {
        var builder = cachedBuilder;
        if (builder is null)
        {
            return new StringBuilder(Capacity);
        }

        cachedBuilder = null;
        return builder;
    }

    private static void Return(StringBuilder builder)
    {
        if (builder.Capacity <= Capacity * 4)
        {
            builder.Clear();
            cachedBuilder = builder;
        }
    }
}
