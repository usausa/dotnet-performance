namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study: CON-01 Interlocked one-shot guard
// Question: for preventing repeated Dispose, does Interlocked have a clear advantage over the alternatives
// (plain bool / volatile bool / lock)? The measurement is the steady-state path of calling again on an already disposed instance.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class DisposeGuardBenchmark
{
    private const int N = 100;

#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    private bool boolFlag = true;

    private volatile bool volatileFlag = true;

    private bool lockedFlag = true;

    private int interlockedFlag = 1;

    // Not thread safe (baseline for a type that assumes a single thread)
    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int PlainBool()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            if (boolFlag)
            {
                count++;
                continue;
            }

            boolFlag = true;
        }

        return count;
    }

    // Guarantees visibility only (no exactly-once guarantee)
    [Benchmark(OperationsPerInvoke = N)]
    public int VolatileBool()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            if (volatileFlag)
            {
                count++;
                continue;
            }

            volatileFlag = true;
        }

        return count;
    }

    // Mutual exclusion with lock (thread safe, exactly once)
    [Benchmark(OperationsPerInvoke = N)]
    public int LockGuard()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            lock (sync)
            {
                if (lockedFlag)
                {
                    count++;
                    continue;
                }

                lockedFlag = true;
            }
        }

        return count;
    }

    // Interlocked CAS (thread safe, exactly once)
    [Benchmark(OperationsPerInvoke = N)]
    public int InterlockedCas()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            if (Interlocked.CompareExchange(ref interlockedFlag, 1, 0) != 0)
            {
                count++;
            }
        }

        return count;
    }

    // Interlocked Exchange (thread safe, exactly once)
    [Benchmark(OperationsPerInvoke = N)]
    public int InterlockedExchange()
    {
        var count = 0;
        for (var i = 0; i < N; i++)
        {
            if (Interlocked.Exchange(ref interlockedFlag, 1) == 1)
            {
                count++;
            }
        }

        return count;
    }
}
