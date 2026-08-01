namespace PerformancePatterns.Benchmarks.Lab;

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー⑤: SIMD 実装例(Vector<T> / Vector256)
// 問い: 集計処理の明示的 SIMD 化の効果と、BCL(Enumerable.Sum)の既存ベクトル化との位置関係。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class VectorSumBenchmark
{
    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[4096];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i % 1000;
        }
    }

    [Benchmark(Baseline = true)]
    public int ScalarSum()
    {
        var total = 0;
        var span = values.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public int EnumerableSum() => values.Sum();

    [Benchmark]
    public int VectorTSum()
    {
        var span = values.AsSpan();
        var acc = Vector<int>.Zero;
        var i = 0;
        for (; i <= span.Length - Vector<int>.Count; i += Vector<int>.Count)
        {
            acc += new Vector<int>(span.Slice(i, Vector<int>.Count));
        }

        var total = Vector.Sum(acc);
        for (; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public int Vector256Sum()
    {
        if (!Vector256.IsHardwareAccelerated)
        {
            return ScalarSum();
        }

        ref var start = ref MemoryMarshal.GetArrayDataReference(values);
        var acc = Vector256<int>.Zero;
        var i = 0;
        for (; i <= values.Length - Vector256<int>.Count; i += Vector256<int>.Count)
        {
            acc += Vector256.LoadUnsafe(ref start, (nuint)i);
        }

        var total = Vector256.Sum(acc);
        for (; i < values.Length; i++)
        {
            total += Unsafe.Add(ref start, i);
        }

        return total;
    }
}
