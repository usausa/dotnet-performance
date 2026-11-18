namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.ObjectModel;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// R-04 follow-up for .NET 10: the JIT can now devirtualize interface calls on T[] (PR #108153 and
// friends). The .NET 10 performance post reports that as a result `foreach` over a
// ReadOnlyCollection<int> wrapping an array now beats the indexer, reversing the usual advice for
// wrapper collections. Measured here over the same 1024 values.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ReadOnlyCollectionLoopBenchmark
{
    private static readonly ReadOnlyCollection<int> SharedCollection = new(CreateValues());

    private ReadOnlyCollection<int> collection = default!;

    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = CreateValues();
        collection = new ReadOnlyCollection<int>(values);
    }

    [Benchmark(Baseline = true)]
    public long Indexer()
    {
        var total = 0L;
        var source = collection;
        for (var i = 0; i < source.Count; i++)
        {
            total += source[i];
        }

        return total;
    }

    [Benchmark]
    public long Foreach()
    {
        var total = 0L;
        foreach (var value in collection)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long ForeachStaticReadOnly()
    {
        var total = 0L;
        foreach (var value in SharedCollection)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long ForeachLocalWrapper()
    {
        var total = 0L;
        foreach (var value in new ReadOnlyCollection<int>(values))
        {
            total += value;
        }

        return total;
    }

    private static int[] CreateValues()
    {
        var array = new int[1024];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        return array;
    }
}
