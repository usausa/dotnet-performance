namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー①: 定数サイズ stackalloc
// 問い: 定数確保+スライス(フレーム固定)と可変サイズ確保(localloc 命令)のコスト差、
// および SkipLocalsInit の有無によるゼロ初期化コストの内訳。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StackallocSizeBenchmark
{
    [Params(64, 512)]
    public int Size { get; set; }

    [Benchmark(Baseline = true)]
    public int ConstantSize()
    {
        Span<byte> buffer = stackalloc byte[512];
        var span = buffer[..Size];
        span[0] = 1;
        span[^1] = 2;
        return span[0] + span[^1];
    }

    [Benchmark]
    public int VariableSize()
    {
        Span<byte> buffer = stackalloc byte[Size];
        buffer[0] = 1;
        buffer[^1] = 2;
        return buffer[0] + buffer[^1];
    }

    [Benchmark]
    [SkipLocalsInit]
    public int ConstantSizeSkipInit()
    {
        Span<byte> buffer = stackalloc byte[512];
        var span = buffer[..Size];
        span[0] = 1;
        span[^1] = 2;
        return span[0] + span[^1];
    }

    [Benchmark]
    [SkipLocalsInit]
    public int VariableSizeSkipInit()
    {
        Span<byte> buffer = stackalloc byte[Size];
        buffer[0] = 1;
        buffer[^1] = 2;
        return buffer[0] + buffer[^1];
    }
}
