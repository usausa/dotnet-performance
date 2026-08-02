namespace PerformancePatterns.Col;

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// BIT-01 / COL-04: Read-only lookup specialized for a known set of names.
/// Computes an O(1) sampling hash from the length plus only the first / middle / last characters (BIT-01),
/// selects a bucket with a power-of-two size and mask (BIT-02), then confirms the match with an Ordinal comparison inside the bucket.
/// Keys can be matched as <see cref="ReadOnlySpan{T}"/> without materializing a string (the same goal as COL-03).
/// </summary>
public sealed class SampledNameTable<TValue>
{
    private readonly Bucket[] buckets;

    private readonly int mask;

    public SampledNameTable(IEnumerable<KeyValuePair<string, TValue>> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var source = entries.ToArray();
        Count = source.Length;

        // Deliberately sparse (at least twice the entry count) so the linear scan inside a bucket is close to a single item
        var size = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(source.Length * 2, 4));
        mask = size - 1;

        var work = new List<KeyValuePair<string, TValue>>?[size];
        foreach (var pair in source)
        {
            var index = CalculateHash(pair.Key.AsSpan()) & mask;
            (work[index] ??= []).Add(pair);
        }

        buckets = new Bucket[size];
        for (var i = 0; i < size; i++)
        {
            var items = work[i];
            if (items is null)
            {
                buckets[i] = new Bucket([], []);
                continue;
            }

            var keys = new string[items.Count];
            var values = new TValue[items.Count];
            for (var j = 0; j < items.Count; j++)
            {
                keys[j] = items[j].Key;
                values[j] = items[j].Value;
            }

            buckets[i] = new Bucket(keys, values);
        }
    }

    public int Count { get; }

    /// <summary>O(1) hash that looks only at the length and three characters. Collisions are resolved by the caller's exact-match comparison.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateHash(ReadOnlySpan<char> value)
    {
        var length = value.Length;
        if (length == 0)
        {
            return 0;
        }

        ref var head = ref MemoryMarshal.GetReference(value);
        var first = Unsafe.Add(ref head, 0);
        var middle = Unsafe.Add(ref head, length >> 1);
        var last = Unsafe.Add(ref head, length - 1);
        return (length << 16) ^ (first << 8) ^ (middle << 4) ^ last;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out TValue value)
    {
        ref var bucket = ref buckets[CalculateHash(key) & mask];
        var keys = bucket.Keys;
        for (var i = 0; i < keys.Length; i++)
        {
            // Always confirm with an exact match after the hash matches (the sampling hash can collide)
            if (key.SequenceEqual(keys[i]))
            {
                value = bucket.Values[i];
                return true;
            }
        }

        value = default;
        return false;
    }

    private readonly struct Bucket(string[] keys, TValue[] values)
    {
        public string[] Keys { get; } = keys;

        public TValue[] Values { get; } = values;
    }
}
