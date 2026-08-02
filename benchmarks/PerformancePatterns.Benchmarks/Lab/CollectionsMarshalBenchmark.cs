namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 2: measuring COL-01 (CollectionsMarshal) on this machine
// Question: record on net10 the effect of iterating a List through AsSpan and of doing dictionary read-modify-write by ref.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ListIterationBenchmark
{
    private List<int> list = default!;

    [GlobalSetup]
    public void Setup()
    {
        list = Enumerable.Range(0, 1024).ToList();
    }

    [Benchmark(Baseline = true)]
    public int ForEachList()
    {
        var total = 0;
        foreach (var value in list)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public int ForList()
    {
        var total = 0;
        for (var i = 0; i < list.Count; i++)
        {
            total += list[i];
        }

        return total;
    }

    [Benchmark]
    public int AsSpanFor()
    {
        var span = CollectionsMarshal.AsSpan(list);
        var total = 0;
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public int AsSpanForEach()
    {
        var total = 0;
        foreach (var value in CollectionsMarshal.AsSpan(list))
        {
            total += value;
        }

        return total;
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class DictionaryCountBenchmark
{
    private string[] keys = default!;

    [GlobalSetup]
    public void Setup()
    {
        // Concatenated at run time, so not interned. 256 distinct keys, each appearing 4 times
        keys = new string[1024];
        for (var i = 0; i < keys.Length; i++)
        {
            keys[i] = "key" + (i % 256);
        }
    }

    [Benchmark(Baseline = true)]
    public int DoubleLookup()
    {
        var map = new Dictionary<string, int>();
        foreach (var key in keys)
        {
            map.TryGetValue(key, out var count);
            map[key] = count + 1;
        }

        return map.Count;
    }

    [Benchmark]
    public int RefLookup()
    {
        var map = new Dictionary<string, int>();
        foreach (var key in keys)
        {
            ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out _);
            count++;
        }

        return map.Count;
    }
}
