namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-04 study: capturing vs non-capturing local functions (closure allocation on delegate conversion)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class LocalFunctionClosureBenchmark
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
    public int CapturingLocalFunction()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            var target = i & 15;

            // Converting a capturing local function to a delegate allocates a closure
            bool Match(int x) => x == target;
            total += Apply(values, Match);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public int StaticLocalFunction()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            var target = i & 15;

            // A static local function (which cannot capture) plus a state argument lets the delegate be cached
            static bool Match(int x, int t) => x == t;
            total += Apply(values, target, Match);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Apply(int[] source, Func<int, bool> predicate)
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
    private static int Apply(int[] source, int state, Func<int, int, bool> predicate)
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
