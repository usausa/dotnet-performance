namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// ASY-06 study: creating a Timer per job vs a single-loop wake-up primitive (swapping the TCS)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SchedulerPrimitiveBenchmark
{
    private TaskCompletionSource wakeup = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Cost of registering one job: create and dispose a Timer (including enqueueing it on the timer queue)
    [Benchmark(Baseline = true)]
    public void TimerPerJob()
    {
        using var timer = new Timer(static _ => { }, null, Timeout.Infinite, Timeout.Infinite);
    }

    // Cost of signalling one job: swap in a new TCS, then complete the old one
    [Benchmark]
    public bool TcsSwapNotify()
    {
        var previous = Interlocked.Exchange(
            ref wakeup,
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        return previous.TrySetResult();
    }
}
