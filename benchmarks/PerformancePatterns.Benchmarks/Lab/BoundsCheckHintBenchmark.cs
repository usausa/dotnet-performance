namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 1: bounds check elimination by touching the last element first
// Question: in a loop whose length comes from a parameter, are the "touch the last element first" and
// "unsigned guard" idioms still effective? Compare generations to confirm they help on .NET 8 and are expected to disappear on .NET 10 (likely an anti-pattern going forward).
// Only this class additionally runs a net8 job so the generations can be compared.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net80)]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BoundsCheckHintBenchmark
{
    private int[] array = default!;

    private int length;

    [GlobalSetup]
    public void Setup()
    {
        array = new int[1024];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        length = array.Length;
    }

    [Benchmark(Baseline = true)]
    public int SumByLength() => SumWithExternalLength(array, length);

    [Benchmark]
    public int SumByArrayLength() => SumWithOwnLength(array);

    [Benchmark]
    public int SumWithTailTouch() => SumWithTailTouchCore(array, length);

    [Benchmark]
    public int SumWithUnsignedGuard() => SumWithGuardCore(array, length);

    // Length supplied from outside: the JIT cannot eliminate the bounds check (baseline)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithExternalLength(int[] array, int length)
    {
        var total = 0;
        for (var i = 0; i < length; i++)
        {
            total += array[i];
        }

        return total;
    }

    // Using array.Length as the condition: bounds check elimination kicks in (the ideal reference value)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithOwnLength(int[] array)
    {
        var total = 0;
        for (var i = 0; i < array.Length; i++)
        {
            total += array[i];
        }

        return total;
    }

    // Idiom that touches the last element first to teach the JIT the range
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithTailTouchCore(int[] array, int length)
    {
        _ = array[length - 1];
        var total = 0;
        for (var i = 0; i < length; i++)
        {
            total += array[i];
        }

        return total;
    }

    // Idiom that proves the range with a leading guard (BCL throw helper)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithGuardCore(int[] array, int length)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)length, (uint)array.Length);

        var total = 0;
        for (var i = 0; i < length; i++)
        {
            total += array[i];
        }

        return total;
    }
}
