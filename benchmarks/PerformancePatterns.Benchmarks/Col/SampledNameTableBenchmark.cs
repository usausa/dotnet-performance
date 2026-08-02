namespace PerformancePatterns.Benchmarks.Col;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Col;

// BIT-01 / COL-04 example: comparing name -> value resolution with a dictionary / a linear scan / a sampling hash table
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SampledNameTableBenchmark
{
    private string[] names = default!;

    private string[] probes = default!;

    private Dictionary<string, int> dictionary = default!;

    private SampledNameTable<int> table = default!;

    [Params(4, 16, 32)]
    public int Columns { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (names, probes, dictionary, table) = NameTableFixture.Create(Columns);
    }

    [Benchmark(Baseline = true)]
    public int DictionaryLookup()
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
    public int LinearScan()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            var span = probe.AsSpan();
            for (var i = 0; i < names.Length; i++)
            {
                if (span.SequenceEqual(names[i]))
                {
                    total += i;
                    break;
                }
            }
        }

        return total;
    }

    [Benchmark]
    public int SampledHashTable()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            table.TryGetValue(probe.AsSpan(), out var value);
            total += value;
        }

        return total;
    }
}

// net8.0 has no AlternateLookup, so the whole class is kept separate (methodology pitfall 7)
#if NET9_0_OR_GREATER
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SampledNameTableSpanKeyBenchmark
{
    private string[] probes = default!;

    private Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> alternateLookup;

    private System.Collections.Frozen.FrozenDictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> frozenAlternate;

    private SampledNameTable<int> table = default!;

    [Params(4, 16, 32)]
    public int Columns { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        (_, probes, var dictionary, table) = NameTableFixture.Create(Columns);
        alternateLookup = dictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        frozenAlternate = System.Collections.Frozen.FrozenDictionary
            .ToFrozenDictionary(dictionary, StringComparer.Ordinal)
            .GetAlternateLookup<ReadOnlySpan<char>>();
    }

    [Benchmark(Baseline = true)]
    public int DictionaryAlternateLookup()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            alternateLookup.TryGetValue(probe.AsSpan(), out var value);
            total += value;
        }

        return total;
    }

    [Benchmark]
    public int FrozenAlternateLookup()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            frozenAlternate.TryGetValue(probe.AsSpan(), out var value);
            total += value;
        }

        return total;
    }

    [Benchmark]
    public int SampledHashTable()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            table.TryGetValue(probe.AsSpan(), out var value);
            total += value;
        }

        return total;
    }
}
#endif

internal static class NameTableFixture
{
    public static (string[] Names, string[] Probes, Dictionary<string, int> Dictionary, SampledNameTable<int> Table) Create(int columns)
    {
        var names = new string[columns];
        var probes = new string[columns];
        for (var i = 0; i < columns; i++)
        {
            names[i] = "Column" + i;
            // Runtime-built copy (avoids reference-equality short-circuit from interned literals)
            probes[i] = new string(names[i].AsSpan());
        }

        var dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < columns; i++)
        {
            dictionary[names[i]] = i;
        }

        return (names, probes, dictionary, new SampledNameTable<int>(dictionary));
    }
}
