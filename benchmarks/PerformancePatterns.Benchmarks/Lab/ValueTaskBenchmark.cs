namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// ASY-05 study: Task vs ValueTask on a synchronously completing path (equivalent to a cache hit)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ValueTaskBenchmark
{
    private const int N = 100;

    // Task<int> cache (uses a value outside the BCL cache of -1 to 8)
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

    // Awaiting an already completed task is the real shape of a cache hit (the synchronous completion path)
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
