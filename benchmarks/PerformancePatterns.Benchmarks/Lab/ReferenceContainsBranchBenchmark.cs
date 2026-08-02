namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 1: branching on RuntimeHelpers.IsReferenceOrContainsReferences<T>
// Question: does the JIT constant-fold the branch that skips clearing for a T holding no references, so that
// unnecessary work is removed at zero check cost (the standard idiom for clearing on pool return)?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ReferenceContainsBranchBenchmark
{
    private const int Size = 1024;

    private int[] intArray = default!;

    private string[] stringArray = default!;

    [GlobalSetup]
    public void Setup()
    {
        intArray = new int[Size];
        stringArray = new string[Size];
    }

    [Benchmark]
    public int ClearAlwaysInt()
    {
        Array.Clear(intArray);
        return intArray[0];
    }

    [Benchmark]
    public int ClearIfNeededInt()
    {
        ClearIfNeeded(intArray);
        return intArray[0];
    }

    [Benchmark]
    public string? ClearAlwaysString()
    {
        Array.Clear(stringArray);
        return stringArray[0];
    }

    [Benchmark]
    public string? ClearIfNeededString()
    {
        ClearIfNeeded(stringArray);
        return stringArray[0];
    }

    // No clearing needed for types that hold no references (the standard idiom on pool return). The JIT folds the branch per T
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ClearIfNeeded<T>(T[] array)
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(array);
        }
    }
}
