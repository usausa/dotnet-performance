namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー②: IEnumerable<T> 引数の具象型分岐
// 問い: `is T[]` / `is List<T>` で Span パスへ逃がす分岐は、配列・List でどれだけ効き、
// 分岐が外れる列挙子ソースへのペナルティはあるか(LINQ 内部の定石の実証)。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class EnumerableDispatchBenchmark
{
    private const int Size = 1024;

    private IEnumerable<int> arraySource = default!;

    private IEnumerable<int> listSource = default!;

    private IEnumerable<int> iteratorSource = default!;

    [GlobalSetup]
    public void Setup()
    {
        var array = Enumerable.Range(0, Size).ToArray();
        arraySource = array;
        listSource = array.ToList();
        iteratorSource = CreateIterator(Size);
    }

    [Benchmark]
    public int EnumerateArray() => SumEnumerate(arraySource);

    [Benchmark]
    public int DispatchArray() => SumDispatch(arraySource);

    [Benchmark]
    public int EnumerateList() => SumEnumerate(listSource);

    [Benchmark]
    public int DispatchList() => SumDispatch(listSource);

    [Benchmark]
    public int EnumerateIterator() => SumEnumerate(iteratorSource);

    [Benchmark]
    public int DispatchIterator() => SumDispatch(iteratorSource);

    private static IEnumerable<int> CreateIterator(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return i;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumEnumerate(IEnumerable<int> source)
    {
        var total = 0;
        foreach (var value in source)
        {
            total += value;
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumDispatch(IEnumerable<int> source)
    {
        if (source is int[] array)
        {
            return SumSpan(array);
        }

        if (source is List<int> list)
        {
            return SumSpan(CollectionsMarshal.AsSpan(list));
        }

        var total = 0;
        foreach (var value in source)
        {
            total += value;
        }

        return total;
    }

    private static int SumSpan(ReadOnlySpan<int> span)
    {
        var total = 0;
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }
}
