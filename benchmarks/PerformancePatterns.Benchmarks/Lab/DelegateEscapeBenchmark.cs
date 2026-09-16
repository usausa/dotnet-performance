namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// DSP-04 follow-up for .NET 10: escape analysis can now stack-allocate a delegate that does not escape
// the method, but it cannot remove the closure (display class) behind a capturing lambda. This measures
// the three shapes so the catalog can say precisely what the static lambda still buys.
//
//   CapturingLocal   - capturing lambda created and invoked in one expression (delegate may be elided;
//                      the display class that holds `local` cannot be)
//   StaticWithState  - static lambda, state passed as an argument (the DSP-04 form; the compiler caches
//                      the delegate, so nothing is allocated at all)
//   CapturingEscaped - capturing lambda stored to a field before being invoked, so the delegate must
//                      exist on the heap
//
// The delegate is invoked directly (not passed to an opaque callee) because escape analysis can only
// prove non-escape when it sees every use.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class DelegateEscapeBenchmark
{
    private const int N = 64;

    private readonly int offset = 7;

    // Escape target for CapturingEscaped; read in Cleanup so the store is observable
    public Func<int, int>? Sink { get; private set; }

    [GlobalCleanup]
    public void Cleanup() => GC.KeepAlive(Sink);

    [Benchmark(Baseline = true)]
    public int CapturingLocal()
    {
        var local = offset;
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += ((Func<int, int>)(x => x + local))(i);
        }

        return total;
    }

    [Benchmark]
    public int StaticWithState()
    {
        var local = offset;
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += ((Func<int, int, int>)(static (x, s) => x + s))(i, local);
        }

        return total;
    }

    // Captures a loop-scoped variable, so Roslyn cannot cache the delegate in a shared display class: a
    // fresh display class and delegate are created every iteration. This is the shape where .NET 10 can
    // stack-allocate the delegate (the display class stays on the heap).
    [Benchmark]
    public int CapturingLoopLocal()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            var k = offset;
            total += ((Func<int, int>)(x => x + k))(i);
        }

        return total;
    }

    [Benchmark]
    public int CapturingEscaped()
    {
        var local = offset;
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            Sink = x => x + local;
            total += Sink(i);
        }

        return total;
    }
}
