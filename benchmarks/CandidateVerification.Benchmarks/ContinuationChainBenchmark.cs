namespace CandidateVerification.Benchmarks;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// C-08 (DSP-05 update candidate): interceptor chain composition strategies.
// ClosureCompose = per-hop closure allocation (naive) / CachedContinuation = context-cached method-group delegate
// PrecomposedChain = ASP.NET Core middleware style (delegate chain folded once at build time)
// IndexForward = Azure.Core HttpPipeline style (policy array + index advancing, no delegate at all)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ContinuationChainBenchmark
{
    private const int InterceptorCount = 4;

    private IInterceptorLike[] interceptors = default!;

    private ChainContext chainContext = default!;

    private Func<int, ValueTask> precomposed = default!;

    private IndexedPipeline indexedPipeline = default!;

    private long counter;

    [GlobalSetup]
    public void Setup()
    {
        interceptors = new IInterceptorLike[InterceptorCount];
        var indexedInterceptors = new IIndexedInterceptorLike[InterceptorCount];
        for (var i = 0; i < InterceptorCount; i++)
        {
            interceptors[i] = new PassThroughInterceptor();
            indexedInterceptors[i] = new IndexedPassThroughInterceptor();
        }

        // In production the context comes from a pool (C-01); the single-threaded benchmark reuses one instance
        chainContext = new ChainContext(interceptors, command => counter += command);

        // ASP.NET Core style: fold the delegate chain once at build time (closures allocated at setup only)
        Func<int, ValueTask> composed = Terminal;
        for (var i = InterceptorCount - 1; i >= 0; i--)
        {
            var interceptor = interceptors[i];
            var next = composed;
            composed = command => interceptor.InvokeAsync(command, next);
        }

        precomposed = composed;

        indexedPipeline = new IndexedPipeline(indexedInterceptors, command => counter += command);
    }

    [Benchmark(Baseline = true)]
    public async Task<long> ClosureCompose()
    {
        await InvokeFrom(0, 1).ConfigureAwait(false);
        return counter;
    }

    [Benchmark]
    public async Task<long> CachedContinuation()
    {
        await chainContext.Publish(1).ConfigureAwait(false);
        return counter;
    }

    [Benchmark]
    public async Task<long> PrecomposedChain()
    {
        await precomposed(1).ConfigureAwait(false);
        return counter;
    }

    [Benchmark]
    public async Task<long> IndexForward()
    {
        await indexedPipeline.Publish(1).ConfigureAwait(false);
        return counter;
    }

    public static void Verify()
    {
        var benchmark = new ContinuationChainBenchmark();
        benchmark.Setup();

        benchmark.counter = 0;
        benchmark.ClosureCompose().GetAwaiter().GetResult();
        var closure = benchmark.counter;

        benchmark.counter = 0;
        benchmark.CachedContinuation().GetAwaiter().GetResult();
        var cached = benchmark.counter;

        benchmark.counter = 0;
        benchmark.PrecomposedChain().GetAwaiter().GetResult();
        var precomposed = benchmark.counter;

        benchmark.counter = 0;
        benchmark.IndexForward().GetAwaiter().GetResult();
        var indexed = benchmark.counter;

        // 4 pass-through interceptors each add 1 to the command: 1 + 4 = 5
        if ((closure != InterceptorCount + 1) || (closure != cached) || (closure != precomposed) || (closure != indexed))
        {
            throw new InvalidOperationException($"Chain variants disagree: {closure} / {cached} / {precomposed} / {indexed}.");
        }
    }

    private ValueTask Terminal(int command)
    {
        counter += command;
        return default;
    }

    private ValueTask InvokeFrom(int index, int command)
    {
        if (index < interceptors.Length)
        {
            // Naive shape: every hop allocates a closure + delegate for "next"
            return interceptors[index].InvokeAsync(command, c => InvokeFrom(index + 1, c));
        }

        counter += command;
        return default;
    }

    private sealed class ChainContext
    {
        private readonly Func<int, ValueTask> continuation;

        private readonly IInterceptorLike[] interceptors;

        private readonly Action<int> terminal;

        private int index;

        public ChainContext(IInterceptorLike[] interceptors, Action<int> terminal)
        {
            this.interceptors = interceptors;
            this.terminal = terminal;
            continuation = InvokeNext;    // method-group delegate allocated once per context lifetime
        }

        public ValueTask Publish(int command)
        {
            index = 0;
            return InvokeNext(command);
        }

        private ValueTask InvokeNext(int command)
        {
            if (index < interceptors.Length)
            {
                var interceptor = interceptors[index];
                index++;
                return interceptor.InvokeAsync(command, continuation);   // same delegate instance on every hop
            }

            terminal(command);
            return default;
        }
    }
}

public interface IInterceptorLike
{
    ValueTask InvokeAsync(int command, Func<int, ValueTask> continuation);
}

public sealed class PassThroughInterceptor : IInterceptorLike
{
    public ValueTask InvokeAsync(int command, Func<int, ValueTask> continuation) => continuation(command + 1);
}

// Azure.Core HttpPipeline style: no delegate, the pipeline object and the next index are passed along
public interface IIndexedInterceptorLike
{
    ValueTask InvokeAsync(int command, IndexedPipeline pipeline, int index);
}

public sealed class IndexedPipeline
{
    private readonly IIndexedInterceptorLike[] interceptors;

    private readonly Action<int> terminal;

    public IndexedPipeline(IIndexedInterceptorLike[] interceptors, Action<int> terminal)
    {
        this.interceptors = interceptors;
        this.terminal = terminal;
    }

    public ValueTask Publish(int command) => ProcessNext(command, 0);

    public ValueTask ProcessNext(int command, int index)
    {
        if (index < interceptors.Length)
        {
            return interceptors[index].InvokeAsync(command, this, index);
        }

        terminal(command);
        return default;
    }
}

public sealed class IndexedPassThroughInterceptor : IIndexedInterceptorLike
{
    public ValueTask InvokeAsync(int command, IndexedPipeline pipeline, int index)
        => pipeline.ProcessNext(command + 1, index + 1);
}
