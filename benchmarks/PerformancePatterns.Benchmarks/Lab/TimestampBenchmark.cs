namespace PerformancePatterns.Benchmarks.Lab;

using System.Diagnostics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 4: cheaper ways to read the current time and elapsed time
// Question: for cache TTL and timeout checks, how much is gained by replacing DateTime.UtcNow with
// Environment.TickCount64 / Stopwatch.GetTimestamp?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TimestampBenchmark
{
    private const int N = 100;

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public long DateTimeUtcNow()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += DateTime.UtcNow.Ticks;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long DateTimeOffsetUtcNow()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += DateTimeOffset.UtcNow.Ticks;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long EnvironmentTickCount64()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += Environment.TickCount64;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long StopwatchGetTimestamp()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += Stopwatch.GetTimestamp();
        }

        return total;
    }
}
