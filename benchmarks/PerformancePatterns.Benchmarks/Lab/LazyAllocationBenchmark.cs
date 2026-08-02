namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-07 study: allocating the error list up front vs lazily (10% failure rate)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class LazyAllocationBenchmark
{
    private int[] items = default!;

    [GlobalSetup]
    public void Setup()
    {
        // One in ten items is a "failure" (a negative value)
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

    // The all-success case (with lazy allocation this allocates nothing)
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

// STK-07 study: a shared singleton for empty results
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

    // Use a non-constant length to shield the "new int[0] every time" case under measurement from the analyzer (deliberately the bad form)
    private static readonly int ZeroLength = string.Empty.Length;

    private static int[] CreateNew() => new int[ZeroLength];

    private static int[] CreateShared() => [];
}
