namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// DSP-02 検証: 呼び出し抽象化 5 方式のオーバーヘッド(加算 × 1024)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public unsafe class CallAbstractionBenchmark
{
    private SealedAdder sealedAdder = default!;

    private IAdder interfaceAdder = default!;

    private AbstractAdder abstractAdder = default!;

    private Func<long, int, long> delegateAdder = default!;

    private delegate*<long, int, long> functionPointer;

    [GlobalSetup]
    public void Setup()
    {
        sealedAdder = new SealedAdder();
        interfaceAdder = CreateInterface();
        abstractAdder = CreateAbstract();
        delegateAdder = static (total, value) => total + value;
        functionPointer = &AddStatic;
    }

    // 実行時分岐で生成し、単相 devirt を過度に助けない(実利用の形に寄せる)
    private static IAdder CreateInterface()
        => Environment.TickCount >= int.MinValue ? new SealedAdder() : new AltAdder();

    private static AbstractAdder CreateAbstract()
        => Environment.TickCount >= int.MinValue ? new ConcreteAdder() : new AltConcreteAdder();

    [Benchmark(Baseline = true)]
    public long DirectSealed()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = sealedAdder.Add(total, i);
        }

        return total;
    }

    [Benchmark]
    public long ViaInterface()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = interfaceAdder.Add(total, i);
        }

        return total;
    }

    [Benchmark]
    public long ViaAbstract()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = abstractAdder.Add(total, i);
        }

        return total;
    }

    [Benchmark]
    public long ViaDelegate()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = delegateAdder(total, i);
        }

        return total;
    }

    [Benchmark]
    public long ViaFunctionPointer()
    {
        var total = 0L;
        for (var i = 0; i < 1024; i++)
        {
            total = functionPointer(total, i);
        }

        return total;
    }

    private static long AddStatic(long total, int value) => total + value;
}

internal interface IAdder
{
    long Add(long total, int value);
}

internal sealed class SealedAdder : IAdder
{
    public long Add(long total, int value) => total + value;
}

internal sealed class AltAdder : IAdder
{
    public long Add(long total, int value) => total - value;
}

internal abstract class AbstractAdder
{
    public abstract long Add(long total, int value);
}

internal sealed class ConcreteAdder : AbstractAdder
{
    public override long Add(long total, int value) => total + value;
}

internal sealed class AltConcreteAdder : AbstractAdder
{
    public override long Add(long total, int value) => total - value;
}
