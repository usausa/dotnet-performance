namespace PerformancePatterns.Benchmarks.Seq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Seq;

// SEQ-04 / STK-03 実装例: 1024 要素を 100 件ずつのチャンクへ分割して合計する
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BatchBenchmark
{
    private const int ChunkSize = 100;

    private int[] source = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new int[1024];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = i;
        }
    }

    [Benchmark(Baseline = true)]
    public long LinqChunk()
    {
        var total = 0L;
        foreach (var chunk in source.Chunk(ChunkSize))
        {
            foreach (var value in chunk)
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark]
    public long ArrayBatch()
    {
        var total = 0L;
        foreach (var segment in source.Batch(ChunkSize))
        {
            foreach (var value in segment.AsSpan())
            {
                total += value;
            }
        }

        return total;
    }

    [Benchmark]
    public long SpanBatch()
    {
        var total = 0L;
        foreach (var chunk in source.AsSpan().Batch(ChunkSize))
        {
            foreach (var value in chunk)
            {
                total += value;
            }
        }

        return total;
    }
}
