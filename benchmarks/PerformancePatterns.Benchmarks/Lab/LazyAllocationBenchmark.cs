namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-07 検証: エラーリストの先行確保 vs 遅延確保(失敗率 10%)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class LazyAllocationBenchmark
{
    private int[] items = default!;

    [GlobalSetup]
    public void Setup()
    {
        // 10 件に 1 件が「失敗」(負値)
        items = new int[100];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = (i % 10) == 9 ? -i : i;
        }
    }

    [Benchmark(Baseline = true)]
    public int EagerList()
    {
        var errors = new List<int>();
        foreach (var item in items)
        {
            if (item < 0)
            {
                errors.Add(item);
            }
        }

        return errors.Count;
    }

    [Benchmark]
    public int LazyList()
    {
        List<int>? errors = null;
        foreach (var item in items)
        {
            if (item < 0)
            {
                (errors ??= []).Add(item);
            }
        }

        return errors?.Count ?? 0;
    }

    // 全件成功のケース(遅延確保なら割り当てゼロになる)
    [Benchmark]
    public int LazyListAllValid()
    {
        List<int>? errors = null;
        foreach (var item in items)
        {
            if (item < -1000)
            {
                (errors ??= []).Add(item);
            }
        }

        return errors?.Count ?? 0;
    }
}

// STK-07 検証: 空結果の共有シングルトン
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SharedEmptyBenchmark
{
    private const int N = 100;

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int NewEmptyArray()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += CreateNew().Length;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int SharedEmptyArray()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += CreateShared().Length;
        }

        return total;
    }

    // 長さを非定数にして「毎回 new int[0]」の測定対象をアナライザから守る(意図的な悪い形)
    private static readonly int ZeroLength = string.Empty.Length;

    private static int[] CreateNew() => new int[ZeroLength];

    private static int[] CreateShared() => [];
}
