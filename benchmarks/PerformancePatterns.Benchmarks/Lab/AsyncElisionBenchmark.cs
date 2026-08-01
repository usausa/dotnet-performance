namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー④: async ステートマシンの省略
// 問い: 同期完了する内側呼び出しを単純フォワードする場合、async/await を書かず
// Task/ValueTask をそのまま返すとどれだけ差が出るか(キャッシュ済み Task の再ラップ問題を含む)。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class AsyncElisionBenchmark
{
    private const int N = 100;

    // 42 はランタイムの Task キャッシュ(-1〜8)の範囲外 → async ラッパーは再ラップで毎回 Task を確保する
    private static readonly Task<int> CompletedTask = Task.FromResult(42);

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public async Task<int> TaskAwaitForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await AwaitForwardTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<int> TaskDirectForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await DirectForwardTask().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<int> ValueTaskAwaitForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await AwaitForwardValueTaskAsync().ConfigureAwait(false);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public async Task<int> ValueTaskDirectForward()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += await DirectForwardValueTask().ConfigureAwait(false);
        }

        return total;
    }

    private static Task<int> InnerTask() => CompletedTask;

    private static ValueTask<int> InnerValueTask() => new(42);

    // ❌ 単純フォワードに async/await(ステートマシン + 結果の再ラップ)
    private static async Task<int> AwaitForwardTaskAsync() => await InnerTask().ConfigureAwait(false);

    // ✅ Task をそのまま返す(async 消去)
    private static Task<int> DirectForwardTask() => InnerTask();

    private static async ValueTask<int> AwaitForwardValueTaskAsync() => await InnerValueTask().ConfigureAwait(false);

    private static ValueTask<int> DirectForwardValueTask() => InnerValueTask();
}
