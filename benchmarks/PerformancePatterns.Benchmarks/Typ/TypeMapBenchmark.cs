namespace PerformancePatterns.Benchmarks.Typ;

using System.Collections.Frozen;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Typ;

// TYP-01 example: moving type-keyed lookup from a dictionary to a static slot array
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeMapBenchmark
{
    private Dictionary<Type, string> dictionary = default!;

    private FrozenDictionary<Type, string> frozen = default!;

    private TypeMap<string> typeMap = default!;

    private Type runtimeType = default!;

    [GlobalSetup]
    public void Setup()
    {
        var pairs = new Dictionary<Type, string>
        {
            [typeof(int)] = "int",
            [typeof(long)] = "long",
            [typeof(string)] = "string",
            [typeof(Guid)] = "guid",
            [typeof(DateTime)] = "datetime",
            [typeof(decimal)] = "decimal",
            [typeof(double)] = "double",
            [typeof(byte)] = "byte",
        };

        dictionary = pairs;
        frozen = pairs.ToFrozenDictionary();

        typeMap = new TypeMap<string>();
        typeMap.Set<int>("int");
        typeMap.Set<long>("long");
        typeMap.Set<string>("string");
        typeMap.Set<Guid>("guid");
        typeMap.Set<DateTime>("datetime");
        typeMap.Set<decimal>("decimal");
        typeMap.Set<double>("double");
        typeMap.Set<byte>("byte");

        runtimeType = typeof(Guid);
    }

    [Benchmark(Baseline = true)]
    public string? DictionaryLookup()
    {
        dictionary.TryGetValue(typeof(Guid), out var value);
        return value;
    }

    [Benchmark]
    public string? FrozenLookup()
    {
        frozen.TryGetValue(typeof(Guid), out var value);
        return value;
    }

    [Benchmark]
    public string? TypeMapGeneric()
    {
        typeMap.TryGetValue<Guid>(out var value);
        return value;
    }

    [Benchmark]
    public string? TypeMapRuntimeType()
    {
        typeMap.TryGetValue(runtimeType, out var value);
        return value;
    }
}
