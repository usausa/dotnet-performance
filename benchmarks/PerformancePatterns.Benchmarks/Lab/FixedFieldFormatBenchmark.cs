namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TXT-09 検証: 固定長フィールドの数値左詰め整形とトリム
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

        // トリム対象: 前後にフィラーを持つ固定長フィールド
        Array.Fill(padded, Filler);
        "TOKYO-0001".CopyTo(padded.AsSpan(4));
    }

    // Verify 用: 現在のフィールド内容を取得
    public string SnapshotField() => new(field);

    // BCL: TryFormat は左詰めで書くので、残りを Fill するだけ
    [Benchmark(Baseline = true)]
    public char TryFormatThenFill()
    {
        var span = field.AsSpan();
        value.TryFormat(span, out var written);
        span[written..].Fill(Filler);
        return span[0];
    }

    // 手書き: LSB から前向きに書いて、最後に Reverse
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

    // 手書き: 末尾から右詰めで書いて、先頭へ一括シフト(Reverse も桁数事前計算も不要)
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

    // トリム: 手書きループで前後のフィラーを探す
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

    // トリム: ベクトル化済みの IndexOfAnyExcept / LastIndexOfAnyExcept
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
