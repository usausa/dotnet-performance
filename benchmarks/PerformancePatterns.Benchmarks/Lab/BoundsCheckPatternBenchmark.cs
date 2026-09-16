namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// R-15 / R-18 follow-up: two bounds-check elimination shapes that are new or version-dependent.
//
// 1. Length from another sequence (Zenn "境界チェックが消えるパターン集" #13):
//    `if (prefix.Length < path.Length) path[prefix.Length]` — on .NET 10 the check disappears for
//    string and array but is said to remain for ReadOnlySpan<char>. The three variants below are the
//    same operation over the three container kinds; DisassemblyDiagnoser shows whether the check is there.
//
// 2. switch on Length (.NET 10 PR #113998): `switch (span.Length) { case 4: ... span[3] }` — the case
//    label now proves the length, so the four indexed reads should carry no checks, same as the
//    `if (span.Length == 4)` guard that was already handled.
//
// All variants are NoInlining so each one is disassembled on its own.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BoundsCheckPatternBenchmark
{
    private const int N = 256;

    private string pathString = default!;
    private string prefixString = default!;
    private char[] pathArray = default!;
    private char[] prefixArray = default!;
    private int[] quad = default!;

    [GlobalSetup]
    public void Setup()
    {
        prefixString = "/api/v1/";
        pathString = "/api/v1/items/42";
        prefixArray = prefixString.ToCharArray();
        pathArray = pathString.ToCharArray();
        quad = [1, 2, 3, 4];
    }

    //--------------------------------------------------------------------------------
    // 1. Length from another sequence
    //--------------------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    public int OtherLength_String()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += CharAfterPrefix(pathString, prefixString);
        }

        return total;
    }

    [Benchmark]
    public int OtherLength_Array()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += CharAfterPrefix(pathArray, prefixArray);
        }

        return total;
    }

    [Benchmark]
    public int OtherLength_Span()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += CharAfterPrefix(pathString.AsSpan(), prefixString.AsSpan());
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CharAfterPrefix(string path, string prefix) =>
        prefix.Length < path.Length ? path[prefix.Length] : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CharAfterPrefix(char[] path, char[] prefix) =>
        prefix.Length < path.Length ? path[prefix.Length] : -1;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CharAfterPrefix(ReadOnlySpan<char> path, ReadOnlySpan<char> prefix) =>
        prefix.Length < path.Length ? path[prefix.Length] : -1;

    //--------------------------------------------------------------------------------
    // 2. switch on Length
    //--------------------------------------------------------------------------------

    [Benchmark]
    public int LengthGuard_If()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += SumIfFour(quad);
        }

        return total;
    }

    [Benchmark]
    public int LengthGuard_Switch()
    {
        var total = 0;
        for (var i = 0; i < N; i++)
        {
            total += SumSwitchFour(quad);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumIfFour(ReadOnlySpan<int> span)
    {
        if (span.Length == 4)
        {
            return span[0] + span[1] + span[2] + span[3];
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumSwitchFour(ReadOnlySpan<int> span) =>
        span.Length switch
        {
            4 => span[0] + span[1] + span[2] + span[3],
            _ => -1,
        };
}
