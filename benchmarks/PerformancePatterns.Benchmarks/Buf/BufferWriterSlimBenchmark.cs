namespace PerformancePatterns.Benchmarks.Buf;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Buf;

// BUF-03 実装例: 逐次書き込みのライフサイクル(生成 → 16 バイト × N 書き込み → 読み出し → 破棄)を比較
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BufferWriterSlimBenchmark
{
    private byte[] chunk = default!;

    [Params(64, 4096)]
    public int TotalBytes { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        chunk = new byte[16];
        for (var i = 0; i < chunk.Length; i++)
        {
            chunk[i] = (byte)i;
        }
    }

    [Benchmark(Baseline = true)]
    public int ArrayBufferWriter()
    {
        var writer = new ArrayBufferWriter<byte>(256);
        for (var written = 0; written < TotalBytes; written += chunk.Length)
        {
            var span = writer.GetSpan(chunk.Length);
            chunk.CopyTo(span);
            writer.Advance(chunk.Length);
        }

        return Checksum(writer.WrittenSpan);
    }

    [Benchmark]
    public int PooledWriter()
    {
        using var writer = new PooledBufferWriter<byte>(256);
        for (var written = 0; written < TotalBytes; written += chunk.Length)
        {
            var span = writer.GetSpan(chunk.Length);
            chunk.CopyTo(span);
            writer.Advance(chunk.Length);
        }

        return Checksum(writer.WrittenSpan);
    }

    [Benchmark]
    public int WriterSlim()
    {
        using var writer = new BufferWriterSlim<byte>(stackalloc byte[256]);
        for (var written = 0; written < TotalBytes; written += chunk.Length)
        {
            var span = writer.GetSpan(chunk.Length);
            chunk.CopyTo(span);
            writer.Advance(chunk.Length);
        }

        return Checksum(writer.WrittenSpan);
    }

    private static int Checksum(ReadOnlySpan<byte> span)
    {
        var total = 0;
        foreach (var b in span)
        {
            total += b;
        }

        return total;
    }
}
