namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 2: CollectionsMarshal.SetCount (.NET 8+)
// Question: is it worth replacing an Add loop (N capacity checks plus version bumps) with
// SetCount plus direct Span writes? Also check how it combines with a preset capacity.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ListSetCountBenchmark
{
    [Params(16, 1024)]
    public int Size { get; set; }

    [Benchmark(Baseline = true)]
    public int AddLoop()
    {
        var list = new List<int>();
        for (var i = 0; i < Size; i++)
        {
            list.Add(i);
        }

        return list.Count;
    }

    [Benchmark]
    public int AddLoopCapacity()
    {
        var list = new List<int>(Size);
        for (var i = 0; i < Size; i++)
        {
            list.Add(i);
        }

        return list.Count;
    }

    [Benchmark]
    public int SetCountSpanWrite()
    {
        var list = new List<int>();
        CollectionsMarshal.SetCount(list, Size);
        var span = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = i;
        }

        return list.Count;
    }

    [Benchmark]
    public int SetCountCapacitySpanWrite()
    {
        var list = new List<int>(Size);
        CollectionsMarshal.SetCount(list, Size);
        var span = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = i;
        }

        return list.Count;
    }
}
