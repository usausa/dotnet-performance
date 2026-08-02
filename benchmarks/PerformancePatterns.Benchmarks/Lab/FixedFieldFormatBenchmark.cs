namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TXT-09 study: left-aligned numeric formatting into fixed-length fields, and trimming
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class FixedFieldFormatBenchmark
{
    private const int FieldWidth = 12;

    private const char Filler = ' ';

    private readonly char[] field = new char[FieldWidth];

    private readonly char[] padded = new char[32];

    private int value;

    [GlobalSetup]
    public void Setup()
    {
        value = 12345678;

        // Trim target: a fixed-length field with filler on both sides
        Array.Fill(padded, Filler);
        "TOKYO-0001".CopyTo(padded.AsSpan(4));
    }

    // For Verify: gets the current field content
    public string SnapshotField() => new(field);

    // BCL: TryFormat writes left-aligned, so only the remainder needs Fill
    [Benchmark(Baseline = true)]
    public char TryFormatThenFill()
    {
        var span = field.AsSpan();
        value.TryFormat(span, out var written);
        span[written..].Fill(Filler);
        return span[0];
    }

    // Handwritten: write forward from the least significant digit, then Reverse at the end
    [Benchmark]
    public char ManualLsbThenReverse()
    {
        var span = field.AsSpan();
        var v = value;
        var pos = 0;
        do
        {
            span[pos++] = (char)('0' + (v % 10));
            v /= 10;
        }
        while (v != 0);

        span[..pos].Reverse();
        span[pos..].Fill(Filler);
        return span[0];
    }

    // Handwritten: write right-aligned from the end, then shift to the front in one go (no Reverse and no digit-count precomputation)
    [Benchmark]
    public char ManualRightAlignShift()
    {
        var span = field.AsSpan();
        var v = value;
        var pos = FieldWidth;
        do
        {
            span[--pos] = (char)('0' + (v % 10));
            v /= 10;
        }
        while (v != 0);

        var written = FieldWidth - pos;
        span[pos..].CopyTo(span);
        span[written..].Fill(Filler);
        return span[0];
    }

    // Trim: find the leading and trailing filler with a handwritten loop
    [Benchmark]
    public int TrimManualLoop()
    {
        var span = padded.AsSpan();
        var start = 0;
        while ((start < span.Length) && (span[start] == Filler))
        {
            start++;
        }

        var end = span.Length - 1;
        while ((end >= start) && (span[end] == Filler))
        {
            end--;
        }

        return end - start + 1;
    }

    // Trim: the vectorized IndexOfAnyExcept / LastIndexOfAnyExcept
    [Benchmark]
    public int TrimVectorized()
    {
        var span = padded.AsSpan();
        var start = span.IndexOfAnyExcept(Filler);
        if (start < 0)
        {
            return 0;
        }

        var end = span.LastIndexOfAnyExcept(Filler);
        return end - start + 1;
    }
}
