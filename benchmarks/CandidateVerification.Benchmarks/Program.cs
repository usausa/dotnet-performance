namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        // Verify all variants agree before measuring (docs/benchmark-methodology.md)
        ThreeTierPoolBenchmark.Verify();
        CrossThreadPoolBenchmark.Verify();
        FreeListBenchmark.Verify();
        RobinHoodTypeTableBenchmark.Verify();
        OrderedDigestSearchBenchmark.Verify();
        GuardedDevirtBenchmark.Verify();
        RefCountUncontendedBenchmark.Verify();
        RetainBatchingBenchmark.Verify();
        ValueTaskWhenAllBenchmark.Verify();
        WhenAllPendingBenchmark.Verify();
        ContinuationChainBenchmark.Verify();
        SequenceBuilderBenchmark.Verify();
        TempSplitBenchmark.Verify();
        TypeIdentityHashBenchmark.Verify();

        // Example: dotnet run -c Release -- --filter "*"
        BenchmarkSwitcher
            .FromTypes(
            [
                typeof(ThreeTierPoolBenchmark),
                typeof(CrossThreadPoolBenchmark),
                typeof(FreeListBenchmark),
                typeof(RobinHoodTypeTableBenchmark),
                typeof(OrderedDigestSearchBenchmark),
                typeof(GuardedDevirtBenchmark),
                typeof(RefCountUncontendedBenchmark),
                typeof(RefCountContendedBenchmark),
                typeof(RetainBatchingBenchmark),
                typeof(ValueTaskWhenAllBenchmark),
                typeof(WhenAllPendingBenchmark),
                typeof(ContinuationChainBenchmark),
                typeof(SequenceBuilderBenchmark),
                typeof(TempSplitBenchmark),
                typeof(TypeIdentityHashBenchmark),
            ])
            .Run(args);
    }
}
