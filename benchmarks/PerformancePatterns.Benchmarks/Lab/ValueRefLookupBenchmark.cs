namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

public enum LookupProbe
{
    AllHit,
    HalfMiss,
}

// Study queue 7-3: CollectionsMarshal.GetValueRefOrNullRef + Unsafe.IsNullRef (the "optional ref" idiom).
// COL-01 already covers GetValueRefOrAddDefault; its note claims the gain shrinks when the shape is
// "check existence, then read or update in place" instead of "read-modify-write with insert".
// Question: quantify that. One hash probe returning a ref versus TryGetValue (one probe, value copied out).
//           Small value first, then a 32-byte value where the copy itself is supposed to matter.
// Note: probe keys are separate string instances from the stored keys, so reference equality cannot short-circuit.
// Note: the ContainsKey + indexer (two probe) shape is deliberately not measured -- CA1854 already rejects it
//       at build time in this repository, so TryGetValue is the honest baseline for the read path.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ValueRefLookupBenchmark
{
    private const int EntryCount = 1024;

    private const int ProbeCount = 256;

    private Dictionary<string, long> map = default!;

    private string[] probes = default!;

    [Params(LookupProbe.AllHit, LookupProbe.HalfMiss)]
    public LookupProbe Probe { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        map = new Dictionary<string, long>(EntryCount, StringComparer.Ordinal);
        for (var i = 0; i < EntryCount; i++)
        {
            map[LookupKeys.Stored(i)] = i;
        }

        probes = LookupKeys.BuildProbes(ProbeCount, Probe);
    }

    // One hash probe, value copied out
    [Benchmark(Baseline = true)]
    public long TryGetValueRead()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            if (map.TryGetValue(key, out var value))
            {
                total += value;
            }
        }

        return total;
    }

    // One hash probe, no copy: the entry itself is handed back as a ref
    [Benchmark]
    public long ValueRefOrNullRefRead()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            ref var slot = ref CollectionsMarshal.GetValueRefOrNullRef(map, key);
            if (!Unsafe.IsNullRef(ref slot))
            {
                total += slot;
            }
        }

        return total;
    }

    // Update path: two probes (read, then write back through the indexer)
    [Benchmark]
    public long TryGetValueThenIndexerUpdate()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            if (map.TryGetValue(key, out var value))
            {
                map[key] = value + 1;
                total += value + 1;
            }
        }

        return total;
    }

    // Update path: one probe, incremented in place
    [Benchmark]
    public long ValueRefOrNullRefUpdate()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            ref var slot = ref CollectionsMarshal.GetValueRefOrNullRef(map, key);
            if (!Unsafe.IsNullRef(ref slot))
            {
                slot++;
                total += slot;
            }
        }

        return total;
    }

    public static void Verify()
    {
        foreach (var probe in new[] { LookupProbe.AllHit, LookupProbe.HalfMiss })
        {
            var benchmark = new ValueRefLookupBenchmark { Probe = probe };

            benchmark.Setup();
            var expectedRead = benchmark.TryGetValueRead();
            if (benchmark.ValueRefOrNullRefRead() != expectedRead)
            {
                throw new InvalidOperationException($"Verify failed. ValueRefLookup read. probe=[{probe}]");
            }

            benchmark.Setup();
            var expectedUpdate = benchmark.TryGetValueThenIndexerUpdate();
            benchmark.Setup();
            if (benchmark.ValueRefOrNullRefUpdate() != expectedUpdate)
            {
                throw new InvalidOperationException($"Verify failed. ValueRefLookup update. probe=[{probe}]");
            }
        }
    }
}

// The same shapes over a 32-byte value, where copying the value out is supposed to be the deciding cost
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ValueRefLargeLookupBenchmark
{
    private const int EntryCount = 1024;

    private const int ProbeCount = 256;

    private Dictionary<string, Stat32> map = default!;

    private string[] probes = default!;

    [GlobalSetup]
    public void Setup()
    {
        map = new Dictionary<string, Stat32>(EntryCount, StringComparer.Ordinal);
        for (var i = 0; i < EntryCount; i++)
        {
            map[LookupKeys.Stored(i)] = new Stat32 { Count = i, Sum = i * 2, Min = i, Max = i * 3 };
        }

        probes = LookupKeys.BuildProbes(ProbeCount, LookupProbe.AllHit);
    }

    [Benchmark(Baseline = true)]
    public long TryGetValueRead()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            if (map.TryGetValue(key, out var value))
            {
                total += value.Count + value.Sum;
            }
        }

        return total;
    }

    [Benchmark]
    public long ValueRefOrNullRefRead()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            ref var slot = ref CollectionsMarshal.GetValueRefOrNullRef(map, key);
            if (!Unsafe.IsNullRef(ref slot))
            {
                total += slot.Count + slot.Sum;
            }
        }

        return total;
    }

    [Benchmark]
    public long TryGetValueThenIndexerUpdate()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            if (map.TryGetValue(key, out var value))
            {
                value.Count++;
                value.Sum += 2;
                map[key] = value;
                total += value.Count;
            }
        }

        return total;
    }

    [Benchmark]
    public long ValueRefOrNullRefUpdate()
    {
        var total = 0L;
        foreach (var key in probes)
        {
            ref var slot = ref CollectionsMarshal.GetValueRefOrNullRef(map, key);
            if (!Unsafe.IsNullRef(ref slot))
            {
                slot.Count++;
                slot.Sum += 2;
                total += slot.Count;
            }
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new ValueRefLargeLookupBenchmark();

        benchmark.Setup();
        var expectedRead = benchmark.TryGetValueRead();
        if (benchmark.ValueRefOrNullRefRead() != expectedRead)
        {
            throw new InvalidOperationException("Verify failed. ValueRefLargeLookup read.");
        }

        benchmark.Setup();
        var expectedUpdate = benchmark.TryGetValueThenIndexerUpdate();
        benchmark.Setup();
        if (benchmark.ValueRefOrNullRefUpdate() != expectedUpdate)
        {
            throw new InvalidOperationException("Verify failed. ValueRefLargeLookup update.");
        }
    }
}

internal struct Stat32
{
    public long Count;

    public long Sum;

    public long Min;

    public long Max;
}

internal static class LookupKeys
{
    public static string Stored(int index) => $"Column{index:D4}Name";

    // Probe keys are built independently so they are distinct instances from the stored keys
    public static string[] BuildProbes(int count, LookupProbe probe)
    {
        var keys = new string[count];
        for (var i = 0; i < count; i++)
        {
            var miss = (probe == LookupProbe.HalfMiss) && ((i & 1) == 1);
            keys[i] = new string((miss ? $"Absent{i:D4}Name" : Stored(i * 3)).AsSpan());
        }

        return keys;
    }
}
