namespace PerformancePatterns.Benchmarks.Lab;

using System.Reflection;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-03 検証: 非公開フィールドへのアクセス(UnsafeAccessor vs リフレクション)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class UnsafeAccessorBenchmark
{
    private const int N = 100;

    private static readonly FieldInfo CountField =
        typeof(AccessTarget).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private AccessTarget target = default!;

    [GlobalSetup]
    public void Setup() => target = new AccessTarget(42);

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public long PublicProperty()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += target.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long UnsafeAccessorField()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += GetCount(target);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long ReflectionGetValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            // FieldInfo.GetValue は値型をボックス化して返す
            total += (int)CountField.GetValue(target)!;
        }

        return total;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "count")]
    private static extern ref int GetCount(AccessTarget instance);
}

// 非公開フィールドを持つ対象型(外部ライブラリの内部状態を想定)。
// Count は通常アクセスの基準用で、count はアクセサ対象の非公開フィールド(独立に保持)
internal sealed class AccessTarget
{
    private readonly int count;

    public AccessTarget(int value)
    {
        count = value;
        Count = value;
    }

    public int Count { get; }

    // count の C# 側からの読み出し(主用途は UnsafeAccessor / リフレクション経由)
    public int GetCountDirect() => count;
}
