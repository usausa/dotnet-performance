namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-07 follow-up: TYP-07 compared where the hash comes from inside a hand-written Type-keyed table.
// This compares the *key type* handed to the BCL Dictionary instead, because the two decisions are
// independent and a library that stays on Dictionary<,> cannot use TYP-07's answer directly.
//
//   DictionaryType         - Dictionary<Type, int>, default comparer. Type.GetHashCode / Type.Equals are
//                            virtual (RuntimeType overrides them) and the key is a reference type.
//   DictionaryHandle       - Dictionary<RuntimeTypeHandle, int>, default comparer. Same identity hash
//                            value, but the key is an 8-byte struct whose GetHashCode / Equals resolve
//                            without a virtual call, and Dictionary takes its value-type-key fast path.
//   DictionaryTypeComparer - Dictionary<Type, int> with a class comparer that hashes TypeHandle.Value,
//                            i.e. TYP-07's hash source forced through Dictionary's IEqualityComparer slot.
//   CustomHandleValue      - the TYP-07 NodeTypeHashMap reading TypeHandle.Value directly (reference).
//   ConcurrentDictionaryType / ConcurrentDictionaryHandle
//                          - the same two key types on ConcurrentDictionary, the shape of a cache that grows
//                            at run time (static registries, GetOrAdd caches). Its TryGetValue has a
//                            value-type-key fast path (comparer stored as null) and always goes through
//                            the IEqualityComparer interface for reference-type keys.
//
// The caller holds a Type (the common shape: obj.GetType() or a Type flowing as data), so the handle
// variants pay for type.TypeHandle at the call site. 32 hits / 8 misses, as in TYP-07.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeKeyBenchmark
{
    private Dictionary<Type, int> byType = default!;
    private Dictionary<RuntimeTypeHandle, int> byHandle = default!;
    private Dictionary<Type, int> byTypeHandleComparer = default!;
    private ConcurrentDictionary<Type, int> concurrentByType = default!;
    private ConcurrentDictionary<RuntimeTypeHandle, int> concurrentByHandle = default!;
    private NodeTypeHashMap<int> custom = default!;

    [GlobalSetup]
    public void Setup()
    {
        var hits = TypeSets.Hit;
        byType = [];
        byType.EnsureCapacity(hits.Length);
        byHandle = [];
        byHandle.EnsureCapacity(hits.Length);
        byTypeHandleComparer = new Dictionary<Type, int>(hits.Length, TypeHandleComparer.Instance);
        concurrentByType = new();
        concurrentByHandle = new();
        var pairs = new KeyValuePair<Type, int>[hits.Length];
        for (var i = 0; i < hits.Length; i++)
        {
            byType.Add(hits[i], i);
            byHandle.Add(hits[i].TypeHandle, i);
            byTypeHandleComparer.Add(hits[i], i);
            concurrentByType.TryAdd(hits[i], i);
            concurrentByHandle.TryAdd(hits[i].TypeHandle, i);
            pairs[i] = new KeyValuePair<Type, int>(hits[i], i);
        }

        custom = new NodeTypeHashMap<int>(pairs);
    }

    //--------------------------------------------------------------------------------
    // Hit
    //--------------------------------------------------------------------------------

    [Benchmark(Baseline = true, OperationsPerInvoke = 32)]
    public int DictionaryTypeHit()
    {
        var total = 0;
        var map = byType;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int DictionaryHandleHit()
    {
        var total = 0;
        var map = byHandle;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValue(type.TypeHandle, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int DictionaryTypeComparerHit()
    {
        var total = 0;
        var map = byTypeHandleComparer;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int CustomHandleValueHit()
    {
        var total = 0;
        var map = custom;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValueHandleHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int ConcurrentDictionaryTypeHit()
    {
        var total = 0;
        var map = concurrentByType;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 32)]
    public int ConcurrentDictionaryHandleHit()
    {
        var total = 0;
        var map = concurrentByHandle;
        foreach (var type in TypeSets.Hit)
        {
            if (map.TryGetValue(type.TypeHandle, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    //--------------------------------------------------------------------------------
    // Miss
    //--------------------------------------------------------------------------------

    [Benchmark(OperationsPerInvoke = 8)]
    public int DictionaryTypeMiss()
    {
        var total = 0;
        var map = byType;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int DictionaryHandleMiss()
    {
        var total = 0;
        var map = byHandle;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValue(type.TypeHandle, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int DictionaryTypeComparerMiss()
    {
        var total = 0;
        var map = byTypeHandleComparer;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int CustomHandleValueMiss()
    {
        var total = 0;
        var map = custom;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValueHandleHash(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int ConcurrentDictionaryTypeMiss()
    {
        var total = 0;
        var map = concurrentByType;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValue(type, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = 8)]
    public int ConcurrentDictionaryHandleMiss()
    {
        var total = 0;
        var map = concurrentByHandle;
        foreach (var type in TypeSets.Miss)
        {
            if (map.TryGetValue(type.TypeHandle, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    // Every hit variant must find all 32 keys with their index; every miss variant must find nothing.
    public static void Verify()
    {
        var benchmark = new TypeKeyBenchmark();
        benchmark.Setup();
        const int expected = 32 * 31 / 2;
        if ((benchmark.DictionaryTypeHit() != expected) ||
            (benchmark.DictionaryHandleHit() != expected) ||
            (benchmark.DictionaryTypeComparerHit() != expected) ||
            (benchmark.CustomHandleValueHit() != expected) ||
            (benchmark.ConcurrentDictionaryTypeHit() != expected) ||
            (benchmark.ConcurrentDictionaryHandleHit() != expected) ||
            (benchmark.DictionaryTypeMiss() != 0) ||
            (benchmark.DictionaryHandleMiss() != 0) ||
            (benchmark.DictionaryTypeComparerMiss() != 0) ||
            (benchmark.ConcurrentDictionaryTypeMiss() != 0) ||
            (benchmark.ConcurrentDictionaryHandleMiss() != 0) ||
            (benchmark.CustomHandleValueMiss() != 0))
        {
            throw new InvalidOperationException("Verify failed. TypeKey");
        }
    }

    // TYP-07's hash source, forced through the only extension point Dictionary offers. It has to be a
    // class (Dictionary stores IEqualityComparer<TKey>), so each call is an interface dispatch.
    private sealed class TypeHandleComparer : IEqualityComparer<Type>
    {
        public static readonly TypeHandleComparer Instance = new();

        public bool Equals(Type? x, Type? y) => ReferenceEquals(x, y);

        // Handles are pointer-aligned, so the low 3 bits are always zero: shift them out
        public int GetHashCode([DisallowNull] Type obj) => (int)((ulong)obj.TypeHandle.Value.ToInt64() >> 3);
    }
}
