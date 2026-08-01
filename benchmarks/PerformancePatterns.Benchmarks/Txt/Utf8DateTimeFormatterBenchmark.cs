namespace PerformancePatterns.Benchmarks.Txt;

using System.Globalization;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Txt;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class Utf8DateTimeFormatterBenchmark
{
    private const string Format = "yyyyMMddHHmmss";

    private DateTime value;

    private byte[] buffer = default!;

    private char[] charBuffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        value = new DateTime(2026, 8, 1, 12, 34, 56);
        buffer = new byte[32];
        charBuffer = new char[32];
    }

    [Benchmark(Baseline = true)]
    public int ToStringEncode()
    {
        var text = value.ToString(Format, CultureInfo.InvariantCulture);
        return Encoding.ASCII.GetBytes(text.AsSpan(), buffer);
    }

    [Benchmark]
    public int TryFormatEncode()
    {
        value.TryFormat(charBuffer, out var written, Format, CultureInfo.InvariantCulture);
        return Encoding.ASCII.GetBytes(charBuffer.AsSpan(0, written), buffer);
    }

    [Benchmark]
    public int TableFormat()
    {
        Utf8DateTimeFormatter.TryFormat(value, buffer, out var written);
        return written;
    }
}
