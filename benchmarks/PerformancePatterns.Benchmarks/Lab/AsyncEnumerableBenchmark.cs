namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー⑤: IAsyncEnumerable のコスト
// 問い: 同期完了で列挙できるデータを await foreach で流す場合の
// 要素あたりオーバーヘッドは IEnumerable の foreach 比でどれくらいか。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class AsyncEnumerableBenchmark
{
    private const int N = 1024;

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int SyncForeach()
    {
        var total = 0;
        foreach (var value in SyncItems(N))
        {
            total += value;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<int> AsyncForeach()
    {
        var total = 0;
        await foreach (var value in AsyncItems(N).ConfigureAwait(false))
        {
            total += value;
        }

        return total;
    }

    private static IEnumerable<int> SyncItems(int count)
    {
        for (var i = 0; i < count; i++)
        {
            yield return i;
        }
    }

    private static async IAsyncEnumerable<int> AsyncItems(int count)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        for (var i = 0; i < count; i++)
        {
            yield return i;
        }
    }
}
