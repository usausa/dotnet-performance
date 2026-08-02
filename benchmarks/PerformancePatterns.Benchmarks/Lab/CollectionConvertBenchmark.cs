namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Immutable;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// COL-06 study: ways to build an ImmutableArray (MoveToImmutable vs ToImmutable vs ToImmutableArray)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ImmutableBuildBenchmark
{
    private int[] source = default!;

    [Params(16, 256)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = new int[Count];
        for (var i = 0; i < Count; i++)
        {
            source[i] = i;
        }
    }

    [Benchmark(Baseline = true)]
    public int ToImmutableArrayExtension() => source.ToImmutableArray().Length;

    [Benchmark]
    public int BuilderToImmutable()
    {
        var builder = ImmutableArray.CreateBuilder<int>(Count);
        for (var i = 0; i < source.Length; i++)
        {
            builder.Add(source[i]);
        }

        return builder.ToImmutable().Length;
    }

    [Benchmark]
    public int BuilderMoveToImmutable()
    {
        var builder = ImmutableArray.CreateBuilder<int>(Count);
        for (var i = 0; i < source.Length; i++)
        {
            builder.Add(source[i]);
        }

        // When capacity and count match, the array can be finalized without a copy
        return builder.MoveToImmutable().Length;
    }
}

// COL-06 study: ways to allocate the destination List (new every time vs a preset capacity vs reuse with Clear + EnsureCapacity)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ListReuseBenchmark
{
    private int[] source = default!;

    private List<int> reused = default!;

    [Params(16, 256)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = new int[Count];
        for (var i = 0; i < Count; i++)
        {
            source[i] = i;
        }

        reused = [];
        reused.EnsureCapacity(Count);
    }

    [Benchmark(Baseline = true)]
    public int NewListNoCapacity()
    {
        var list = new List<int>();
        for (var i = 0; i < source.Length; i++)
        {
            list.Add(source[i]);
        }

        return list.Count;
    }

    [Benchmark]
    public int NewListWithCapacity()
    {
        var list = new List<int>(Count);
        for (var i = 0; i < source.Length; i++)
        {
            list.Add(source[i]);
        }

        return list.Count;
    }

    [Benchmark]
    public int ReuseWithClear()
    {
        reused.Clear();
        reused.EnsureCapacity(Count);
        for (var i = 0; i < source.Length; i++)
        {
            reused.Add(source[i]);
        }

        return reused.Count;
    }

    [Benchmark]
    public int ReuseWithSetCountSpan()
    {
        reused.Clear();
        CollectionsMarshal.SetCount(reused, Count);
        var span = CollectionsMarshal.AsSpan(reused);
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = source[i];
        }

        return reused.Count;
    }
}
