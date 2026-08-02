namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// ASY-07 検証: 全体バッファリング vs チャンク逐次処理(1 MB ペイロード)
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

    // 全体を byte[] へバッファしてから処理(ReadAsByteArray 相当)
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

    // ArrayPool のチャンクで読みながら逐次処理(ピークメモリ = チャンクサイズ)
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
