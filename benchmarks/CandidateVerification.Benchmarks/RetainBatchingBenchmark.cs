namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

// C-06 full verification (2): run-length retain batching for range-scan results.
// 128 consecutive rows over 4 pages: retain per row vs retain once per page run.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RetainBatchingBenchmark
{
    private const int Rows = 128;

    private const int Pages = 4;

    private readonly OptimisticRefCount[] pages = new OptimisticRefCount[Pages];

    private readonly OptimisticRefCount[] retainedPages = new OptimisticRefCount[Pages];

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < Pages; i++)
        {
            pages[i] = new OptimisticRefCount();
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Rows)]
    public int RetainPerRow()
    {
        var total = 0;
        for (var i = 0; i < Rows; i++)
        {
            if (pages[i >> 5].TryRetain())
            {
                total++;
            }
        }

        for (var i = 0; i < Rows; i++)
        {
            pages[i >> 5].Release();
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Rows)]
    public int RetainPerRun()
    {
        var total = 0;
        var retained = 0;
        OptimisticRefCount? last = null;
        for (var i = 0; i < Rows; i++)
        {
            var page = pages[i >> 5];
            if (!ReferenceEquals(page, last))
            {
                if (page.TryRetain())
                {
                    retainedPages[retained] = page;
                    retained++;
                }

                last = page;
            }

            total++;
        }

        for (var i = 0; i < retained; i++)
        {
            retainedPages[i].Release();
            retainedPages[i] = null!;
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new RetainBatchingBenchmark();
        benchmark.Setup();
        if ((benchmark.RetainPerRow() != Rows) || (benchmark.RetainPerRun() != Rows))
        {
            throw new InvalidOperationException("Retain batching variants disagree.");
        }

        foreach (var page in benchmark.pages)
        {
            if (page.Disposed)
            {
                throw new InvalidOperationException("Balanced retain/release must not dispose pages.");
            }
        }
    }
}
