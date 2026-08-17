namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        // Verify all variants agree before measuring (docs/benchmark-methodology.md)
        OrderedDigestSearchBenchmark.Verify();
        RefCountUncontendedBenchmark.Verify();
        RetainBatchingBenchmark.Verify();
        ContinuationChainBenchmark.Verify();
        SequenceBuilderBenchmark.Verify();
        TypeIdentityHashBenchmark.Verify();

        // Example: dotnet run -c Release -- --filter "*"
        BenchmarkSwitcher
            .FromTypes(
            [
                typeof(OrderedDigestSearchBenchmark),
                typeof(RefCountUncontendedBenchmark),
                typeof(RefCountContendedBenchmark),
                typeof(RetainBatchingBenchmark),
                typeof(ContinuationChainBenchmark),
                typeof(SequenceBuilderBenchmark),
                typeof(TypeIdentityHashBenchmark),
            ])
            .Run(args);
    }
}
