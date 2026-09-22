namespace PerformancePatterns.Benchmarks;

using BenchmarkDotNet.Running;

using PerformancePatterns.Benchmarks.Lab;

public static class Program
{
    public static void Main(string[] args)
    {
        // Verify all variants agree before measuring (benchmark-methodology.md)
        StaticAbstractCallBenchmark.Verify();
        TypeHashSourceBenchmark.Verify();
        TypeKeyBenchmark.Verify();
        TypeKeyDispatchBenchmark.Verify();
        TypeKeyDispatchSweepBenchmark.Verify();

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
