namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-03 検証: stackalloc のゼロ初期化コストと SkipLocalsInit によるスキップ
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SkipLocalsInitBenchmark
{
    private const int N = 16;

    [Params(512, 4096)]
    public int Size { get; set; }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int ZeroInit()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += ZeroInitCore(Size);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int SkipInit()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += SkipInitCore(Size);
        }

        return total;
    }

    // 既定: stackalloc はゼロ初期化される
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ZeroInitCore(int size)
    {
        Span<byte> buffer = stackalloc byte[4096];
        var span = buffer[..size];
        span[0] = 1;
        span[^1] = 2;
        return span[0] + span[^1];
    }

    // SkipLocalsInit: ゼロ初期化を省く(書き込んだ位置しか読まないことが前提)
    [SkipLocalsInit]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SkipInitCore(int size)
    {
        Span<byte> buffer = stackalloc byte[4096];
        var span = buffer[..size];
        span[0] = 1;
        span[^1] = 2;
        return span[0] + span[^1];
    }
}
