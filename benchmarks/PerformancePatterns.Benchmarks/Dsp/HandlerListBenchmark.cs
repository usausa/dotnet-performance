namespace PerformancePatterns.Benchmarks.Dsp;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Dsp;

// DSP-03 実装例: マルチキャストデリゲート vs 不変配列 + Volatile 読み
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class HandlerListBenchmark
{
    private Action<int>? multicast;

    private HandlerList<int> handlerList = default!;

    private int sink;

    [Params(1, 2, 4, 8)]
    public int Subscribers { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        multicast = null;
        handlerList = new HandlerList<int>();
        for (var i = 0; i < Subscribers; i++)
        {
            multicast += Handle;
            handlerList.Add(Handle);
        }
    }

    [Benchmark(Baseline = true)]
    public int MulticastDelegate()
    {
        multicast!(1);
        return sink;
    }

    [Benchmark]
    public int HandlerArray()
    {
        handlerList.Publish(1);
        return sink;
    }

    private void Handle(int value) => sink += value;
}
