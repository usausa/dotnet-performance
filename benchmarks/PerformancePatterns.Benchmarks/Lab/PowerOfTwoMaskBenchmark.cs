namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// BIT-03 検証: バケットインデックス計算の 剰余(実行時サイズ) vs マスク vs 剰余(定数サイズ)
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
            // 疑似ランダムなハッシュ列(再現可能)
            value = (value ^ (uint)i) * 16777619;
            hashes[i] = value;
        }
    }

    // 実行時サイズの剰余(除算命令が出る形)
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

    // 2 の累乗マスク(AND 1 命令)
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

    // 定数サイズの剰余(JIT が AND へ最適化することの確認用)
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
