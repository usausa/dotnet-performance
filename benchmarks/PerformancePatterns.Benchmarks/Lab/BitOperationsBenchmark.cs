namespace PerformancePatterns.Benchmarks.Lab;

using System.Numerics;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー④: BitOperations 活用
// 問い: ビットマップ走査・ビット数計測を素朴なループから
// TrailingZeroCount / PopCount(ハードウェア命令)へ置き換える効果。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BitOperationsBenchmark
{
    private ulong[] masks = default!;

    [GlobalSetup]
    public void Setup()
    {
        // 各 7 ビット立った疎なビットマップ(決定的パターン)
        masks = new ulong[64];
        for (var i = 0; i < masks.Length; i++)
        {
            masks[i] = BitOperations.RotateLeft(0x0001020408102040UL, i);
        }
    }

    [Benchmark(Baseline = true)]
    public int SetBitScanLoop()
    {
        var total = 0;
        foreach (var mask in masks)
        {
            total += ScanLoop(mask);
        }

        return total;
    }

    [Benchmark]
    public int SetBitScanTzcnt()
    {
        var total = 0;
        foreach (var mask in masks)
        {
            total += ScanTzcnt(mask);
        }

        return total;
    }

    [Benchmark]
    public int PopCountManual()
    {
        var total = 0;
        foreach (var mask in masks)
        {
            total += CountLoop(mask);
        }

        return total;
    }

    [Benchmark]
    public int PopCountIntrinsic()
    {
        var total = 0;
        foreach (var mask in masks)
        {
            total += BitOperations.PopCount(mask);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ScanLoop(ulong mask)
    {
        var total = 0;
        for (var bit = 0; bit < 64; bit++)
        {
            if (((mask >> bit) & 1UL) != 0UL)
            {
                total += bit;
            }
        }

        return total;
    }

    // 立っているビットだけを辿る: 最下位ビット位置を取得し、mask &= mask - 1 で消す
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ScanTzcnt(ulong mask)
    {
        var total = 0;
        while (mask != 0UL)
        {
            total += BitOperations.TrailingZeroCount(mask);
            mask &= mask - 1;
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountLoop(ulong mask)
    {
        var count = 0;
        while (mask != 0UL)
        {
            count += (int)(mask & 1UL);
            mask >>= 1;
        }

        return count;
    }
}
