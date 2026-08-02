namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-05 検証: object 境界のボックス化 vs 頻出値の事前ボックスキャッシュ
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BoxingCacheBenchmark
{
    private const int N = 100;

    private static readonly object BoxedMinusOne = -1;

    private static readonly object BoxedZero = 0;

    private static readonly object BoxedOne = 1;

    private int[] values = default!;

    private object?[] sink = default!;

    [GlobalSetup]
    public void Setup()
    {
        // フラグ・戻り値コードに典型的な -1 / 0 / 1 の列
        values = new int[N];
        for (var i = 0; i < N; i++)
        {
            values[i] = (i % 3) - 1;
        }

        sink = new object?[N];
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int DirectBoxing()
    {
        for (var i = 0; i < N; i++)
        {
            // 格納先が object[] のためエスケープし、毎回ヒープへボックス化される
            sink[i] = values[i];
        }

        return Unbox(sink);
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int CachedBox()
    {
        for (var i = 0; i < N; i++)
        {
            sink[i] = values[i] switch
            {
                -1 => BoxedMinusOne,
                0 => BoxedZero,
                1 => BoxedOne,
                var other => other,   // キャッシュ外のみボックス化
            };
        }

        return Unbox(sink);
    }

    private static int Unbox(object?[] boxed)
    {
        var total = 0;
        foreach (var value in boxed)
        {
            total += (int)value!;
        }

        return total;
    }
}
