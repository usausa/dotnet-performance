namespace PerformancePatterns.Benchmarks.Lab;

using System.Text;
using System.Text.Unicode;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー③: Utf8.TryWrite(.NET 8+)
// 問い: UTF-8 補間ハンドラで Span<byte> へ直接整形すると、
// string 補間 + Encode / char 補間 TryWrite + Encode に対してどれだけ速いか。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class Utf8WriteBenchmark
{
    private int id;

    private string name = default!;

    private long timestamp;

    private byte[] buffer = default!;

    private char[] charBuffer = default!;

    // Verify 用(測定対象外)
    public byte[] CopyBuffer(int length)
    {
        var copy = new byte[length];
        buffer.AsSpan(0, length).CopyTo(copy);
        return copy;
    }

    [GlobalSetup]
    public void Setup()
    {
        id = 12345;
        name = new string("sensor-a".AsSpan());
        timestamp = 1754000000123L;
        buffer = new byte[128];
        charBuffer = new char[128];
    }

    [Benchmark(Baseline = true)]
    public int StringInterpolationEncode()
    {
        var text = $"id={id}&name={name}&ts={timestamp}";
        return Encoding.UTF8.GetBytes(text.AsSpan(), buffer);
    }

    [Benchmark]
    public int CharTryWriteEncode()
    {
        charBuffer.AsSpan().TryWrite($"id={id}&name={name}&ts={timestamp}", out var written);
        return Encoding.UTF8.GetBytes(charBuffer.AsSpan(0, written), buffer);
    }

    [Benchmark]
    public int Utf8TryWrite()
    {
        Utf8.TryWrite(buffer, $"id={id}&name={name}&ts={timestamp}", out var written);
        return written;
    }
}
