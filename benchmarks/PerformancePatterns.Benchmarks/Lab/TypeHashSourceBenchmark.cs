namespace PerformancePatterns.Benchmarks.Lab;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-07 study: Type-keyed hash lookup with identical layout and bucket placement, varying only the
// hash acquisition path - type.GetHashCode() (virtual) vs RuntimeHelpers.GetHashCode (static, inlined)
// vs type.TypeHandle.Value (plain field read of the type handle)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeHashSourceBenchmark
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
        var benchmark = new TypeHashSourceBenchmark();
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

// Node-list Type-keyed hash map exposing three lookup paths over the same bucket layout.
// type.GetHashCode() and RuntimeHelpers.GetHashCode(type) return the same identity hash value, so those two
// share a table and the only variable is the acquisition path (virtual call vs inlined static call).
// The handle hash places keys differently, so it gets its own table of the same shape.
public sealed class NodeTypeHashMap<TValue>
{
    private readonly Node?[] buckets;

    private readonly Node?[] handleBuckets;

    private readonly int mask;

    public NodeTypeHashMap(IEnumerable<KeyValuePair<Type, TValue>> source)
    {
        var pairs = source.ToArray();

        // Capacity rule: 2^n >= N * 2 (matches the external measurement)
        var initialCapacity = pairs.Length * 2;
        var capacity = 1;
        while (capacity < initialCapacity)
        {
            capacity <<= 1;
        }

        buckets = new Node?[capacity];

        // Separate table: the handle hash places keys in different buckets than the identity hash
        handleBuckets = new Node?[capacity];
        mask = capacity - 1;

        foreach (var pair in pairs)
        {
            var index = RuntimeHelpers.GetHashCode(pair.Key) & mask;
            buckets[index] = new Node(pair.Key, pair.Value, buckets[index]);

            var handleIndex = (int)((ulong)pair.Key.TypeHandle.Value.ToInt64() >> 3) & mask;
            handleBuckets[handleIndex] = new Node(pair.Key, pair.Value, handleBuckets[handleIndex]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValueVirtualHash(Type key, [MaybeNullWhen(false)] out TValue value)
    {
        // Virtual dispatch: RuntimeType.GetHashCode via the vtable (PGO may only partially devirtualize)
        var node = buckets[key.GetHashCode() & mask];
        while (node is not null)
        {
            if (ReferenceEquals(node.Key, key))
            {
                value = node.Value;
                return true;
            }

            node = node.Next;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValueIdentityHash(Type key, [MaybeNullWhen(false)] out TValue value)
    {
        // Static call: the identity-hash header read inlines, no virtual call in the loop body
        var node = buckets[RuntimeHelpers.GetHashCode(key) & mask];
        while (node is not null)
        {
            if (ReferenceEquals(node.Key, key))
            {
                value = node.Value;
                return true;
            }

            node = node.Next;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValueHandleHash(Type key, [MaybeNullWhen(false)] out TValue value)
    {
        // Type handle as the identity: a plain field read, but 8-byte aligned,
        // so the low bits are always zero and must be shifted away before masking
        var node = handleBuckets[(int)((ulong)key.TypeHandle.Value.ToInt64() >> 3) & mask];
        while (node is not null)
        {
            if (ReferenceEquals(node.Key, key))
            {
                value = node.Value;
                return true;
            }

            node = node.Next;
        }

        value = default;
        return false;
    }

    private sealed class Node(Type key, TValue value, Node? next)
    {
        public Type Key { get; } = key;

        public TValue Value { get; } = value;

        public Node? Next { get; } = next;
    }
}

// Shared probe sets for the Type-keyed lookup benchmark
public static class TypeSets
{
    public static readonly Type[] Hit =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(bool),
        typeof(char), typeof(string), typeof(object), typeof(DateTime),
        typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid), typeof(Uri),
        typeof(Version), typeof(int[]), typeof(string[]), typeof(List<int>),
        typeof(Dictionary<int, int>), typeof(HashSet<int>), typeof(Queue<int>), typeof(Stack<int>),
        typeof(Task), typeof(ValueTask), typeof(StringBuilder), typeof(Exception)
    ];

    public static readonly Type[] Miss =
    [
        typeof(Random), typeof(Regex), typeof(Array), typeof(Enum),
        typeof(Delegate), typeof(Attribute), typeof(Convert), typeof(GC)
    ];
}
