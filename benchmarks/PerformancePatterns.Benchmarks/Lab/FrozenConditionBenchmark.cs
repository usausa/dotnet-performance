namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Frozen;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// COL-02 study: the build cost of FrozenDictionary (the denominator of the adoption trade-off)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class FrozenBuildBenchmark
{
    private KeyValuePair<string, int>[] pairs = default!;

    // 1024: R-08 revival check - does the lookup side start to win at larger sizes?
    [Params(16, 256, 1024)]
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

// COL-02 study: the lookup cost (the numerator of the adoption trade-off) — a full scan with non-interned keys
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class FrozenLookupBenchmark
{
    private Dictionary<string, int> dictionary = default!;

    private FrozenDictionary<string, int> frozen = default!;

    private string[] probes = default!;

    // 1024: R-08 revival check - does the lookup side start to win at larger sizes?
    [Params(16, 256, 1024)]
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
            probes[i] = new string(key.AsSpan());   // Non-interned runtime key
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

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class FrozenBuildSourceBenchmark
{
    private KeyValuePair<string, int>[] pairs = default!;

    private Dictionary<string, int> dictionary = default!;

    [Params(256, 4096)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        pairs = new KeyValuePair<string, int>[Count];
        for (var i = 0; i < Count; i++)
        {
            pairs[i] = new KeyValuePair<string, int>("Key" + i, i);
        }

        dictionary = new Dictionary<string, int>(pairs, StringComparer.Ordinal);
    }

    [Benchmark(Baseline = true)]
    public int ArrayWithSelectors() => pairs.ToFrozenDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal).Count;

    [Benchmark]
    public int ArrayOfPairs() => pairs.ToFrozenDictionary(StringComparer.Ordinal).Count;

    [Benchmark]
    public int IteratorWithSelectors() => Iterate(pairs).ToFrozenDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal).Count;

    [Benchmark]
    public int IteratorOfPairs() => Iterate(pairs).ToFrozenDictionary(StringComparer.Ordinal).Count;

    [Benchmark]
    public int FromDictionary() => dictionary.ToFrozenDictionary(StringComparer.Ordinal).Count;

    public static void Verify()
    {
        var benchmark = new FrozenBuildSourceBenchmark { Count = 64 };
        benchmark.Setup();
        var expected = benchmark.pairs.ToFrozenDictionary(StringComparer.Ordinal);
        foreach (var built in new[]
        {
            benchmark.pairs.ToFrozenDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal),
            Iterate(benchmark.pairs).ToFrozenDictionary(static p => p.Key, static p => p.Value, StringComparer.Ordinal),
            Iterate(benchmark.pairs).ToFrozenDictionary(StringComparer.Ordinal),
            benchmark.dictionary.ToFrozenDictionary(StringComparer.Ordinal),
        })
        {
            if ((built.Count != expected.Count) || built.Any(x => !expected.TryGetValue(x.Key, out var value) || (value != x.Value)))
            {
                throw new InvalidOperationException("Verify failed. FrozenBuildSource");
            }
        }

        if ((benchmark.ArrayWithSelectors() != 64) ||
            (benchmark.ArrayOfPairs() != 64) ||
            (benchmark.IteratorWithSelectors() != 64) ||
            (benchmark.IteratorOfPairs() != 64) ||
            (benchmark.FromDictionary() != 64))
        {
            throw new InvalidOperationException("Verify failed. FrozenBuildSource count");
        }
    }

    private static IEnumerable<KeyValuePair<string, int>> Iterate(KeyValuePair<string, int>[] source)
    {
        foreach (var pair in source)
        {
            yield return pair;
        }
    }
}
