namespace CandidateVerification.Benchmarks.Candidates;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// C-12: node-list Type-keyed hash map exposing two lookup paths over the same bucket layout.
// type.GetHashCode() and RuntimeHelpers.GetHashCode(type) return the same identity hash value,
// so bucket positions are identical — the only variable is the acquisition path (virtual call vs inlined static call).
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
    public bool TryGetValueHandleHash(Type key, [MaybeNullWhen(false)] out TValue value)
    {
        // MethodTable pointer as the identity: a plain field read, but 8-byte aligned,
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

    private sealed class Node(Type key, TValue value, Node? next)
    {
        public Type Key { get; } = key;

        public TValue Value { get; } = value;

        public Node? Next { get; } = next;
    }
}
