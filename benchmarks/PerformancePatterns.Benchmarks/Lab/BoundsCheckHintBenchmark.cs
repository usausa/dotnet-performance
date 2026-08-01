namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー①: 末尾要素の事前アクセスによる境界チェック除去
// 問い: 長さを引数で受けるループで「末尾を先にタッチ」「符号なしガード」は
// まだ有効か。.NET 8 で有効・.NET 10 で消滅の見込みを世代比較で確認する(反パターン化想定)。
// 世代検証のため、このクラスのみ例外的に net8 ジョブを併走する。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net80)]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BoundsCheckHintBenchmark
{
    private int[] array = default!;

    private int length;

    [GlobalSetup]
    public void Setup()
    {
        array = new int[1024];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        length = array.Length;
    }

    [Benchmark(Baseline = true)]
    public int SumByLength() => SumWithExternalLength(array, length);

    [Benchmark]
    public int SumByArrayLength() => SumWithOwnLength(array);

    [Benchmark]
    public int SumWithTailTouch() => SumWithTailTouchCore(array, length);

    [Benchmark]
    public int SumWithUnsignedGuard() => SumWithGuardCore(array, length);

    // 長さを外部から受ける形: JIT は境界チェックを除去できない(基準)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithExternalLength(int[] array, int length)
    {
        var total = 0;
        for (var i = 0; i < length; i++)
        {
            total += array[i];
        }

        return total;
    }

    // array.Length を条件に使う形: 境界チェック除去が効く(理想形の参照値)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithOwnLength(int[] array)
    {
        var total = 0;
        for (var i = 0; i < array.Length; i++)
        {
            total += array[i];
        }

        return total;
    }

    // 末尾要素を先にタッチして JIT に範囲を教えるイディオム
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithTailTouchCore(int[] array, int length)
    {
        _ = array[length - 1];
        var total = 0;
        for (var i = 0; i < length; i++)
        {
            total += array[i];
        }

        return total;
    }

    // 先頭ガードで範囲を証明するイディオム(BCL throw ヘルパー)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithGuardCore(int[] array, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)array.Length);

        var total = 0;
        for (var i = 0; i < length; i++)
        {
            total += array[i];
        }

        return total;
    }
}
