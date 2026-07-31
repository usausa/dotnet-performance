namespace PerformancePatterns.Benchmarks;

using System.Buffers.Binary;
using System.Globalization;
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
        VerifySpanReaderWriter();
        VerifyUtf8DateTimeFormatter();

        // 実行例: dotnet run -c Release --framework net10.0 -- --filter "*"
        BenchmarkSwitcher
            .FromTypes(
            [
                typeof(SpanTokenizerBenchmark),
#if NET9_0_OR_GREATER
                typeof(SpanTokenizerBclComparisonBenchmark),
#endif
                typeof(SpanReaderBenchmark),
                typeof(TemporaryBufferBenchmark),
                typeof(ValueStringBuilderBenchmark),
                typeof(Utf8DateTimeFormatterBenchmark),
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

    private static void VerifySpanReaderWriter()
    {
        const int count = 4;
        var packet = SpanReaderBenchmark.CreatePacket(count);

        // SpanReader によるパース
        var reader = new SpanReader<byte>(packet);
        var total = (long)reader.ReadUnmanaged<uint>();
        var readCount = reader.ReadUnmanaged<int>();
        for (var i = 0; i < readCount; i++)
        {
            total += reader.ReadUnmanaged<long>();
            total += reader.ReadUnmanaged<int>();
        }

        // BinaryPrimitives による独立実装と一致することを検証
        var span = packet.AsSpan();
        var offset = 0;
        var expected = (long)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset, sizeof(uint)));
        offset += sizeof(uint);
        var expectedCount = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, sizeof(int)));
        offset += sizeof(int);
        for (var i = 0; i < expectedCount; i++)
        {
            expected += BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, sizeof(long)));
            offset += sizeof(long);
            expected += BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, sizeof(int)));
            offset += sizeof(int);
        }

        // SpanWriter で構築したパケットが CreatePacket と一致することを検証(リトルエンディアン環境)
        var rebuilt = new byte[packet.Length];
        var writer = new SpanWriter<byte>(rebuilt);
        writer.WriteUnmanaged(0xCAFEBABEu);
        writer.WriteUnmanaged(count);
        for (var i = 0; i < count; i++)
        {
            writer.WriteUnmanaged(1000L + i);
            writer.WriteUnmanaged(i * 3);
        }

        if ((total != expected) ||
            (writer.Position != packet.Length) ||
            (BitConverter.IsLittleEndian && !packet.AsSpan().SequenceEqual(rebuilt)))
        {
            throw new InvalidOperationException("Verify failed. SpanReader/SpanWriter");
        }
    }

    private static void VerifyUtf8DateTimeFormatter()
    {
        Span<byte> buffer = stackalloc byte[Utf8DateTimeFormatter.FormattedLength];
        var values = new[]
        {
            new DateTime(1, 1, 1, 0, 0, 0),
            new DateTime(2000, 2, 29, 12, 0, 0),
            new DateTime(2026, 8, 1, 12, 34, 56),
            new DateTime(9999, 12, 31, 23, 59, 59),
        };
        foreach (var value in values)
        {
            if (!Utf8DateTimeFormatter.TryFormat(value, buffer, out _) ||
                !string.Equals(value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), Encoding.ASCII.GetString(buffer), StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Verify failed. Utf8DateTimeFormatter value=[{value:O}]");
            }
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
