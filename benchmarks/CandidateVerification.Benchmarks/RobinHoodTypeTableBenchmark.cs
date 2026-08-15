namespace CandidateVerification.Benchmarks;

using System.Collections.Frozen;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

// C-03 (TYP-01 / COL-04 update candidate): runtime Type-keyed lookup.
// The question is whether Robin Hood + fingerprint metadata beats the existing node-list layout
// (ThreadsafeTypeHashArrayMap shape). Hash acquisition is settled by C-12: all candidates use identity hash.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RobinHoodTypeTableBenchmark
{
    private Dictionary<Type, int> dictionary = default!;

    private FrozenDictionary<Type, int> frozen = default!;

    private NodeTypeHashMap<int> nodeList = default!;

    private RobinHoodTypeTable<int> robinHood = default!;

    [GlobalSetup]
    public void Setup()
    {
        dictionary = [];
        for (var i = 0; i < TypeSets.Hit.Length; i++)
        {
            dictionary[TypeSets.Hit[i]] = i;
        }

        frozen = dictionary.ToFrozenDictionary();
        nodeList = new NodeTypeHashMap<int>(dictionary);
        robinHood = new RobinHoodTypeTable<int>(dictionary);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 32)]
    public int HitDictionary()
    {
        var total = 0;
        foreach (var type in TypeSets.Hit)
        {
            if (dictionary.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int HitFrozen()
    {
        var total = 0;
        foreach (var type in TypeSets.Hit)
        {
            if (frozen.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int HitNodeList()
    {
        var total = 0;
        foreach (var type in TypeSets.Hit)
        {
            if (nodeList.TryGetValueIdentityHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int HitRobinHood()
    {
        var total = 0;
        foreach (var type in TypeSets.Hit)
        {
            if (robinHood.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int MissDictionary()
    {
        var total = 0;
        foreach (var type in TypeSets.Miss)
        {
            if (dictionary.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int MissFrozen()
    {
        var total = 0;
        foreach (var type in TypeSets.Miss)
        {
            if (frozen.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int MissNodeList()
    {
        var total = 0;
        foreach (var type in TypeSets.Miss)
        {
            if (nodeList.TryGetValueIdentityHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int MissRobinHood()
    {
        var total = 0;
        foreach (var type in TypeSets.Miss)
        {
            if (robinHood.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new RobinHoodTypeTableBenchmark();
        benchmark.Setup();

        for (var i = 0; i < TypeSets.Hit.Length; i++)
        {
            if (!benchmark.robinHood.TryGetValue(TypeSets.Hit[i], out var value) || (value != i))
            {
                throw new InvalidOperationException($"RobinHood lookup failed for {TypeSets.Hit[i]}.");
            }
        }

        foreach (var type in TypeSets.Miss)
        {
            if (benchmark.robinHood.TryGetValue(type, out _))
            {
                throw new InvalidOperationException($"RobinHood returned a value for missing key {type}.");
            }
        }

        if ((benchmark.HitDictionary() != benchmark.HitRobinHood()) ||
            (benchmark.HitDictionary() != benchmark.HitFrozen()) ||
            (benchmark.HitDictionary() != benchmark.HitNodeList()) ||
            (benchmark.MissRobinHood() != 0) || (benchmark.MissNodeList() != 0))
        {
            throw new InvalidOperationException("Type table variants disagree.");
        }
    }
}
