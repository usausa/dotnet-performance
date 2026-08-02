namespace PerformancePatterns.Benchmarks.Lab;

using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Txt;

// TXT-07 study: comparing ways to build a string whose length is known in advance
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StringBuildBenchmark
{
    private string prefix = default!;

    private string name = default!;

    private int id;

    [GlobalSetup]
    public void Setup()
    {
        prefix = new string("cache".AsSpan());
        name = new string("customer-profile".AsSpan());
        id = 12345;
    }

    [Benchmark(Baseline = true)]
    public string Interpolation() => $"{prefix}:{name}:{id}";

    [Benchmark]
    public string Concat() => string.Concat(prefix, ":", name, ":") + id;

    [Benchmark]
    public string StringBuilderCapacity()
    {
        var builder = new StringBuilder(64);
        builder.Append(prefix).Append(':').Append(name).Append(':').Append(id);
        return builder.ToString();
    }

    [Benchmark]
    public string ValueStringBuilderBuild()
    {
        using var builder = new ValueStringBuilder(stackalloc char[64]);
        builder.Append(prefix);
        builder.Append(':');
        builder.Append(name);
        builder.Append(':');

        Span<char> digits = stackalloc char[16];
        id.TryFormat(digits, out var written);
        builder.Append(digits[..written]);
        return builder.ToStringAndClear();
    }

    [Benchmark]
    public string StringCreate()
    {
        Span<char> digits = stackalloc char[16];
        id.TryFormat(digits, out var written);
        var length = prefix.Length + 1 + name.Length + 1 + written;

        // The state is passed as a tuple and the lambda is static (DSP-04)
        return string.Create(length, (prefix, name, id), static (span, state) =>
        {
            state.prefix.CopyTo(span);
            var offset = state.prefix.Length;
            span[offset++] = ':';
            state.name.CopyTo(span[offset..]);
            offset += state.name.Length;
            span[offset++] = ':';
            state.id.TryFormat(span[offset..], out _);
        });
    }
}
