namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

// C-07 full verification: the pending path. 8 handlers that actually suspend (Task.Yield)
// joined via Task.WhenAll (AsTask + array + promise) vs the pooled IValueTaskSource.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class WhenAllPendingBenchmark
{
    private const int HandlerCount = 8;

    private Func<ValueTask>[] handlers = default!;

    private int counter;

    [GlobalSetup]
    public void Setup()
    {
        handlers = new Func<ValueTask>[HandlerCount];
        for (var i = 0; i < HandlerCount; i++)
        {
            handlers[i] = RunHandlerAsync;
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<int> TaskWhenAll()
    {
        var tasks = new Task[HandlerCount];
        for (var i = 0; i < HandlerCount; i++)
        {
            tasks[i] = handlers[i]().AsTask();
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
        return counter;
    }

    [Benchmark]
    public async Task<int> PooledSource()
    {
        await ReusableWhenAllSource.WhenAll(handlers).ConfigureAwait(false);
        return counter;
    }

    public static void Verify()
    {
        var benchmark = new WhenAllPendingBenchmark();
        benchmark.Setup();

        benchmark.counter = 0;
        benchmark.TaskWhenAll().GetAwaiter().GetResult();
        var whenAll = benchmark.counter;

        // Two consecutive runs exercise source/node pooling and version reset
        benchmark.counter = 0;
        benchmark.PooledSource().GetAwaiter().GetResult();
        benchmark.PooledSource().GetAwaiter().GetResult();
        var pooled = benchmark.counter;

        if ((whenAll != HandlerCount) || (pooled != HandlerCount * 2))
        {
            throw new InvalidOperationException($"Pending WhenAll variants disagree: {whenAll} / {pooled}.");
        }

        // The all-sync path must not rent a source
        Func<ValueTask>[] completed = [static () => default, static () => default];
        var task = ReusableWhenAllSource.WhenAll(completed);
        if (!task.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("All-sync WhenAll must complete synchronously.");
        }
    }

    private async ValueTask RunHandlerAsync()
    {
        await Task.Yield();
        Interlocked.Increment(ref counter);
    }
}
