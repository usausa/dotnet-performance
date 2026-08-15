namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

// C-06 (new pattern candidate, CON): retain/release cost without contention
// CAS retry loop vs optimistic increment-first (DeadBias)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RefCountUncontendedBenchmark
{
    private const int Operations = 1000;

    private readonly CasLoopRefCount casLoop = new();

    private readonly OptimisticRefCount optimistic = new();

    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public int CasLoop()
    {
        var total = 0;
        for (var i = 0; i < Operations; i++)
        {
            if (casLoop.TryRetain())
            {
                total++;
                casLoop.Release();
            }
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public int Optimistic()
    {
        var total = 0;
        for (var i = 0; i < Operations; i++)
        {
            if (optimistic.TryRetain())
            {
                total++;
                optimistic.Release();
            }
        }

        return total;
    }

    public static void Verify()
    {
        var casLoop = new CasLoopRefCount();
        var optimistic = new OptimisticRefCount();

        // Live object: retain succeeds and the balanced release must not dispose
        if (!casLoop.TryRetain() || !optimistic.TryRetain())
        {
            throw new InvalidOperationException("TryRetain on a live object must succeed.");
        }

        casLoop.Release();
        optimistic.Release();
        if (casLoop.Disposed || optimistic.Disposed)
        {
            throw new InvalidOperationException("Balanced retain/release must not dispose.");
        }

        // Dropping the initial reference kills the object; late retains must fail
        casLoop.Release();
        optimistic.Release();
        if (!casLoop.Disposed || !optimistic.Disposed)
        {
            throw new InvalidOperationException("Releasing the base reference must dispose.");
        }

        if (casLoop.TryRetain() || optimistic.TryRetain())
        {
            throw new InvalidOperationException("TryRetain on a dead object must fail.");
        }
    }
}

// C-06 contended shape: 4 threads hammering retain/release on the same instance
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RefCountContendedBenchmark
{
    private const int Threads = 4;

    private const int OperationsPerThread = 25_000;

    private readonly CasLoopRefCount casLoop = new();

    private readonly OptimisticRefCount optimistic = new();

    [Benchmark(Baseline = true, OperationsPerInvoke = Threads * OperationsPerThread)]
    public void CasLoop()
    {
        var target = casLoop;
        RunContended(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                if (target.TryRetain())
                {
                    target.Release();
                }
            }
        });
    }

    [Benchmark(OperationsPerInvoke = Threads * OperationsPerThread)]
    public void Optimistic()
    {
        var target = optimistic;
        RunContended(() =>
        {
            for (var i = 0; i < OperationsPerThread; i++)
            {
                if (target.TryRetain())
                {
                    target.Release();
                }
            }
        });
    }

    private static void RunContended(Action body)
    {
        var tasks = new Task[Threads];
        for (var i = 0; i < Threads; i++)
        {
            tasks[i] = Task.Run(body);
        }

        Task.WaitAll(tasks);
    }
}
