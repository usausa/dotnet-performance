namespace PerformancePatterns.Benchmarks;

using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Running;

using PerformancePatterns.Benchmarks.Buf;
using PerformancePatterns.Benchmarks.Seq;
using PerformancePatterns.Benchmarks.Txt;
using PerformancePatterns.Buf;
using PerformancePatterns.Seq;
using PerformancePatterns.Txt;

public static class Program
{
    public static void Main(string[] args)
    {
        // 測定前にバリアント間の等価性を検証する(benchmark-methodology.md)
        VerifySpanTokenizer();
        VerifyTemporaryBuffer();
        VerifyValueStringBuilder();

        // 実行例: dotnet run -c Release --framework net10.0 -- --filter "*"
        BenchmarkSwitcher
            .FromTypes(
            [
                typeof(SpanTokenizerBenchmark),
#if NET9_0_OR_GREATER
                typeof(SpanTokenizerBclComparisonBenchmark),
#endif
                typeof(TemporaryBufferBenchmark),
                typeof(ValueStringBuilderBenchmark),
            ])
            .Run(args);
    }

    private static void VerifySpanTokenizer()
    {
        var inputs = new[] { string.Empty, "a", "a,b,c", ",", "a,", ",a", "a,,b", ",,", "value0,value1,value2" };
        foreach (var original in inputs)
        {
            // インターン済みリテラルを避けるためコピーを検証対象にする
            var probe = new string(original.AsSpan());
            var expected = probe.Split(',');

            var actual = new List<string>();
            foreach (var token in probe.Tokenize(','))
            {
                actual.Add(token.ToString());
            }

            if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Verify failed. input=[{original}]");
            }
        }
    }

    private static void VerifyTemporaryBuffer()
    {
        using var small = new TemporaryBuffer<byte>(stackalloc byte[512], 64);
        using var large = new TemporaryBuffer<byte>(4096);
        small.Span.Fill(1);
        large.Span.Fill(2);
        if ((small.Span.Length != 64) || (large.Span.Length != 4096) || (small.Span[63] != 1) || (large.Span[4095] != 2))
        {
            throw new InvalidOperationException("Verify failed. TemporaryBuffer");
        }
    }

    private static void VerifyValueStringBuilder()
    {
        var parts = new[] { new string('a', 24), new string('b', 24), new string('c', 24), new string('d', 24) };

        var expectedBuilder = new StringBuilder();
        var handler = new DefaultInterpolatedStringHandler(0, parts.Length, null, stackalloc char[8]);
        using var builder = new ValueStringBuilder(stackalloc char[8]); // 必ず Grow パスを通す
        foreach (var part in parts)
        {
            expectedBuilder.Append(part);
            handler.AppendFormatted(part);
            builder.Append(part);
        }

        var expected = expectedBuilder.ToString();
        var handlerResult = handler.ToStringAndClear();
        var actual = builder.ToStringAndClear();
        if (!string.Equals(expected, actual, StringComparison.Ordinal) ||
            !string.Equals(expected, handlerResult, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verify failed. ValueStringBuilder");
        }
    }
}
