namespace PerformancePatterns.Benchmarks.Txt;

using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Txt;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ValueStringBuilderBenchmark
{
    private string part1 = default!;

    private string part2 = default!;

    private string part3 = default!;

    private string part4 = default!;

    [GlobalSetup]
    public void Setup()
    {
        // Built at run time so it is not interned. 24 chars x 4 = 96 chars (fits in the 128-char initial buffer)
        part1 = new string('a', 24);
        part2 = new string('b', 24);
        part3 = new string('c', 24);
        part4 = new string('d', 24);
    }

    [Benchmark(Baseline = true)]
    public string StringBuilderDefault()
    {
        var builder = new StringBuilder();
        builder.Append(part1);
        builder.Append(part2);
        builder.Append(part3);
        builder.Append(part4);
        return builder.ToString();
    }

    [Benchmark]
    public string StringBuilderCapacity()
    {
        var builder = new StringBuilder(128);
        builder.Append(part1);
        builder.Append(part2);
        builder.Append(part3);
        builder.Append(part4);
        return builder.ToString();
    }

    [Benchmark]
    public string InterpolatedHandler()
    {
        var handler = new DefaultInterpolatedStringHandler(0, 4, null, stackalloc char[128]);
        handler.AppendFormatted(part1);
        handler.AppendFormatted(part2);
        handler.AppendFormatted(part3);
        handler.AppendFormatted(part4);
        return handler.ToStringAndClear();
    }

    [Benchmark]
    public string ValueStringBuilder()
    {
        using var builder = new ValueStringBuilder(stackalloc char[128]);
        builder.Append(part1);
        builder.Append(part2);
        builder.Append(part3);
        builder.Append(part4);
        return builder.ToStringAndClear();
    }
}
