namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// DSP-04 study: a lambda capturing locals vs a static lambda with TState
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StaticLambdaBenchmark
{
    private const int N = 100;

    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[16];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public int CaptureLocal()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            var target = i & 15;
            // Captures a local derived from the loop variable, allocating a closure and a delegate on every call
            total += FindIndex(values, x => x == target);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int StaticWithState()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            var target = i & 15;
            // The compiler caches a static lambda, and the state is passed through the TState argument
            total += FindIndex(values, target, static (x, t) => x == t);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FindIndex(int[] source, Func<int, bool> predicate)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (predicate(source[i]))
            {
                return i;
            }
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FindIndex<TState>(int[] source, TState state, Func<int, TState, bool> predicate)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (predicate(source[i], state))
            {
                return i;
            }
        }

        return -1;
    }
}
