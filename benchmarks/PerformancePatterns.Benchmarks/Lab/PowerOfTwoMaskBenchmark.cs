namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// BIT-02 study: bucket index computation with modulo (runtime size) vs mask vs modulo (constant size)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class PowerOfTwoMaskBenchmark
{
    private const int ConstSize = 64;

    private uint[] hashes = default!;

    private int size;

    private int mask;

    [GlobalSetup]
    public void Setup()
    {
        size = 64;
        mask = size - 1;
        hashes = new uint[1024];
        var value = 2166136261u;
        for (var i = 0; i < hashes.Length; i++)
        {
            // Pseudo-random hash sequence (reproducible)
            value = (value ^ (uint)i) * 16777619;
            hashes[i] = value;
        }
    }

    // Modulo by a runtime size (emits a division instruction)
    [Benchmark(Baseline = true)]
    public long RuntimeSizeModulo()
    {
        var total = 0L;
        var localSize = (uint)size;
        foreach (var hash in hashes)
        {
            total += hash % localSize;
        }

        return total;
    }

    // Power-of-two mask (a single AND instruction)
    [Benchmark]
    public long PowerOfTwoMask()
    {
        var total = 0L;
        var localMask = (uint)mask;
        foreach (var hash in hashes)
        {
            total += hash & localMask;
        }

        return total;
    }

    // Modulo by a constant size (to confirm the JIT optimizes it into an AND)
    [Benchmark]
    public long ConstSizeModulo()
    {
        var total = 0L;
        foreach (var hash in hashes)
        {
            total += hash % ConstSize;
        }

        return total;
    }
}
