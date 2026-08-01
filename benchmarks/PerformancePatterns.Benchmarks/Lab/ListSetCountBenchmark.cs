namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー②: CollectionsMarshal.SetCount(.NET 8+)
// 問い: Add ループ(容量チェック+バージョン更新×N)を SetCount + Span 直接書き込みに
// 置き換える価値はあるか。容量指定との組み合わせ効果も確認する。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ListSetCountBenchmark
{
    [Params(16, 1024)]
    public int Size { get; set; }

    [Benchmark(Baseline = true)]
    public int AddLoop()
    {
        var list = new List<int>();
        for (var i = 0; i < Size; i++)
        {
            list.Add(i);
        }

        return list.Count;
    }

    [Benchmark]
    public int AddLoopCapacity()
    {
        var list = new List<int>(Size);
        for (var i = 0; i < Size; i++)
        {
            list.Add(i);
        }

        return list.Count;
    }

    [Benchmark]
    public int SetCountSpanWrite()
    {
        var list = new List<int>();
        CollectionsMarshal.SetCount(list, Size);
        var span = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = i;
        }

        return list.Count;
    }

    [Benchmark]
    public int SetCountCapacitySpanWrite()
    {
        var list = new List<int>(Size);
        CollectionsMarshal.SetCount(list, Size);
        var span = CollectionsMarshal.AsSpan(list);
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = i;
        }

        return list.Count;
    }
}
