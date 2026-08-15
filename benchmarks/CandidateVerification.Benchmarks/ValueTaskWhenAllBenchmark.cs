namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// C-07 (ASY-05 update candidate): fan-out completion over 8 synchronously-completing handlers.
// Preliminary scope: only the all-sync path (the pooled IValueTaskSource pending path is deferred to full verification).
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ValueTaskWhenAllBenchmark
{
    private const int HandlerCount = 8;

    private Func<ValueTask>[] handlers = default!;

    private long counter;

    [GlobalSetup]
    public void Setup()
    {
        handlers = new Func<ValueTask>[HandlerCount];
        for (var i = 0; i < HandlerCount; i++)
        {
            handlers[i] = () =>
            {
                counter++;
                return default;
            };
        }
    }

    [Benchmark(Baseline = true)]
    public async Task<long> TaskWhenAll()
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
    public async Task<long> SequentialAwait()
    {
        await PublishSequentialAsync(handlers).ConfigureAwait(false);
        return counter;
    }

    [Benchmark]
    public Task<long> FoldFastPath()
    {
        PublishFold(handlers);
        return Task.FromResult(counter);
    }

    public static void Verify()
    {
        var benchmark = new ValueTaskWhenAllBenchmark();
        benchmark.Setup();

        benchmark.counter = 0;
        benchmark.TaskWhenAll().GetAwaiter().GetResult();
        var whenAll = benchmark.counter;

        benchmark.counter = 0;
        benchmark.SequentialAwait().GetAwaiter().GetResult();
        var sequential = benchmark.counter;

        benchmark.counter = 0;
        benchmark.FoldFastPath().GetAwaiter().GetResult();
        var fold = benchmark.counter;

        if ((whenAll != HandlerCount) || (sequential != HandlerCount) || (fold != HandlerCount))
        {
            throw new InvalidOperationException($"WhenAll variants disagree: {whenAll} / {sequential} / {fold}.");
        }
    }

    private static async ValueTask PublishSequentialAsync(Func<ValueTask>[] handlers)
    {
        foreach (var handler in handlers)
        {
            await handler().ConfigureAwait(false);
        }
    }

    private static void PublishFold(Func<ValueTask>[] handlers)
    {
        // Count pending tasks first; the all-sync case allocates nothing at all
        var pendingCount = 0;
        foreach (var handler in handlers)
        {
            var task = handler();
            if (task.IsCompletedSuccessfully)
            {
                task.GetAwaiter().GetResult();
            }
            else
            {
                pendingCount++;
            }
        }

        if (pendingCount == 0)
        {
            return;
        }

        // Multiple pending tasks would rent a pooled IValueTaskSource here (full verification scope)
        throw new NotSupportedException("Preliminary benchmark covers the synchronous completion path only.");
    }
}
