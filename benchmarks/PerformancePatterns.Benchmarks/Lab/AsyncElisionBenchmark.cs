namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 4: eliding the async state machine
// Question: when simply forwarding an inner call that completes synchronously, how much is gained by returning
// the Task/ValueTask directly instead of writing async/await (including the re-wrapping of an already cached Task)?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class AsyncElisionBenchmark
{
    private const int N = 100;

    // 42 is outside the runtime's Task cache (-1 to 8), so the async wrapper re-wraps and allocates a Task on every call
    private static readonly Task<int> CompletedTask = Task.FromResult(42);

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public async Task<int> TaskAwaitForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await AwaitForwardTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<int> TaskDirectForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await DirectForwardTask().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<int> ValueTaskAwaitForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await AwaitForwardValueTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<int> ValueTaskDirectForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await DirectForwardValueTask().ConfigureAwait(false);
        }

        return total;
    }

    private static Task<int> InnerTask() => CompletedTask;

    private static ValueTask<int> InnerValueTask() => new(42);

    // ❌ async/await on a simple forward (state machine + re-wrapping the result)
    private static async Task<int> AwaitForwardTaskAsync() => await InnerTask().ConfigureAwait(false);

    // ✅ Return the Task directly (async elision)
    private static Task<int> DirectForwardTask() => InnerTask();

    private static async ValueTask<int> AwaitForwardValueTaskAsync() => await InnerValueTask().ConfigureAwait(false);

    private static ValueTask<int> DirectForwardValueTask() => InnerValueTask();
}
