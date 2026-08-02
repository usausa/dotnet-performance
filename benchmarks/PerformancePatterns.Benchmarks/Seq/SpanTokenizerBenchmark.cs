namespace PerformancePatterns.Benchmarks.Seq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Seq;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SpanTokenizerBenchmark
{
    private string input = default!;

    [Params(4, 64)]
    public int Tokens { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Built at run time so it is not interned (avoids the Equals short-circuit caused by using a literal directly)
        input = string.Join(',', Enumerable.Range(0, Tokens).Select(static x => "value" + x));
    }

    [Benchmark(Baseline = true)]
    public int StringSplit()
    {
        var total = 0;
        foreach (var token in input.Split(','))
        {
            total += token.Length;
        }

        return total;
    }

    [Benchmark]
    public int SpanTokenizer()
    {
        var total = 0;
        foreach (var token in input.Tokenize(','))
        {
            total += token.Length;
        }

        return total;
    }
}

// The net8.0 build has no MemoryExtensions.Split, so the whole class is excluded (methodology pitfall 7)
#if NET9_0_OR_GREATER
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SpanTokenizerBclComparisonBenchmark
{
    private string input = default!;

    [Params(4, 64)]
    public int Tokens { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        input = string.Join(',', Enumerable.Range(0, Tokens).Select(static x => "value" + x));
    }

    [Benchmark(Baseline = true)]
    public int SpanTokenizer()
    {
        var total = 0;
        foreach (var token in input.Tokenize(','))
        {
            total += token.Length;
        }

        return total;
    }

    [Benchmark]
    public int MemoryExtensionsSplit()
    {
        var total = 0;
        var span = input.AsSpan();
        foreach (var range in span.Split(','))
        {
            total += span[range].Length;
        }

        return total;
    }
}
#endif
