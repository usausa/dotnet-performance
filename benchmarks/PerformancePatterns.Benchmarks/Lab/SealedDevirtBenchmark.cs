namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// DSP-01 検証: sealed による脱仮想化(インターフェース経由呼び出し、単一実装)
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

    // 実行時分岐で生成し、静的解析にも「複数実装がありうる」ことを見せる
    // (非 sealed が devirt できない実際の条件 = 派生の可能性、をそのまま表現)
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

// OpenAccumulator に派生が実在することを示す型(非 sealed を意図的に維持するための実体)
internal sealed class DerivedAccumulator : OpenAccumulator;
