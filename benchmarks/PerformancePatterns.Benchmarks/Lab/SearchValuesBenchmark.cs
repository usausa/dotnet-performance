namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TXT-08 検証: 候補数別に IndexOfAny(配列) と SearchValues を比較
// (候補 2〜3 個での専用オーバーロード優位は R-07 で測定済み)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SearchValuesBenchmark
{
    private string text = default!;

    private char[] candidates = default!;

    private SearchValues<char> searchValues = default!;

    [Params(3, 8, 32)]
    public int Candidates { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // 区切りは末尾付近にのみ存在させ、走査量を確保する
        var chars = new char[256];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = 'a';
        }

        chars[^8] = '!';
        text = new string(chars);

        candidates = new char[Candidates];
        for (var i = 0; i < Candidates - 1; i++)
        {
            candidates[i] = (char)('A' + i);
        }

        candidates[^1] = '!';
        searchValues = SearchValues.Create(candidates);
    }

    [Benchmark(Baseline = true)]
    public int IndexOfAnyArray() => text.AsSpan().IndexOfAny(candidates);

    [Benchmark]
    public int IndexOfAnySearchValues() => text.AsSpan().IndexOfAny(searchValues);
}
