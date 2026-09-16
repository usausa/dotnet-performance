namespace PerformancePatterns.Benchmarks.Lab;

using System.Numerics;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// COL follow-up: an article (屋根裏工房改, 2024) compares six hash table designs at 1024 entries and
// reports the ankerl::unordered_dense layout (Robin Hood probing over a compact metadata array whose
// entries carry probe distance + an 8-bit fingerprint, with the key/value pairs kept dense in a separate
// array) at 4.88 us for 1024 hit lookups against 18.61 us for Dictionary<K,V> (about 3.8x). The catalog
// has COL-04 for known key sets and TYP-01 for Type keys but no general-purpose table, so the claim is
// checked here with both tables using the same string hash (string.GetHashCode) and the same 1024 keys.
//
//   LookupHit  - every key present
//   LookupMiss - no key present (probe until the Robin Hood invariant proves absence)
//   Build      - insert all 1024 keys into a freshly sized table
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class HashTableDesignBenchmark
{
    private const int Count = 1024;

    private string[] keys = default!;
    private string[] missing = default!;
    private Dictionary<string, int> dictionary = default!;
    private Dictionary<string, int> dictionaryOrdinal = default!;
    private AnkerlDenseMap<string, int> dense = default!;

    [GlobalSetup]
    public void Setup()
    {
        keys = new string[Count];
        missing = new string[Count];
        for (var i = 0; i < Count; i++)
        {
            keys[i] = $"key_{i:D4}";
            missing[i] = $"miss_{i:D4}";
        }

        dictionary = [];
        dictionary.EnsureCapacity(Count);
        dictionaryOrdinal = new Dictionary<string, int>(Count, StringComparer.Ordinal);
        dense = new AnkerlDenseMap<string, int>(Count);
        for (var i = 0; i < Count; i++)
        {
            dictionary.Add(keys[i], i);
            dictionaryOrdinal.Add(keys[i], i);
            dense.Add(keys[i], i);
        }
    }

    //--------------------------------------------------------------------------------
    // Lookup (hit)
    //--------------------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    public long DictionaryHit()
    {
        var total = 0L;
        var map = dictionary;
        var source = keys;
        for (var i = 0; i < source.Length; i++)
        {
            if (map.TryGetValue(source[i], out var value))
            {
                total += value;
            }
        }

        return total;
    }

    // Dictionary<string, V> defaults to a non-randomized string hash and switches to the randomized one only
    // on heavy collision; the Ankerl table can only call string.GetHashCode (randomized). This row gives
    // Dictionary the same randomized hash so the remaining gap is the table layout alone.
    [Benchmark]
    public long DictionaryOrdinalHit()
    {
        var total = 0L;
        var map = dictionaryOrdinal;
        var source = keys;
        for (var i = 0; i < source.Length; i++)
        {
            if (map.TryGetValue(source[i], out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark]
    public long AnkerlHit()
    {
        var total = 0L;
        var map = dense;
        var source = keys;
        for (var i = 0; i < source.Length; i++)
        {
            if (map.TryGetValue(source[i], out var value))
            {
                total += value;
            }
        }

        return total;
    }

    //--------------------------------------------------------------------------------
    // Lookup (miss)
    //--------------------------------------------------------------------------------

    [Benchmark]
    public long DictionaryMiss()
    {
        var total = 0L;
        var map = dictionary;
        var source = missing;
        for (var i = 0; i < source.Length; i++)
        {
            if (map.TryGetValue(source[i], out var value))
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark]
    public long AnkerlMiss()
    {
        var total = 0L;
        var map = dense;
        var source = missing;
        for (var i = 0; i < source.Length; i++)
        {
            if (map.TryGetValue(source[i], out var value))
            {
                total += value;
            }
        }

        return total;
    }

    //--------------------------------------------------------------------------------
    // Build
    //--------------------------------------------------------------------------------

    [Benchmark]
    public int DictionaryBuild()
    {
        var map = new Dictionary<string, int>(Count);
        var source = keys;
        for (var i = 0; i < source.Length; i++)
        {
            map.Add(source[i], i);
        }

        return map.Count;
    }

    [Benchmark]
    public int AnkerlBuild()
    {
        var map = new AnkerlDenseMap<string, int>(Count);
        var source = keys;
        for (var i = 0; i < source.Length; i++)
        {
            map.Add(source[i], i);
        }

        return map.Count;
    }

    //--------------------------------------------------------------------------------
    // ankerl::unordered_dense layout (fixed capacity; enough for this study)
    //--------------------------------------------------------------------------------

    // Bucket metadata: high bits = probe distance (starts at DistanceIncrement), low 8 bits = fingerprint.
    // Zero means empty. Values live densely in a separate array and buckets point at them by index.
    public sealed class AnkerlDenseMap<TKey, TValue>
        where TKey : notnull
    {
        private const uint DistanceIncrement = 1u << 8;
        private const uint FingerprintMask = 0xFFu;

        private readonly Bucket[] buckets;
        private readonly TKey[] keys;
        private readonly TValue[] values;
        private readonly int shift;

        public AnkerlDenseMap(int capacity)
        {
            // Load factor 0.8 like the reference implementation, rounded up to a power of two
            var bucketCount = (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(16, capacity * 5 / 4));
            buckets = new Bucket[bucketCount];
            keys = new TKey[capacity];
            values = new TValue[capacity];
            shift = 32 - BitOperations.Log2((uint)bucketCount);
        }

        public int Count { get; private set; }

        public void Add(TKey key, TValue value)
        {
            var hash = (uint)key.GetHashCode();
            var distanceAndFingerprint = DistanceIncrement | (hash & FingerprintMask);
            var index = (int)(hash >> shift);

            while (true)
            {
                ref var bucket = ref buckets[index];
                if (distanceAndFingerprint == bucket.DistanceAndFingerprint &&
                    EqualityComparer<TKey>.Default.Equals(keys[bucket.ValueIndex], key))
                {
                    throw new ArgumentException("Duplicate key.", nameof(key));
                }

                if (distanceAndFingerprint > bucket.DistanceAndFingerprint)
                {
                    break;
                }

                distanceAndFingerprint += DistanceIncrement;
                index = Next(index);
            }

            var valueIndex = Count++;
            keys[valueIndex] = key;
            values[valueIndex] = value;
            PlaceAndShiftUp(new Bucket(distanceAndFingerprint, (uint)valueIndex), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(TKey key, out TValue value)
        {
            var hash = (uint)key.GetHashCode();
            var distanceAndFingerprint = DistanceIncrement | (hash & FingerprintMask);
            var index = (int)(hash >> shift);
            var table = buckets;

            while (true)
            {
                var bucket = table[index];
                if (distanceAndFingerprint == bucket.DistanceAndFingerprint)
                {
                    var valueIndex = (int)bucket.ValueIndex;
                    if (EqualityComparer<TKey>.Default.Equals(keys[valueIndex], key))
                    {
                        value = values[valueIndex];
                        return true;
                    }
                }
                else if (distanceAndFingerprint > bucket.DistanceAndFingerprint)
                {
                    // Robin Hood invariant: anything this far from home would have displaced this bucket
                    value = default!;
                    return false;
                }

                distanceAndFingerprint += DistanceIncrement;
                index = Next(index);
            }
        }

        // Robin Hood insertion: keep swapping with poorer buckets until an empty slot is reached
        private void PlaceAndShiftUp(Bucket bucket, int index)
        {
            while (buckets[index].DistanceAndFingerprint != 0)
            {
                (bucket, buckets[index]) = (buckets[index], bucket);
                bucket = new Bucket(bucket.DistanceAndFingerprint + DistanceIncrement, bucket.ValueIndex);
                index = Next(index);
            }

            buckets[index] = bucket;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Next(int index) => index + 1 == buckets.Length ? 0 : index + 1;

        private readonly record struct Bucket(uint DistanceAndFingerprint, uint ValueIndex);
    }
}
