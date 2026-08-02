namespace PerformancePatterns.Benchmarks.Lab;

using System.Numerics;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 4: making use of BitOperations
// Question: what is gained by replacing naive loops for bitmap scanning and bit counting with
// TrailingZeroCount / PopCount (hardware instructions)?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BitOperationsBenchmark
{
    private ulong[] masks = default!;

    [GlobalSetup]
    public void Setup()
    {
        // Sparse bitmaps with 7 bits set each (a deterministic pattern)
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

    // Walk only the set bits: take the lowest set bit position, then clear it with mask &= mask - 1
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
