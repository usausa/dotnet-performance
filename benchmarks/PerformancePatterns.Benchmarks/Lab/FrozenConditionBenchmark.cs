namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Frozen;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// COL-02 検証: FrozenDictionary の構築コスト(採用条件の分母)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class FrozenBuildBenchmark
{
    private KeyValuePair<string, int>[] pairs = default!;

    [Params(16, 256)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        pairs = new KeyValuePair<string, int>[Count];
        for (var i = 0; i < Count; i++)
        {
            pairs[i] = new KeyValuePair<string, int>("Key" + i, i);
        }
    }

    [Benchmark(Baseline = true)]
    public int BuildDictionary()
    {
        var dictionary = new Dictionary<string, int>(Count, StringComparer.Ordinal);
        foreach (var pair in pairs)
        {
            dictionary[pair.Key] = pair.Value;
        }

        return dictionary.Count;
    }

    [Benchmark]
    public int BuildFrozen()
    {
        var frozen = pairs.ToFrozenDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal);
        return frozen.Count;
    }
}

// COL-02 検証: 検索コスト(採用条件の分子)— 非インターンキーで全件検索
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class FrozenLookupBenchmark
{
    private Dictionary<string, int> dictionary = default!;

    private FrozenDictionary<string, int> frozen = default!;

    private string[] probes = default!;

    [Params(16, 256)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var pairs = new Dictionary<string, int>(StringComparer.Ordinal);
        probes = new string[Count];
        for (var i = 0; i < Count; i++)
        {
            var key = "Key" + i;
            pairs[key] = i;
            probes[i] = new string(key.AsSpan());   // 非インターンの実行時キー
        }

        dictionary = pairs;
        frozen = pairs.ToFrozenDictionary(StringComparer.Ordinal);
    }

    [Benchmark(Baseline = true)]
    public int LookupDictionary()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            dictionary.TryGetValue(probe, out var value);
            total += value;
        }

        return total;
    }

    [Benchmark]
    public int LookupFrozen()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            frozen.TryGetValue(probe, out var value);
            total += value;
        }

        return total;
    }
}
