namespace PerformancePatterns.Benchmarks.Lab;

using System.Diagnostics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー④: 時刻・経過時間取得の低コスト化
// 問い: キャッシュ TTL・タイムアウト判定用途で DateTime.UtcNow を
// Environment.TickCount64 / Stopwatch.GetTimestamp に置き換える価値はどれだけあるか。
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
