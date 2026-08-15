namespace CandidateVerification.Benchmarks.Candidates;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// C-03 candidate: build-once Robin Hood hash table for Type keys.
// Probe loop touches only the packed metadata array (upper bits = probe distance + 1, low byte = hash fingerprint);
// the key/value payload is dereferenced only on a fingerprint match.
public sealed class RobinHoodTypeTable<TValue>
{
    private readonly int[] buckets;

    private readonly Type?[] keys;

    private readonly TValue[] values;

    private readonly int mask;

    public RobinHoodTypeTable(IEnumerable<KeyValuePair<Type, TValue>> source)
    {
        var pairs = source.ToArray();

        var initialCapacity = (int)(pairs.Length / 0.75);
        var capacity = 1;
        while (capacity < initialCapacity)
        {
            capacity <<= 1;
        }

        buckets = new int[capacity];
        keys = new Type?[capacity];
        values = new TValue[capacity];
        mask = capacity - 1;

        foreach (var pair in pairs)
        {
            Insert(pair.Key, pair.Value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(Type key, [MaybeNullWhen(false)] out TValue value)
    {
        // Identity hash: skips the virtual Type.GetHashCode dispatch (runtime Type objects are unique)
        var hash = RuntimeHelpers.GetHashCode(key);
        var fingerprint = hash & 0xFF;
        var index = hash & mask;
        var distance = 0;
        while (true)
        {
            var bucket = buckets[index];
            if ((bucket == 0) || ((bucket >>> 8) - 1 < distance))
            {
                // Robin Hood invariant: a stored entry can never be poorer than the query
                value = default;
                return false;
            }

            if (((bucket & 0xFF) == fingerprint) && ReferenceEquals(keys[index], key))
            {
                value = values[index];
                return true;
            }

            index = (index + 1) & mask;
            distance++;
        }
    }

    private void Insert(Type key, TValue value)
    {
        var hash = RuntimeHelpers.GetHashCode(key);
        var fingerprint = hash & 0xFF;
        var index = hash & mask;
        var distance = 0;
        while (true)
        {
            var bucket = buckets[index];
            if (bucket == 0)
            {
                buckets[index] = ((distance + 1) << 8) | fingerprint;
                keys[index] = key;
                values[index] = value;
                return;
            }

            var existingDistance = (bucket >>> 8) - 1;
            if (existingDistance < distance)
            {
                // Displace the richer entry and keep inserting it (keeps probe lengths short)
                buckets[index] = ((distance + 1) << 8) | fingerprint;
                (key, keys[index]) = (keys[index]!, key);
                (value, values[index]) = (values[index], value);
                fingerprint = bucket & 0xFF;
                distance = existingDistance;
            }

            index = (index + 1) & mask;
            distance++;
        }
    }
}
