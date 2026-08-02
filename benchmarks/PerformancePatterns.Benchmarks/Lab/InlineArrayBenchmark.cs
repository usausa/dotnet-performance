namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-08 検証: 構造体内固定長バッファ(InlineArray)と他の確保手段
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class InlineArrayBenchmark
{
    private const int Size = 8;

    [Benchmark(Baseline = true)]
    public int NewArray()
    {
        var buffer = new int[Size];
        for (var i = 0; i < Size; i++)
        {
            buffer[i] = i;
        }

        return Sum(buffer);
    }

    [Benchmark]
    public int Stackalloc()
    {
        Span<int> buffer = stackalloc int[Size];
        for (var i = 0; i < Size; i++)
        {
            buffer[i] = i;
        }

        return Sum(buffer);
    }

    [Benchmark]
    public int InlineArrayBuffer()
    {
        var buffer = default(Slot8);
        for (var i = 0; i < Size; i++)
        {
            buffer[i] = i;
        }

        return Sum(buffer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Sum(ReadOnlySpan<int> values)
    {
        var total = 0;
        for (var i = 0; i < values.Length; i++)
        {
            total += values[i];
        }

        return total;
    }
}

[InlineArray(8)]
public struct Slot8
{
    private int element0;
}
