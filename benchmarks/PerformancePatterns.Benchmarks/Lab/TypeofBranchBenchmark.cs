namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// JIT-03 検証: ジェネリック内の typeof(T) 分岐は JIT が畳み込みゼロコストになるか
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeofBranchBenchmark
{
    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[1024];
        for (var i = 0; i < 1024; i++)
        {
            values[i] = i;
        }
    }

    [Benchmark(Baseline = true)]
    public long HandwrittenIntSum()
    {
        var total = 0L;
        foreach (var value in values)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long GenericWithTypeofBranch() => SumSpecialized(values);

    private static long SumSpecialized<T>(T[] source)
        where T : struct
    {
        // typeof(T) 比較は JIT がインスタンス化ごとに定数化し、不要な側の分岐を消す
        if (typeof(T) == typeof(int))
        {
            var ints = Unsafe.As<T[], int[]>(ref source);
            var total = 0L;
            foreach (var value in ints)
            {
                total += value;
            }

            return total;
        }

        // フォールバック(int 以外)
        return source.Length;
    }

    // Verify 用: フォールバック経路の確認
    public static long SumFallback(long[] source) => SumSpecialized(source);
}
