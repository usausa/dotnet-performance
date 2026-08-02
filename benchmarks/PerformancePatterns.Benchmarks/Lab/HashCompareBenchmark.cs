namespace PerformancePatterns.Benchmarks.Lab;

using System.IO.Hashing;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Col;

// BIT-05 検証: 汎用ハッシュの選択肢を入力長別に比較
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class HashCompareBenchmark
{
    private string value = default!;

    [Params(8, 64, 512)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var chars = new char[Length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = (char)('a' + (i % 26));
        }

        // 実行時生成(インターンを避ける)
        value = new string(chars);
    }

    [Benchmark(Baseline = true)]
    public int StringGetHashCode() => string.GetHashCode(value.AsSpan(), StringComparison.Ordinal);

    [Benchmark]
    public ulong XxHash3Cast()
        => XxHash3.HashToUInt64(MemoryMarshal.AsBytes(value.AsSpan()));

    [Benchmark]
    public unsafe ulong XxHash3Fixed()
    {
        fixed (char* p = value)
        {
            return XxHash3.HashToUInt64(new ReadOnlySpan<byte>(p, value.Length * sizeof(char)));
        }
    }

    [Benchmark]
    public int Fnv1a()
    {
        unchecked
        {
            var hash = 2166136261u;
            foreach (var c in value)
            {
                hash = (c ^ hash) * 16777619;
            }

            return (int)hash;
        }
    }

    // BIT-02: 長さ + 3 文字だけのサンプリングハッシュ(入力長に依存しない)
    [Benchmark]
    public int SamplingHash() => SampledNameTable<int>.CalculateHash(value.AsSpan());
}
