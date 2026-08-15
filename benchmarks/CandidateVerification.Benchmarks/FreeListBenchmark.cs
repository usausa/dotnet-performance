namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using CandidateVerification.Benchmarks.Candidates;

using PerformancePatterns.Dsp;

// C-02 (DSP-03 update candidate): copy-on-write immutable handler array vs tombstone FreeList
// Publish = traversal frequency axis, Churn = mutation frequency axis
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class FreeListBenchmark
{
    private const int HandlerCount = 8;

    private readonly HandlerList<int> handlerList = new();

    private readonly FreeList<Action<int>> freeList = new(HandlerCount);

    private readonly FreeList<Action<int>> sparseFreeList = new(HandlerCount * 4);

    private Action<int> extraHandler = default!;

    private long counter;

    [GlobalSetup]
    public void Setup()
    {
        for (var i = 0; i < HandlerCount; i++)
        {
            var handler = CreateHandler(i + 1);
            handlerList.Add(handler);
            freeList.Add(handler);
        }

        // Full verification shape: 8 live handlers scattered among 24 tombstones (compaction concern)
        var placeholders = new List<Action<int>>();
        for (var i = 0; i < HandlerCount * 4; i++)
        {
            if ((i % 4) == 3)
            {
                sparseFreeList.Add(CreateHandler((i / 4) + 1));
            }
            else
            {
                var placeholder = CreateHandler(1000 + i);
                placeholders.Add(placeholder);
                sparseFreeList.Add(placeholder);
            }
        }

        foreach (var placeholder in placeholders)
        {
            sparseFreeList.Remove(placeholder);
        }

        extraHandler = CreateHandler(1);
    }

    [Benchmark(Baseline = true)]
    public long PublishImmutable()
    {
        handlerList.Publish(1);
        return counter;
    }

    [Benchmark]
    public long PublishFreeList()
    {
        foreach (var handler in freeList.AsSpan())
        {
            handler?.Invoke(1);
        }

        return counter;
    }

    [Benchmark]
    public long PublishFreeListSparse()
    {
        foreach (var handler in sparseFreeList.AsSpan())
        {
            handler?.Invoke(1);
        }

        return counter;
    }

    [Benchmark]
    public long ChurnImmutable()
    {
        handlerList.Add(extraHandler);
        handlerList.Remove(extraHandler);
        return counter;
    }

    [Benchmark]
    public long ChurnFreeList()
    {
        freeList.Add(extraHandler);
        freeList.Remove(extraHandler);
        return counter;
    }

    private Action<int> CreateHandler(int weight) => x => counter += x * weight;

    public static void Verify()
    {
        var benchmark = new FreeListBenchmark();
        benchmark.Setup();

        benchmark.counter = 0;
        benchmark.PublishImmutable();
        var immutable = benchmark.counter;

        benchmark.counter = 0;
        benchmark.PublishFreeList();
        var free = benchmark.counter;

        benchmark.counter = 0;
        benchmark.PublishFreeListSparse();
        var sparse = benchmark.counter;

        if ((immutable != free) || (immutable != sparse) || (immutable == 0))
        {
            throw new InvalidOperationException($"Publish results differ: {immutable} / {free} / {sparse}.");
        }

        benchmark.ChurnImmutable();
        benchmark.ChurnFreeList();

        benchmark.counter = 0;
        benchmark.PublishFreeList();
        if (benchmark.counter != free)
        {
            throw new InvalidOperationException("FreeList churn must leave the handler set unchanged.");
        }
    }
}
