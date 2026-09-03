namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// R-04 extension: foreach vs for, i.e. when the two forms are interchangeable and when they are not.
// The existing R-04 entry only compares for / while / do-while / descending. These four studies cover
// the shapes where foreach could plausibly differ from an indexed for.
//
// 1. LoopFormSpanBenchmark   - array / Span / ReadOnlySpan: expected to be identical
// 2. LoopFormListBenchmark   - List<T>: foreach goes through List<T>.Enumerator (version check per step)
// 3. LoopFormStructBenchmark - large struct elements: foreach (var x in ...) copies each element
//
// Every variant computes the same sum so the Verify pass in Program.cs can compare them.

//--------------------------------------------------------------------------------
// 1. Array / Span / ReadOnlySpan
//--------------------------------------------------------------------------------

// Also covers the field-backed case: FieldFor reads this.values in the loop condition, LocalFor copies
// the reference to a local first. If the JIT cannot prove the field is unchanged, only the local form
// gets bounds-check elimination.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class LoopFormSpanBenchmark
{
    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[1024];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i;
        }
    }

    [Benchmark(Baseline = true)]
    public long ArrayForeach()
    {
        var total = 0L;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long ArrayFieldFor()
    {
        var total = 0L;
        for (var i = 0; i < values.Length; i++)
        {
            total += values[i];
        }

        return total;
    }

    [Benchmark]
    public long ArrayLocalFor()
    {
        var total = 0L;
        var local = values;
        for (var i = 0; i < local.Length; i++)
        {
            total += local[i];
        }

        return total;
    }

    [Benchmark]
    public long SpanForeach()
    {
        var total = 0L;
        foreach (var value in values.AsSpan())
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long SpanFor()
    {
        var total = 0L;
        var span = values.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public long ReadOnlySpanForeach()
    {
        var total = 0L;
        foreach (var value in (ReadOnlySpan<int>)values)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long ReadOnlySpanFor()
    {
        var total = 0L;
        ReadOnlySpan<int> span = values;
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }
}

//--------------------------------------------------------------------------------
// 2. List<T>
//--------------------------------------------------------------------------------

// List<T>.Enumerator.MoveNext compares _version against the list's version on every step, which the
// indexer form does not do. AsSpan is included as the known-good reference (COL-01).
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class LoopFormListBenchmark
{
    private List<int> values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = [];
        values.Capacity = 1024;
        for (var i = 0; i < 1024; i++)
        {
            values.Add(i);
        }
    }

    [Benchmark(Baseline = true)]
    public long ListForeach()
    {
        var total = 0L;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long ListFor()
    {
        var total = 0L;
        for (var i = 0; i < values.Count; i++)
        {
            total += values[i];
        }

        return total;
    }

    [Benchmark]
    public long ListAsSpanForeach()
    {
        var total = 0L;
        foreach (var value in CollectionsMarshal.AsSpan(values))
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long ListAsSpanFor()
    {
        var total = 0L;
        var span = CollectionsMarshal.AsSpan(values);
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }
}

//--------------------------------------------------------------------------------
// 3. Large struct elements
//--------------------------------------------------------------------------------

// foreach (var x in span) copies each element into the loop variable. At 64 bytes per element that copy
// is the whole point of the comparison; MEM-02 already prescribes ref access, and this measures what
// choosing the copying form actually costs.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class LoopFormStructBenchmark
{
    private Entry[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new Entry[1024];
        for (var i = 0; i < values.Length; i++)
        {
            values[i].Id = i;
        }
    }

    [Benchmark(Baseline = true)]
    public long ForeachCopy()
    {
        var total = 0L;
        foreach (var entry in values.AsSpan())
        {
            total += entry.Id;
        }

        return total;
    }

    [Benchmark]
    public long ForeachRef()
    {
        var total = 0L;
        foreach (ref var entry in values.AsSpan())
        {
            total += entry.Id;
        }

        return total;
    }

    [Benchmark]
    public long ForIndexer()
    {
        var total = 0L;
        var span = values.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i].Id;
        }

        return total;
    }

    [Benchmark]
    public long ForRef()
    {
        var total = 0L;
        var span = values.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            ref var entry = ref span[i];
            total += entry.Id;
        }

        return total;
    }

    // 64 bytes: large enough that copying is not free, and a realistic record-ish size
    private struct Entry
    {
        public long Id;
        public long A;
        public long B;
        public long C;
        public long D;
        public long E;
        public long F;
        public long G;
    }
}
