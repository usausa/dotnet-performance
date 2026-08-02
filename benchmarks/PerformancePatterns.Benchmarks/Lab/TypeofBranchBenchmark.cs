namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// JIT-03 study: does the JIT fold a typeof(T) branch inside a generic down to zero cost
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeofBranchBenchmark
{
    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[1024];
        for (var i = 0; i < 1024; i++)
        {
            values[i] = i;
        }
    }

    [Benchmark(Baseline = true)]
    public long HandwrittenIntSum()
    {
        var total = 0L;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long GenericWithTypeofBranch() => SumSpecialized(values);

    private static long SumSpecialized<T>(T[] source)
        where T : struct
    {
        // The JIT turns the typeof(T) comparison into a constant per instantiation and removes the dead branch
        if (typeof(T) == typeof(int))
        {
            var ints = Unsafe.As<T[], int[]>(ref source);
            var total = 0L;
            foreach (var value in ints)
            {
                total += value;
            }

            return total;
        }

        // Fallback (anything other than int)
        return source.Length;
    }

    // For Verify: checks the fallback path
    public static long SumFallback(long[] source) => SumSpecialized(source);
}
