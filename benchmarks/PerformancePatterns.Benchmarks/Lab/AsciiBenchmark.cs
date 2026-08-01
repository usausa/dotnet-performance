namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー③: ASCII 特化処理(.NET 8 の Ascii クラス)
// 問い: ASCII 前提が保証できる場面(HTTP ヘッダ名等)で、
// string.Equals(OrdinalIgnoreCase) / Ascii.EqualsIgnoreCase / 手書き | 0x20 比較はどう使い分けるか。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class AsciiBenchmark
{
    private string[] lowerStrings = default!;

    private string[] mixedStrings = default!;

    private byte[][] lowerBytes = default!;

    private byte[][] mixedBytes = default!;

    [GlobalSetup]
    public void Setup()
    {
        var headers = new[]
        {
            "content-type", "content-length", "accept-encoding", "cache-control",
            "user-agent", "host", "connection", "authorization",
        };

        lowerStrings = new string[headers.Length];
        mixedStrings = new string[headers.Length];
        lowerBytes = new byte[headers.Length][];
        mixedBytes = new byte[headers.Length][];
        for (var i = 0; i < headers.Length; i++)
        {
            var lower = new string(headers[i].AsSpan());
            var chars = lower.ToCharArray();
            for (var j = 0; j < chars.Length; j += 2)
            {
                chars[j] = char.ToUpperInvariant(chars[j]);
            }

            var mixed = new string(chars);
            lowerStrings[i] = lower;
            mixedStrings[i] = mixed;
            lowerBytes[i] = Encoding.ASCII.GetBytes(lower);
            mixedBytes[i] = Encoding.ASCII.GetBytes(mixed);
        }
    }

    [Benchmark(Baseline = true)]
    public int StringEqualsIgnoreCase()
    {
        var count = 0;
        for (var i = 0; i < lowerStrings.Length; i++)
        {
            if (string.Equals(lowerStrings[i], mixedStrings[i], StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int AsciiEqualsIgnoreCase()
    {
        var count = 0;
        for (var i = 0; i < lowerBytes.Length; i++)
        {
            if (Ascii.EqualsIgnoreCase(lowerBytes[i], mixedBytes[i]))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int ManualOr20Compare()
    {
        var count = 0;
        for (var i = 0; i < lowerBytes.Length; i++)
        {
            if (EqualsIgnoreCaseManual(lowerBytes[i], mixedBytes[i]))
            {
                count++;
            }
        }

        return count;
    }

    // 英字のみ大小が異なる前提の | 0x20 正規化('@' と '`' 等の衝突があるため閉じた用途限定)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualsIgnoreCaseManual(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if ((left[i] | 0x20) != (right[i] | 0x20))
            {
                return false;
            }
        }

        return true;
    }
}
