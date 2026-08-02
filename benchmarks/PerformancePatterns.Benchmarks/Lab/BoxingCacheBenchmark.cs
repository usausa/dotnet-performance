namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-05 study: boxing at an object boundary vs a pre-boxed cache of frequent values
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BoxingCacheBenchmark
{
    private const int N = 100;

    private static readonly object BoxedMinusOne = -1;

    private static readonly object BoxedZero = 0;

    private static readonly object BoxedOne = 1;

    private int[] values = default!;

    private object?[] sink = default!;

    [GlobalSetup]
    public void Setup()
    {
        // A sequence of -1 / 0 / 1, typical of flags and return codes
        values = new int[N];
        for (var i = 0; i < N; i++)
        {
            values[i] = (i % 3) - 1;
        }

        sink = new object?[N];
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int DirectBoxing()
    {
        for (var i = 0; i < N; i++)
        {
            // The destination is object[], so the value escapes and is boxed on the heap every time
            sink[i] = values[i];
        }

        return Unbox(sink);
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int CachedBox()
    {
        for (var i = 0; i < N; i++)
        {
            sink[i] = values[i] switch
            {
                -1 => BoxedMinusOne,
                0 => BoxedZero,
                1 => BoxedOne,
                var other => other,   // Only values outside the cache are boxed
            };
        }

        return Unbox(sink);
    }

    private static int Unbox(object?[] boxed)
    {
        var total = 0;
        foreach (var value in boxed)
        {
            total += (int)value!;
        }

        return total;
    }
}
