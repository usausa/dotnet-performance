namespace CandidateVerification.Benchmarks;

using System.Buffers.Binary;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

public enum DigestKeyShape
{
    // Random ASCII keys: digests almost always decide the probe (best case for the digest)
    Random,

    // All keys share the same 8-byte prefix: every digest ties and falls back to full compare (worst case)
    SharedPrefix,
}

// C-04 (new pattern candidate, BIT): binary search over variable-length keys —
// full SequenceCompareTo probes vs order-preserving 8-byte digest array probes.
// Full verification: table size (64/256/1024) x key shape (random / shared-prefix worst case).
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class OrderedDigestSearchBenchmark
{
    private const int HitProbeCount = 256;

    private const int MissProbeCount = 64;

    private byte[][] sortedKeys = default!;

    private ulong[] digests = default!;

    private byte[][] hitProbes = default!;

    private byte[][] missProbes = default!;

    [Params(64, 256, 1024)]
    public int KeyCount { get; set; }

    [Params(DigestKeyShape.Random, DigestKeyShape.SharedPrefix)]
    public DigestKeyShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var seed = 12345u;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var keys = new List<byte[]>(KeyCount);
        while (keys.Count < KeyCount)
        {
            var key = CreateKey(ref seed, Shape);
            if (seen.Add(Encoding.ASCII.GetString(key)))
            {
                keys.Add(key);
            }
        }

        sortedKeys = [.. keys];
        Array.Sort(sortedKeys, static (a, b) => a.AsSpan().SequenceCompareTo(b));

        digests = new ulong[KeyCount];
        for (var i = 0; i < KeyCount; i++)
        {
            digests[i] = GetDigest(sortedKeys[i]);
        }

        // Content copies in scattered order (reference shortcuts must not help the compare)
        hitProbes = new byte[HitProbeCount][];
        for (var i = 0; i < HitProbeCount; i++)
        {
            hitProbes[i] = [.. sortedKeys[(i * 31) % KeyCount]];
        }

        missProbes = new byte[MissProbeCount][];
        var missIndex = 0;
        while (missIndex < MissProbeCount)
        {
            var key = CreateKey(ref seed, Shape);
            if (seen.Add(Encoding.ASCII.GetString(key)))
            {
                missProbes[missIndex] = key;
                missIndex++;
            }
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = HitProbeCount)]
    public int HitFullCompare()
    {
        var total = 0;
        foreach (var probe in hitProbes)
        {
            total += SearchFull(sortedKeys, probe);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = HitProbeCount)]
    public int HitDigest()
    {
        var total = 0;
        foreach (var probe in hitProbes)
        {
            total += SearchDigest(sortedKeys, digests, probe);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = MissProbeCount)]
    public int MissFullCompare()
    {
        var total = 0;
        foreach (var probe in missProbes)
        {
            total += SearchFull(sortedKeys, probe);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = MissProbeCount)]
    public int MissDigest()
    {
        var total = 0;
        foreach (var probe in missProbes)
        {
            total += SearchDigest(sortedKeys, digests, probe);
        }

        return total;
    }

    public static void Verify()
    {
        foreach (var count in new[] { 64, 256, 1024 })
        {
            foreach (var shape in new[] { DigestKeyShape.Random, DigestKeyShape.SharedPrefix })
            {
                var benchmark = new OrderedDigestSearchBenchmark { KeyCount = count, Shape = shape };
                benchmark.Setup();

                // Digest must preserve byte-lexicographic order over the sorted key set
                for (var i = 1; i < count; i++)
                {
                    if (benchmark.digests[i - 1] > benchmark.digests[i])
                    {
                        throw new InvalidOperationException($"Digest order does not match key order ({count}/{shape}).");
                    }
                }

                foreach (var probe in benchmark.hitProbes)
                {
                    var full = SearchFull(benchmark.sortedKeys, probe);
                    var digest = SearchDigest(benchmark.sortedKeys, benchmark.digests, probe);
                    if ((full != digest) || (full < 0))
                    {
                        throw new InvalidOperationException($"Hit search mismatch ({count}/{shape}): {full} vs {digest}.");
                    }
                }

                foreach (var probe in benchmark.missProbes)
                {
                    if ((SearchFull(benchmark.sortedKeys, probe) != -1) ||
                        (SearchDigest(benchmark.sortedKeys, benchmark.digests, probe) != -1))
                    {
                        throw new InvalidOperationException($"Miss search must return -1 in both variants ({count}/{shape}).");
                    }
                }
            }
        }
    }

    private static int SearchFull(byte[][] keys, ReadOnlySpan<byte> probe)
    {
        var lo = 0;
        var hi = keys.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            var compared = probe.SequenceCompareTo(keys[mid]);
            if (compared == 0)
            {
                return mid;
            }

            if (compared > 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return -1;
    }

    private static int SearchDigest(byte[][] keys, ulong[] digests, ReadOnlySpan<byte> probe)
    {
        var probeDigest = GetDigest(probe);
        var lo = 0;
        var hi = keys.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) >>> 1;
            var digest = digests[mid];
            int compared;
            if (digest != probeDigest)
            {
                // Order-preserving digest: no key bytes touched on this probe
                compared = digest < probeDigest ? 1 : -1;
            }
            else
            {
                compared = probe.SequenceCompareTo(keys[mid]);
            }

            if (compared == 0)
            {
                return mid;
            }

            if (compared > 0)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid - 1;
            }
        }

        return -1;
    }

    // First 8 bytes packed big-endian with zero padding: preserves byte-lexicographic order for ASCII keys
    private static ulong GetDigest(ReadOnlySpan<byte> key)
    {
        Span<byte> buffer = stackalloc byte[8];
        buffer.Clear();
        key[..Math.Min(key.Length, 8)].CopyTo(buffer);
        return BinaryPrimitives.ReadUInt64BigEndian(buffer);
    }

    private static byte[] CreateKey(ref uint seed, DigestKeyShape shape)
    {
        if (shape == DigestKeyShape.SharedPrefix)
        {
            // 8-byte common prefix + 4-8 random letters: all digests collide
            var suffixLength = 4 + (int)(NextRandom(ref seed) % 5u);
            var key = new byte[8 + suffixLength];
            "sharedpr"u8.CopyTo(key);
            for (var i = 8; i < key.Length; i++)
            {
                key[i] = (byte)('a' + (NextRandom(ref seed) % 26u));
            }

            return key;
        }

        var length = 4 + (int)(NextRandom(ref seed) % 13u);
        var randomKey = new byte[length];
        for (var i = 0; i < length; i++)
        {
            randomKey[i] = (byte)('a' + (NextRandom(ref seed) % 26u));
        }

        return randomKey;
    }

    private static uint NextRandom(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }
}
