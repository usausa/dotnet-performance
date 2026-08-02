namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// ASY-06 検証: ジョブごとの Timer 生成 vs 単一ループ方式の起床プリミティブ(TCS 差し替え)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SchedulerPrimitiveBenchmark
{
    private TaskCompletionSource wakeup = new(TaskCreationOptions.RunContinuationsAsynchronously);

    // ジョブ 1 件の登録コスト: Timer を生成して破棄(タイマーキューへの登録を含む)
    [Benchmark(Baseline = true)]
    public void TimerPerJob()
    {
        using var timer = new Timer(static _ => { }, null, Timeout.Infinite, Timeout.Infinite);
    }

    // ジョブ 1 件の通知コスト: 新しい TCS に差し替えてから旧 TCS を完了させる
    [Benchmark]
    public bool TcsSwapNotify()
    {
        var previous = Interlocked.Exchange(
            ref wakeup,
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
        return previous.TrySetResult();
    }
}
