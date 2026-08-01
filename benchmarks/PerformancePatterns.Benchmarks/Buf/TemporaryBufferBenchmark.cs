namespace PerformancePatterns.Benchmarks.Buf;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Buf;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TemporaryBufferBenchmark
{
    // 64 は stackalloc 経路、4096 はプール経路の代表値
    [Params(64, 4096)]
    public int Size { get; set; }

    [Benchmark(Baseline = true)]
    public int AllocateArray()
    {
        var buffer = new byte[Size];
        buffer[0] = 1;
        buffer[^1] = 2;
        return buffer[0] + buffer[^1] + buffer.Length;
    }

    [Benchmark]
    public int TemporaryBuffer()
    {
        using var buffer = Size <= 512
            ? new TemporaryBuffer<byte>(stackalloc byte[512], Size)
            : new TemporaryBuffer<byte>(Size);
        var span = buffer.Span;
        span[0] = 1;
        span[^1] = 2;
        return span[0] + span[^1] + span.Length;
    }

    [Benchmark]
    public int ArrayPoolRent()
    {
        var buffer = ArrayPool<byte>.Shared.Rent(Size);
        try
        {
            buffer[0] = 1;
            buffer[Size - 1] = 2;
            return buffer[0] + buffer[Size - 1] + Size;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
