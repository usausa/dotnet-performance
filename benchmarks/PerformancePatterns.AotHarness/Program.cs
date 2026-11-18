namespace PerformancePatterns.Benchmarks;

using BenchmarkDotNet.Running;

using PerformancePatterns.Benchmarks.Lab;

public static class Program
{
    public static void Main(string[] args)
    {
        // Verify all variants agree before measuring (benchmark-methodology.md)
        NullableBoxingBenchmark.Verify();
        GenericEnumEqualsBenchmark.Verify();
        ComparerDevirtBenchmark.Verify();
        GenericVirtualDevirtBenchmark.Verify();
        ArrayCovarianceBenchmark.Verify();
        BitCastStructVectorBenchmark.Verify();
        SpanSortComparerBenchmark.Verify();
        ReflectionConstructBenchmark.Verify();
        ReflectionAccessorBenchmark.Verify();
        StaticAbstractCallBenchmark.Verify();
        TypeHashSourceBenchmark.Verify();
        TypeKeyBenchmark.Verify();
        TypeKeyDispatchBenchmark.Verify();
        TypeKeyDispatchSweepBenchmark.Verify();

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
