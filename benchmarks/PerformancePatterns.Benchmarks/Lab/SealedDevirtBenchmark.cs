namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// DSP-01 study: devirtualization through sealed (call via an interface, single implementation)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SealedDevirtBenchmark
{
    private IAccumulator openViaInterface = default!;

    private IAccumulator sealedViaInterface = default!;

    private SealedAccumulator sealedConcrete = default!;

    [GlobalSetup]
    public void Setup()
    {
        openViaInterface = CreateOpen();
        sealedViaInterface = CreateSealed();
        sealedConcrete = new SealedAccumulator();
    }

    // Created through a runtime branch so that static analysis also sees that multiple implementations are possible
    // (this reproduces the actual reason a non-sealed type cannot be devirtualized: it may be derived from)
    private static IAccumulator CreateOpen()
        => Environment.TickCount >= int.MinValue ? new OpenAccumulator() : new DerivedAccumulator();

    private static IAccumulator CreateSealed()
        => Environment.TickCount >= int.MinValue ? new SealedAccumulator() : new OpenAccumulator();

    [Benchmark(Baseline = true)]
    public long OpenInterface()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = openViaInterface.Add(total, i);
        }

        return total;
    }

    [Benchmark]
    public long SealedInterface()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = sealedViaInterface.Add(total, i);
        }

        return total;
    }

    [Benchmark]
    public long SealedConcrete()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = sealedConcrete.Add(total, i);
        }

        return total;
    }
}

internal interface IAccumulator
{
    long Add(long total, int value);
}

internal class OpenAccumulator : IAccumulator
{
    public long Add(long total, int value) => total + value;
}

internal sealed class SealedAccumulator : IAccumulator
{
    public long Add(long total, int value) => total + value;
}

// Type proving that a derivative of OpenAccumulator really exists (the concrete reason to keep it non-sealed)
internal sealed class DerivedAccumulator : OpenAccumulator;
