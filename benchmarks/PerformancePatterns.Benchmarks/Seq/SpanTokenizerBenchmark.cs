namespace PerformancePatterns.Benchmarks.Seq;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Seq;

// 注意: 下位 TFM に存在しないメソッドを同一クラスに #if で混在させると、
// ホスト(net10.0)が発見したメソッドを下位ランタイムの子ビルドが解決できず全ケース NA になる。
// TFM 依存の比較は「クラスごと #if + そのクラスにだけ新しいランタイムのジョブを付ける」形で分離する。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net80)]
[MediumRunJob(RuntimeMoniker.Net90)]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SpanTokenizerBenchmark
{
    private string input = default!;

    [Params(4, 64)]
    public int Tokens { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // 実行時に組み立てるためインターンされない(literal 直接使用による Equals 短絡を回避)
        input = string.Join(',', Enumerable.Range(0, Tokens).Select(static x => "value" + x));
    }

    [Benchmark(Baseline = true)]
    public int StringSplit()
    {
        var total = 0;
        foreach (var token in input.Split(','))
        {
            total += token.Length;
        }

        return total;
    }

    [Benchmark]
    public int SpanTokenizer()
    {
        var total = 0;
        foreach (var token in input.Tokenize(','))
        {
            total += token.Length;
        }

        return total;
    }
}

#if NET9_0_OR_GREATER
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net90)]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SpanTokenizerBclComparisonBenchmark
{
    private string input = default!;

    [Params(4, 64)]
    public int Tokens { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        input = string.Join(',', Enumerable.Range(0, Tokens).Select(static x => "value" + x));
    }

    [Benchmark(Baseline = true)]
    public int SpanTokenizer()
    {
        var total = 0;
        foreach (var token in input.Tokenize(','))
        {
            total += token.Length;
        }

        return total;
    }

    [Benchmark]
    public int MemoryExtensionsSplit()
    {
        var total = 0;
        var span = input.AsSpan();
        foreach (var range in span.Split(','))
        {
            total += span[range].Length;
        }

        return total;
    }
}
#endif
