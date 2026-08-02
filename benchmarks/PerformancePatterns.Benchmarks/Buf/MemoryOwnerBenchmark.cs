namespace PerformancePatterns.Benchmarks.Buf;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Buf;

// BUF-04 実装例: 4 KB バッファの取得 → 書き込み → 集計 → 解放のライフサイクルを比較
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class MemoryOwnerBenchmark
{
    private const int Size = 4096;

    [Benchmark(Baseline = true)]
    public long NewArray()
    {
        var buffer = new byte[Size];
        return FillAndSum(buffer);
    }

    [Benchmark]
    public long ArrayPoolRaw()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Size);
        try
        {
            return FillAndSum(buffer.AsSpan(0, Size));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    [Benchmark]
    public long MemoryOwnerAllocate()
    {
        using var owner = MemoryOwner<byte>.Allocate(Size);
        return FillAndSum(owner.Span);
    }

    [Benchmark]
    public long TemporaryBufferPooled()
    {
        using var buffer = new TemporaryBuffer<byte>(Size);
        return FillAndSum(buffer.Span);
    }

    private static long FillAndSum(Span<byte> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = (byte)i;
        }

        var total = 0L;
        foreach (var b in span)
        {
            total += b;
        }

        return total;
    }
}
