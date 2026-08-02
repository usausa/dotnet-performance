namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-04 検証: ローカル関数のキャプチャ有無(デリゲート変換時のクロージャ確保)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class LocalFunctionClosureBenchmark
{
    private const int N = 100;

    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[16];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int CapturingLocalFunction()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            var target = i & 15;

            // ローカルをキャプチャするローカル関数をデリゲート化 → クロージャ確保
            bool Match(int x) => x == target;
            total += Apply(values, Match);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int StaticLocalFunction()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            var target = i & 15;

            // static ローカル関数(キャプチャ不可)+ state 引数 → デリゲートはキャッシュされる
            static bool Match(int x, int t) => x == t;
            total += Apply(values, target, Match);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Apply(int[] source, Func<int, bool> predicate)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Apply(int[] source, int state, Func<int, int, bool> predicate)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (predicate(source[i], state))
            {
                return i;
            }
        }

        return -1;
    }
}
