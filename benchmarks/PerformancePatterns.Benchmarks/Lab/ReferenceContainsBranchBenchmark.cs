namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー①: RuntimeHelpers.IsReferenceOrContainsReferences<T> 分岐
// 問い: 参照を含まない T でクリア処理をスキップする分岐は JIT に定数畳み込みされ、
// 「チェックコストゼロで不要な仕事を消せる」か(プール返却時のクリア等の定石)。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ReferenceContainsBranchBenchmark
{
    private const int Size = 1024;

    private int[] intArray = default!;

    private string[] stringArray = default!;

    [GlobalSetup]
    public void Setup()
    {
        intArray = new int[Size];
        stringArray = new string[Size];
    }

    [Benchmark]
    public int ClearAlwaysInt()
    {
        Array.Clear(intArray);
        return intArray[0];
    }

    [Benchmark]
    public int ClearIfNeededInt()
    {
        ClearIfNeeded(intArray);
        return intArray[0];
    }

    [Benchmark]
    public string? ClearAlwaysString()
    {
        Array.Clear(stringArray);
        return stringArray[0];
    }

    [Benchmark]
    public string? ClearIfNeededString()
    {
        ClearIfNeeded(stringArray);
        return stringArray[0];
    }

    // 参照を含まない型ではクリア不要(プール返却時の定石)。JIT が T ごとに分岐を定数化する
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearIfNeeded<T>(T[] array)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(array);
        }
    }
}
