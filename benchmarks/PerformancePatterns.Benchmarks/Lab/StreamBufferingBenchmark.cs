namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// ASY-07 study: buffering everything vs processing chunk by chunk (a 1 MB payload)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StreamBufferingBenchmark
{
    private const int PayloadSize = 1024 * 1024;

    private const int ChunkSize = 16 * 1024;

    private byte[] payload = default!;

    [GlobalSetup]
    public void Setup()
    {
        payload = new byte[PayloadSize];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)i;
        }
    }

    // Buffer everything into a byte[] before processing (equivalent to ReadAsByteArray)
    [Benchmark(Baseline = true)]
    public long FullBufferThenProcess()
    {
        using var source = new MemoryStream(payload);
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        var array = buffer.ToArray();

        var total = 0L;
        foreach (var b in array)
        {
            total += b;
        }

        return total;
    }

    // Process while reading into ArrayPool chunks (peak memory = the chunk size)
    [Benchmark]
    public long StreamingPooledChunks()
    {
        using var source = new MemoryStream(payload);
        var chunk = ArrayPool<byte>.Shared.Rent(ChunkSize);
        try
        {
            var total = 0L;
            int read;
            while ((read = source.Read(chunk, 0, ChunkSize)) > 0)
            {
                for (var i = 0; i < read; i++)
                {
                    total += chunk[i];
                }
            }

            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }
    }
}
