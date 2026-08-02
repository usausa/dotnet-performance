namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// ASY-05 検証: 同期完了パスの Task vs ValueTask(キャッシュヒット相当)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ValueTaskBenchmark
{
    private const int N = 100;

    // Task<int> キャッシュ(-1〜8 の BCL キャッシュに乗らない値を使う)
    private int value;

    [GlobalSetup]
    public void Setup() => value = 12345;

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public async Task<long> TaskFromResult()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += await GetTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<long> ValueTaskDirect()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += await GetValueTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<long> AsyncMethodTask()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += await GetAsyncTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<long> AsyncMethodValueTask()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += await GetAsyncValueTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    private Task<int> GetTaskAsync() => Task.FromResult(value);

    private ValueTask<int> GetValueTaskAsync() => new(value);

    // 完了済みを await する = キャッシュヒット時の実際の形(同期完了パス)
    private async Task<int> GetAsyncTaskAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return value;
    }

    private async ValueTask<int> GetAsyncValueTaskAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        return value;
    }
}
