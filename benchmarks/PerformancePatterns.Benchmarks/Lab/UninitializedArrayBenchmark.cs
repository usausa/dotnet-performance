namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー①: GC.AllocateUninitializedArray<T>
// 問い: ゼロ初期化スキップの効果が出るサイズ閾値はどこか(小サイズはランタイムが通常確保にフォールバックする)。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class UninitializedArrayBenchmark
{
    [Params(256, 2048, 4096, 65536, 1048576)]
    public int Size { get; set; }

    [Benchmark(Baseline = true)]
    public byte NewArray()
    {
        var buffer = new byte[Size];
        buffer[0] = 1;
        buffer[^1] = 2;
        return (byte)(buffer[0] + buffer[^1]);
    }

    [Benchmark]
    public byte AllocateUninitialized()
    {
        var buffer = GC.AllocateUninitializedArray<byte>(Size);
        buffer[0] = 1;
        buffer[^1] = 2;
        return (byte)(buffer[0] + buffer[^1]);
    }
}
