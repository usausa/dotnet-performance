namespace PerformancePatterns.Benchmarks.Buf;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Buf;

// BUF-02 実装例: 出力の組み立てを MemoryStream 蓄積から IBufferWriter 直接書き込みへ
// (16 バイトのチャンクを 64 回書き、最終的な内容を確定するシナリオ)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BufferWriterBenchmark
{
    private const int ChunkSize = 16;

    private const int ChunkCount = 64;

    private byte[] chunk = default!;

    [GlobalSetup]
    public void Setup()
    {
        chunk = new byte[ChunkSize];
        for (var i = 0; i < chunk.Length; i++)
        {
            chunk[i] = (byte)(i + 1);
        }
    }

    [Benchmark(Baseline = true)]
    public int MemoryStreamWrite()
    {
        using var stream = new MemoryStream();
        for (var i = 0; i < ChunkCount; i++)
        {
            stream.Write(chunk, 0, chunk.Length);
        }

        return stream.ToArray().Length;
    }

    [Benchmark]
    public int ArrayBufferWriterWrite()
    {
        var writer = new System.Buffers.ArrayBufferWriter<byte>();
        for (var i = 0; i < ChunkCount; i++)
        {
            var span = writer.GetSpan(ChunkSize);
            chunk.CopyTo(span);
            writer.Advance(ChunkSize);
        }

        return writer.WrittenSpan.Length;
    }

    [Benchmark]
    public int PooledBufferWriterWrite()
    {
        using var writer = new PooledBufferWriter<byte>();
        for (var i = 0; i < ChunkCount; i++)
        {
            var span = writer.GetSpan(ChunkSize);
            chunk.CopyTo(span);
            writer.Advance(ChunkSize);
        }

        return writer.WrittenSpan.Length;
    }
}
