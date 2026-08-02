namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// JIT-01 study: AggressiveInlining on a helper containing a loop (not inlined by default)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class InliningBenchmark
{
    private uint[] seeds = default!;

    [GlobalSetup]
    public void Setup()
    {
        seeds = new uint[1024];
        for (var i = 0; i < seeds.Length; i++)
        {
            seeds[i] = ((uint)i * 2654435761u) + 1u;
        }
    }

    [Benchmark(Baseline = true)]
    public uint DefaultPolicy()
    {
        var total = 0u;
        foreach (var seed in seeds)
        {
            total += MixDefault(seed);
        }

        return total;
    }

    [Benchmark]
    public uint Aggressive()
    {
        var total = 0u;
        foreach (var seed in seeds)
        {
            total += MixAggressive(seed);
        }

        return total;
    }

    [Benchmark]
    public uint NoInline()
    {
        var total = 0u;
        foreach (var seed in seeds)
        {
            total += MixNoInline(seed);
        }

        return total;
    }

    // Contains a loop, so the default policy will not inline it
    private static uint MixDefault(uint value)
    {
        for (var i = 0; i < 4; i++)
        {
            value = (value ^ (value << 7)) * 2654435761u;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint MixAggressive(uint value)
    {
        for (var i = 0; i < 4; i++)
        {
            value = (value ^ (value << 7)) * 2654435761u;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint MixNoInline(uint value)
    {
        for (var i = 0; i < 4; i++)
        {
            value = (value ^ (value << 7)) * 2654435761u;
        }

        return value;
    }
}
