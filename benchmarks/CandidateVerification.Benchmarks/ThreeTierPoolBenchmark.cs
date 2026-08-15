namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

// C-01 (BUF-07 update candidate): rent/return cost of queue-only vs ThreadStatic-only vs CAS fast-slot vs 3-tier pools
// (the ThreadStatic-only variant measures the extra cost tiers 2/3 add for same-thread workloads)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ThreeTierPoolBenchmark
{
    private const int Operations = 1000;

    [GlobalSetup]
    public void Setup()
    {
        // Prime each pool so the benchmark measures steady-state rent/return
        QueueOnlyPool<PooledContext>.Return(new PooledContext());
        ThreadStaticOnlyPool<PooledContext>.Return(new PooledContext());
        TwoTierPool<PooledContext>.Return(new PooledContext());
        ThreeTierPool<PooledContext>.Return(new PooledContext());
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public int ThreadStaticOnly()
    {
        var total = 0;
        for (var i = 0; i < Operations; i++)
        {
            var context = ThreadStaticOnlyPool<PooledContext>.Rent();
            total += context.Touch();
            ThreadStaticOnlyPool<PooledContext>.Return(context);
        }

        return total;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public int QueueOnly()
    {
        var total = 0;
        for (var i = 0; i < Operations; i++)
        {
            var context = QueueOnlyPool<PooledContext>.Rent();
            total += context.Touch();
            QueueOnlyPool<PooledContext>.Return(context);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public int TwoTier()
    {
        var total = 0;
        for (var i = 0; i < Operations; i++)
        {
            var context = TwoTierPool<PooledContext>.Rent();
            total += context.Touch();
            TwoTierPool<PooledContext>.Return(context);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public int ThreeTier()
    {
        var total = 0;
        for (var i = 0; i < Operations; i++)
        {
            var context = ThreeTierPool<PooledContext>.Rent();
            total += context.Touch();
            ThreeTierPool<PooledContext>.Return(context);
        }

        return total;
    }

    public static void Verify()
    {
        var context = ThreeTierPool<PooledContext>.Rent();
        ThreeTierPool<PooledContext>.Return(context);
        if (!ReferenceEquals(ThreeTierPool<PooledContext>.Rent(), context))
        {
            throw new InvalidOperationException("ThreeTierPool must return the thread-local instance.");
        }

        ThreeTierPool<PooledContext>.Return(context);

        var benchmark = new ThreeTierPoolBenchmark();
        benchmark.Setup();
        if ((benchmark.QueueOnly() <= 0) || (benchmark.ThreadStaticOnly() <= 0) ||
            (benchmark.TwoTier() <= 0) || (benchmark.ThreeTier() <= 0))
        {
            throw new InvalidOperationException("Pool variants must complete rent/return cycles.");
        }
    }
}

public sealed class PooledContext
{
    private int counter;

    public int Touch() => ++counter;
}
