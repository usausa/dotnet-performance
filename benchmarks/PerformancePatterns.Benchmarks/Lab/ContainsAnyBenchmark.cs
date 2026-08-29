namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TXT-08 follow-up: when only a yes/no answer is needed, does ContainsAny beat IndexOfAny(...) >= 0?
// ContainsAny can bail out at the first matching vector without extracting the lane position, so the
// question is whether that saving is measurable. Measured at three match positions, because an early
// match makes the index extraction a larger share of the total.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ContainsAnyBenchmark
{
    private static readonly SearchValues<char> Values = SearchValues.Create("!?;:,|\t\n");

    private string early = default!;
    private string late = default!;
    private string absent = default!;

    [GlobalSetup]
    public void Setup()
    {
        early = Fill(256, 4);
        late = Fill(256, 248);
        absent = Fill(256, -1);
    }

    private static string Fill(int length, int hitIndex)
    {
        var chars = new char[length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = 'a';
        }

        if (hitIndex >= 0)
        {
            chars[hitIndex] = '!';
        }

        return new string(chars);
    }

    // --- match near the head ---

    [Benchmark(Baseline = true)]
    public bool IndexOfAny_Early() => early.AsSpan().IndexOfAny(Values) >= 0;

    [Benchmark]
    public bool ContainsAny_Early() => early.AsSpan().ContainsAny(Values);

    // --- match near the tail ---

    [Benchmark]
    public bool IndexOfAny_Late() => late.AsSpan().IndexOfAny(Values) >= 0;

    [Benchmark]
    public bool ContainsAny_Late() => late.AsSpan().ContainsAny(Values);

    // --- no match (full scan) ---

    [Benchmark]
    public bool IndexOfAny_Absent() => absent.AsSpan().IndexOfAny(Values) >= 0;

    [Benchmark]
    public bool ContainsAny_Absent() => absent.AsSpan().ContainsAny(Values);
}
