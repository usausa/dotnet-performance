namespace PerformancePatterns.Benchmarks;

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using BenchmarkDotNet.Running;

using PerformancePatterns.Benchmarks.Buf;
using PerformancePatterns.Benchmarks.Lab;
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
        VerifyLab();
        VerifyLabBatch2();
        VerifyLabBatch3();
        VerifyLabBatch4();
        VerifyLabBatch5();

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
                typeof(ReferenceContainsBranchBenchmark),
                typeof(CopyVariableBenchmark),
                typeof(CopyConstantBenchmark),
                typeof(BoundsCheckHintBenchmark),
                typeof(UninitializedArrayBenchmark),
                typeof(StackallocSizeBenchmark),
                typeof(ListSetCountBenchmark),
                typeof(EnumerableDispatchBenchmark),
                typeof(ListIterationBenchmark),
                typeof(DictionaryCountBenchmark),
                typeof(BufferWriterBenchmark),
                typeof(TokenMatchBenchmark),
                typeof(Utf8WriteBenchmark),
                typeof(AsciiBenchmark),
                typeof(AsyncElisionBenchmark),
                typeof(TimestampBenchmark),
                typeof(PinnedArrayBenchmark),
                typeof(BitOperationsBenchmark),
                typeof(VectorSumBenchmark),
                typeof(RefFieldCursorBenchmark),
                typeof(PInvokeBenchmark),
                typeof(ChannelsBenchmark),
                typeof(PipelinesBenchmark),
                typeof(AsyncEnumerableBenchmark),
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

    private static void VerifyLab()
    {
        // 境界チェック除去バリアントの合計一致
        var bounds = new BoundsCheckHintBenchmark();
        bounds.Setup();
        var expectedSum = 1023 * 1024 / 2;
        if ((bounds.SumByLength() != expectedSum) ||
            (bounds.SumByArrayLength() != expectedSum) ||
            (bounds.SumWithTailTouch() != expectedSum) ||
            (bounds.SumWithUnsignedGuard() != expectedSum))
        {
            throw new InvalidOperationException("Verify failed. BoundsCheckHint");
        }

        // コピーバリアントの結果一致
        var copyConstant = new CopyConstantBenchmark();
        copyConstant.Setup();
        if (copyConstant.SpanCopyTo16() != copyConstant.CopyBlockUnaligned16())
        {
            throw new InvalidOperationException("Verify failed. CopyConstant");
        }

        var copyVariable = new CopyVariableBenchmark { Size = 4096 };
        copyVariable.Setup();
        var copy1 = copyVariable.SpanCopyTo();
        var copy2 = copyVariable.ArrayCopy();
        var copy3 = copyVariable.CopyBlockUnaligned();
        if ((copy1 != copy2) || (copy2 != copy3) || (copy1 != unchecked((byte)4095)))
        {
            throw new InvalidOperationException("Verify failed. CopyVariable");
        }
    }

    private static void VerifyLabBatch2()
    {
        // SetCount + Span 書き込みが Add ループと同一内容になること
        var added = new List<int>();
        for (var i = 0; i < 100; i++)
        {
            added.Add(i);
        }

        var filled = new List<int>();
        CollectionsMarshal.SetCount(filled, 100);
        var fillSpan = CollectionsMarshal.AsSpan(filled);
        for (var i = 0; i < fillSpan.Length; i++)
        {
            fillSpan[i] = i;
        }

        if (!added.SequenceEqual(filled))
        {
            throw new InvalidOperationException("Verify failed. SetCount");
        }

        // 具象型分岐・反復・辞書カウントのバリアント一致
        var dispatch = new EnumerableDispatchBenchmark();
        dispatch.Setup();
        var expectedSum = 1023 * 1024 / 2;
        if ((dispatch.EnumerateArray() != expectedSum) || (dispatch.DispatchArray() != expectedSum) ||
            (dispatch.EnumerateList() != expectedSum) || (dispatch.DispatchList() != expectedSum) ||
            (dispatch.EnumerateIterator() != expectedSum) || (dispatch.DispatchIterator() != expectedSum))
        {
            throw new InvalidOperationException("Verify failed. EnumerableDispatch");
        }

        var iteration = new ListIterationBenchmark();
        iteration.Setup();
        if ((iteration.ForEachList() != expectedSum) || (iteration.ForList() != expectedSum) ||
            (iteration.AsSpanFor() != expectedSum) || (iteration.AsSpanForEach() != expectedSum))
        {
            throw new InvalidOperationException("Verify failed. ListIteration");
        }

        var counting = new DictionaryCountBenchmark();
        counting.Setup();
        if ((counting.DoubleLookup() != 256) || (counting.RefLookup() != 256))
        {
            throw new InvalidOperationException("Verify failed. DictionaryCount");
        }
    }

    private static void VerifyLabBatch3()
    {
        // トークン判定 3 方式の一致(64 プローブ × 4 種のマッチ ID 合計 = 160)
        var tokenMatch = new TokenMatchBenchmark();
        tokenMatch.Setup();
        if ((tokenMatch.StringSwitch() != 160) || (tokenMatch.SequenceEqualChain() != 160) || (tokenMatch.UIntConstantCompare() != 160))
        {
            throw new InvalidOperationException("Verify failed. TokenMatch");
        }

        // UTF-8 整形 3 方式のバイト列一致
        var utf8Write = new Utf8WriteBenchmark();
        utf8Write.Setup();
        var utf8Results = new byte[3][];
        utf8Results[0] = utf8Write.CopyBuffer(utf8Write.StringInterpolationEncode());
        utf8Results[1] = utf8Write.CopyBuffer(utf8Write.CharTryWriteEncode());
        utf8Results[2] = utf8Write.CopyBuffer(utf8Write.Utf8TryWrite());
        if (!utf8Results[0].SequenceEqual(utf8Results[1]) || !utf8Results[0].SequenceEqual(utf8Results[2]))
        {
            throw new InvalidOperationException("Verify failed. Utf8Write");
        }

        // ASCII 比較 3 方式の一致(8 ペアすべて一致)
        var ascii = new AsciiBenchmark();
        ascii.Setup();
        if ((ascii.StringEqualsIgnoreCase() != 8) || (ascii.AsciiEqualsIgnoreCase() != 8) || (ascii.ManualOr20Compare() != 8))
        {
            throw new InvalidOperationException("Verify failed. Ascii");
        }

        // BufferWriter 3 方式の長さと内容の一致
        var bufferWriter = new BufferWriterBenchmark();
        bufferWriter.Setup();
        if ((bufferWriter.MemoryStreamWrite() != 1024) || (bufferWriter.ArrayBufferWriterWrite() != 1024) || (bufferWriter.PooledBufferWriterWrite() != 1024))
        {
            throw new InvalidOperationException("Verify failed. BufferWriter length");
        }

        var expected = new ArrayBufferWriter<byte>();
        using var pooled = new PooledBufferWriter<byte>();
        for (var i = 0; i < 4; i++)
        {
            var chunk = new byte[] { 1, 2, 3, (byte)i };
            chunk.CopyTo(expected.GetSpan(chunk.Length));
            expected.Advance(chunk.Length);
            chunk.CopyTo(pooled.GetSpan(chunk.Length));
            pooled.Advance(chunk.Length);
        }

        if (!expected.WrittenSpan.SequenceEqual(pooled.WrittenSpan))
        {
            throw new InvalidOperationException("Verify failed. BufferWriter content");
        }
    }

    private static void VerifyLabBatch4()
    {
        // async フォワード 4 方式の結果一致(42 × 100)
        var asyncElision = new AsyncElisionBenchmark();
        if ((asyncElision.TaskAwaitForward().GetAwaiter().GetResult() != 4200) ||
            (asyncElision.TaskDirectForward().GetAwaiter().GetResult() != 4200) ||
            (asyncElision.ValueTaskAwaitForward().GetAwaiter().GetResult() != 4200) ||
            (asyncElision.ValueTaskDirectForward().GetAwaiter().GetResult() != 4200))
        {
            throw new InvalidOperationException("Verify failed. AsyncElision");
        }

        // ビット走査・カウント 2 方式の一致
        var bitOperations = new BitOperationsBenchmark();
        bitOperations.Setup();
        if ((bitOperations.SetBitScanLoop() != bitOperations.SetBitScanTzcnt()) ||
            (bitOperations.PopCountManual() != bitOperations.PopCountIntrinsic()))
        {
            throw new InvalidOperationException("Verify failed. BitOperations");
        }

        // pinned バッファ 4 方式の結果一致
        var pinned = new PinnedArrayBenchmark();
        pinned.Setup();
        if ((pinned.PinWithFixed() != 3) || (pinned.PinnedPointerDirect() != 3) ||
            (pinned.AllocateNormal() != 1) || (pinned.AllocatePinned() != 1))
        {
            throw new InvalidOperationException("Verify failed. PinnedArray");
        }
    }

    private static void VerifyLabBatch5()
    {
        // SIMD 3 方式の合計一致
        var vectorSum = new VectorSumBenchmark();
        vectorSum.Setup();
        var expectedVector = vectorSum.ScalarSum();
        if ((vectorSum.EnumerableSum() != expectedVector) ||
            (vectorSum.VectorTSum() != expectedVector) ||
            (vectorSum.Vector256Sum() != expectedVector))
        {
            throw new InvalidOperationException("Verify failed. VectorSum");
        }

        // カーソル 3 方式の合計一致
        var cursor = new RefFieldCursorBenchmark();
        cursor.Setup();
        var expectedCursor = 1023 * 1024 / 2;
        if ((cursor.SumSpanIndex() != expectedCursor) ||
            (cursor.SumSpanReader() != expectedCursor) ||
            (cursor.SumRefFieldCursor() != expectedCursor))
        {
            throw new InvalidOperationException("Verify failed. RefFieldCursor");
        }

        // P/Invoke 各方式が値を返すこと(tick 値そのものは変動するため非ゼロのみ確認)
        var pinvoke = new PInvokeBenchmark();
        if ((pinvoke.DllImportCall() == 0UL) || (pinvoke.LibraryImportCall() == 0UL) ||
            (pinvoke.LibraryImportSuppressGC() == 0UL) || (pinvoke.ManagedTickCount64() == 0UL))
        {
            throw new InvalidOperationException("Verify failed. PInvoke");
        }

        // Channels / Pipe / IAsyncEnumerable の合計一致
        const long expectedChannel = 10_000L * 9_999L / 2L;
        if (new ChannelsBenchmark().UnboundedDefault().GetAwaiter().GetResult() != expectedChannel)
        {
            throw new InvalidOperationException("Verify failed. Channels");
        }

        var pipelines = new PipelinesBenchmark();
        pipelines.Setup();
        if ((pipelines.MemoryStreamPump() != 65536L) ||
            (pipelines.PipePump().GetAwaiter().GetResult() != 65536L))
        {
            throw new InvalidOperationException("Verify failed. Pipelines");
        }

        var asyncEnumerable = new AsyncEnumerableBenchmark();
        if ((asyncEnumerable.SyncForeach() != expectedCursor) ||
            (asyncEnumerable.AsyncForeach().GetAwaiter().GetResult() != expectedCursor))
        {
            throw new InvalidOperationException("Verify failed. AsyncEnumerable");
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
