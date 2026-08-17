namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

// C-12 (new pattern candidate, TYP): Type-keyed hash lookup with identical layout and bucket placement,
// varying only the hash acquisition path — type.GetHashCode() (virtual) vs RuntimeHelpers.GetHashCode (static, inlined)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeIdentityHashBenchmark
{
    private NodeTypeHashMap<int> map = default!;

    [GlobalSetup]
    public void Setup()
    {
        var pairs = new KeyValuePair<Type, int>[TypeSets.Hit.Length];
        for (var i = 0; i < TypeSets.Hit.Length; i++)
        {
            pairs[i] = new KeyValuePair<Type, int>(TypeSets.Hit[i], i);
        }

        map = new NodeTypeHashMap<int>(pairs);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = 32)]
    public int VirtualHashHit()
    {
        var total = 0;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValueVirtualHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int IdentityHashHit()
    {
        var total = 0;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValueIdentityHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int HandleHashHit()
    {
        var total = 0;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValueHandleHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int VirtualHashMiss()
    {
        var total = 0;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValueVirtualHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int IdentityHashMiss()
    {
        var total = 0;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValueIdentityHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int HandleHashMiss()
    {
        var total = 0;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValueHandleHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new TypeIdentityHashBenchmark();
        benchmark.Setup();

        for (var i = 0; i < TypeSets.Hit.Length; i++)
        {
            if (!benchmark.map.TryGetValueVirtualHash(TypeSets.Hit[i], out var viaVirtual) || (viaVirtual != i) ||
                !benchmark.map.TryGetValueIdentityHash(TypeSets.Hit[i], out var viaIdentity) || (viaIdentity != i) ||
                !benchmark.map.TryGetValueHandleHash(TypeSets.Hit[i], out var viaHandle) || (viaHandle != i))
            {
                throw new InvalidOperationException($"Hash path lookup failed for {TypeSets.Hit[i]}.");
            }
        }

        foreach (var type in TypeSets.Miss)
        {
            if (benchmark.map.TryGetValueVirtualHash(type, out _) ||
                benchmark.map.TryGetValueIdentityHash(type, out _) ||
                benchmark.map.TryGetValueHandleHash(type, out _))
            {
                throw new InvalidOperationException($"Hash path returned a value for missing key {type}.");
            }
        }

        if ((benchmark.VirtualHashHit() != benchmark.IdentityHashHit()) ||
            (benchmark.VirtualHashHit() != benchmark.HandleHashHit()) ||
            (benchmark.VirtualHashMiss() != 0) || (benchmark.IdentityHashMiss() != 0) ||
            (benchmark.HandleHashMiss() != 0))
        {
            throw new InvalidOperationException("Hash path variants disagree.");
        }
    }
}
