namespace PerformancePatterns.Benchmarks.Lab;

using System.Globalization;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TXT-03 study: Parse using exceptions for control flow vs TryParse (10% invalid input)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TryPatternBenchmark
{
    private const int N = 100;

    private string[] inputs = default!;

    [GlobalSetup]
    public void Setup()
    {
        // One in ten items is not a valid number
        inputs = new string[N];
        for (var i = 0; i < N; i++)
        {
            inputs[i] = (i % 10) == 9 ? "x" + i : i.ToString(CultureInfo.InvariantCulture);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public long ExceptionControlFlow()
    {
        var total = 0L;
        foreach (var input in inputs)
        {
            try
            {
                total += int.Parse(input, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                total -= 1;
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long TryPattern()
    {
        var total = 0L;
        foreach (var input in inputs)
        {
            if (int.TryParse(input, CultureInfo.InvariantCulture, out var value))
            {
                total += value;
            }
            else
            {
                total -= 1;
            }
        }

        return total;
    }
}
