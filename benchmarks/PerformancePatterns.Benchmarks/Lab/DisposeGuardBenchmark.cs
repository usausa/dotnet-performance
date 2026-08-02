namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証: CON-01 Interlocked ワンショットガード
// 問い: Dispose 多重実行防止として、Interlocked は他方式(素の bool / volatile bool / lock)に対して
// 明確なメリットがあるか。測定は「破棄済みインスタンスへの再呼び出し」の定常パス。
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

    // スレッド安全なし(単一スレッド前提の型の基準値)
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

    // 可視性のみ保証(正確に 1 回の保証はない)
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

    // lock による排他(スレッド安全・正確に 1 回)
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

    // Interlocked CAS(スレッド安全・正確に 1 回)
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

    // Interlocked Exchange(スレッド安全・正確に 1 回)
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
