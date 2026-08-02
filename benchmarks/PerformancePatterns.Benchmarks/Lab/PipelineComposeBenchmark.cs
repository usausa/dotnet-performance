namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// DSP-05 検証: パイプライン合成を毎回行う vs 初期化時に 1 回だけ行う
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class PipelineComposeBenchmark
{
    private Func<Func<int, int>, Func<int, int>>[] middlewares = default!;

    private Func<int, int> composed = default!;

    [GlobalSetup]
    public void Setup()
    {
        middlewares =
        [
            static next => x => next(x) + 1,
            static next => x => next(x) * 2,
            static next => x => next(x) - 3,
        ];
        composed = Compose(middlewares);
    }

    [Benchmark(Baseline = true)]
    public int ComposeEveryCall() => Compose(middlewares)(10);

    [Benchmark]
    public int PreComposed() => composed(10);

    [Benchmark]
    public int TerminalDirect() => Terminal(10);

    private static Func<int, int> Compose(Func<Func<int, int>, Func<int, int>>[] filters)
    {
        Func<int, int> next = Terminal;
        for (var i = filters.Length - 1; i >= 0; i--)
        {
            next = filters[i](next);
        }

        return next;
    }

    private static int Terminal(int x) => x + 100;
}
