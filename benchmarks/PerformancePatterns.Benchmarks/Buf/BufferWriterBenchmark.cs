namespace PerformancePatterns.Benchmarks.Buf;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Buf;

// BUF-02 example: moving output assembly from MemoryStream accumulation to direct IBufferWriter writes
// (scenario: write 64 chunks of 16 bytes, then finalize the content)
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
