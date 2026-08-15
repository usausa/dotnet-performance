namespace CandidateVerification.Benchmarks;

using System.Collections.Concurrent;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

// C-01 full verification: rent on one thread, return on another (async-boundary shape).
// ThreadStatic-only degrades here (the renting thread never sees returned instances);
// the queue tiers are what make cross-thread reuse possible.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class CrossThreadPoolBenchmark
{
    private const int Operations = 10_000;

    [GlobalSetup]
    public void Setup()
    {
        QueueOnlyPool<PooledContext>.Return(new PooledContext());
        ThreadStaticOnlyPool<PooledContext>.Return(new PooledContext());
        ThreeTierPool<PooledContext>.Return(new PooledContext());
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public void QueueOnly()
        => RunCrossThread(
            static () => QueueOnlyPool<PooledContext>.Rent(),
            static context => QueueOnlyPool<PooledContext>.Return(context));

    [Benchmark(OperationsPerInvoke = Operations)]
    public void ThreadStaticOnly()
        => RunCrossThread(
            static () => ThreadStaticOnlyPool<PooledContext>.Rent(),
            static context => ThreadStaticOnlyPool<PooledContext>.Return(context));

    [Benchmark(OperationsPerInvoke = Operations)]
    public void ThreeTier()
        => RunCrossThread(
            static () => ThreeTierPool<PooledContext>.Rent(),
            static context => ThreeTierPool<PooledContext>.Return(context));

    public static void Verify()
    {
        var benchmark = new CrossThreadPoolBenchmark();
        benchmark.Setup();
        benchmark.QueueOnly();
        benchmark.ThreadStaticOnly();
        benchmark.ThreeTier();
    }

    private static void RunCrossThread(Func<PooledContext> rent, Action<PooledContext> ret)
    {
        var handoff = new ConcurrentQueue<PooledContext>();

        var producer = Task.Run(() =>
        {
            for (var i = 0; i < Operations; i++)
            {
                var context = rent();
                context.Touch();
                handoff.Enqueue(context);
            }
        });

        var consumer = Task.Run(() =>
        {
            var count = 0;
            while (count < Operations)
            {
                if (handoff.TryDequeue(out var context))
                {
                    ret(context);
                    count++;
                }
                else
                {
                    Thread.SpinWait(16);
                }
            }
        });

        Task.WaitAll(producer, consumer);
    }
}
