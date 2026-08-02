namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Immutable;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// COL-06 検証: ImmutableArray の構築方式(MoveToImmutable vs ToImmutable vs ToImmutableArray)
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

        // 容量と件数が一致していればコピーなしで確定できる
        return builder.MoveToImmutable().Length;
    }
}

// COL-06 検証: 変換先 List の確保方式(毎回 new vs 容量指定 vs Clear + EnsureCapacity で再利用)
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
