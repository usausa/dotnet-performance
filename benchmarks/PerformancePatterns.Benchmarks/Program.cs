namespace PerformancePatterns.Benchmarks;

using System.Buffers;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using BenchmarkDotNet.Running;

using PerformancePatterns.Benchmarks.Buf;
using PerformancePatterns.Benchmarks.Col;
using PerformancePatterns.Benchmarks.Dsp;
using PerformancePatterns.Benchmarks.Lab;
using PerformancePatterns.Benchmarks.Seq;
using PerformancePatterns.Benchmarks.Txt;
using PerformancePatterns.Benchmarks.Typ;
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
        VerifyPatternImplementations();
        VerifyUnmeasuredBatch();
        VerifyUnmeasuredBatch2();
        VerifyUnmeasuredBatch3();
        VerifyImplementationBatch3();
        VerifyStarFiveBatch();
        VerifyStarFourBatchA();
        VerifyStarFourBatchB();

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
                typeof(DisposeGuardBenchmark),
                typeof(StructArrayRefBenchmark),
                typeof(DualSpanWalkBenchmark),
                typeof(TypeofBranchBenchmark),
                typeof(ColdPathSplitBenchmark),
                typeof(LocalFunctionClosureBenchmark),
                typeof(TryPatternBenchmark),
                typeof(SealedDevirtBenchmark),
                typeof(UnsafeAccessorBenchmark),
                typeof(RangeCheckBenchmark),
                typeof(LazyAllocationBenchmark),
                typeof(SharedEmptyBenchmark),
                typeof(StaticLambdaBenchmark),
                typeof(BoxingCacheBenchmark),
                typeof(InliningBenchmark),
                typeof(SkipLocalsInitBenchmark),
                typeof(PowerOfTwoMaskBenchmark),
                typeof(BufferWriterSlimBenchmark),
                typeof(MemoryOwnerBenchmark),
                typeof(BatchBenchmark),
                typeof(BitwiseComparerBenchmark),
                typeof(TypeMapBenchmark),
                typeof(HandlerListBenchmark),
                typeof(StructPassBenchmark),
                typeof(HashCompareBenchmark),
                typeof(InlineArrayBenchmark),
                typeof(ParamsSpanBenchmark),
                typeof(StringBuildBenchmark),
                typeof(SearchValuesBenchmark),
                typeof(ImmutableBuildBenchmark),
                typeof(ListReuseBenchmark),
                typeof(StaticArtifactBenchmark),
                typeof(PipelineComposeBenchmark),
                typeof(ObjectPoolBenchmark),
                typeof(FixedFieldFormatBenchmark),
                typeof(ValueTaskBenchmark),
                typeof(SchedulerPrimitiveBenchmark),
                typeof(StreamBufferingBenchmark),
                typeof(RingSplitBenchmark),
                typeof(OrdinalResolveBenchmark),
                typeof(EmitStrategyBenchmark),
                typeof(SampledNameTableBenchmark),
#if NET9_0_OR_GREATER
                typeof(SampledNameTableSpanKeyBenchmark),
#endif
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

    private static void VerifyPatternImplementations()
    {
        // TYP-01: 4 経路が同じ値を返すこと
        var typeMap = new TypeMapBenchmark();
        typeMap.Setup();
        if (!string.Equals(typeMap.DictionaryLookup(), "guid", StringComparison.Ordinal) ||
            !string.Equals(typeMap.FrozenLookup(), "guid", StringComparison.Ordinal) ||
            !string.Equals(typeMap.TypeMapGeneric(), "guid", StringComparison.Ordinal) ||
            !string.Equals(typeMap.TypeMapRuntimeType(), "guid", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verify failed. TypeMap");
        }

        // DSP-03: 購読者数ぶん通知されること
        var handlerList = new HandlerListBenchmark { Subscribers = 4 };
        handlerList.Setup();
        var afterMulticast = handlerList.MulticastDelegate();
        var afterArray = handlerList.HandlerArray();
        if ((afterMulticast != 4) || (afterArray != 8))
        {
            throw new InvalidOperationException("Verify failed. HandlerList");
        }

        // BIT-02 / COL-04: 全経路が同じ合計になること
        var expected = 15 * 16 / 2;
        var nameTable = new SampledNameTableBenchmark { Columns = 16 };
        nameTable.Setup();
        if ((nameTable.DictionaryLookup() != expected) ||
            (nameTable.LinearScan() != expected) ||
            (nameTable.SampledHashTable() != expected))
        {
            throw new InvalidOperationException("Verify failed. SampledNameTable");
        }

#if NET9_0_OR_GREATER
        var spanKeyTable = new SampledNameTableSpanKeyBenchmark { Columns = 16 };
        spanKeyTable.Setup();
        if ((spanKeyTable.DictionaryAlternateLookup() != expected) ||
            (spanKeyTable.FrozenAlternateLookup() != expected) ||
            (spanKeyTable.SampledHashTable() != expected))
        {
            throw new InvalidOperationException("Verify failed. SampledNameTableSpanKey");
        }
#endif
    }

    private static void VerifyUnmeasuredBatch()
    {
        // MEM-06: 値渡し / in 渡しが同じ結果になること
        var structPass = new StructPassBenchmark();
        structPass.Setup();
        if ((structPass.Size8ByValue() != structPass.Size8ByIn()) ||
            (structPass.Size32ByValue() != structPass.Size32ByIn()) ||
            (structPass.Size64ByValue() != structPass.Size64ByIn()) ||
            (structPass.InWithReadonlyMember() != structPass.InWithMutableMember()))
        {
            throw new InvalidOperationException("Verify failed. StructPass");
        }

        // STK-08 / STK-09 / COL-06: 各バリアントの結果一致
        var inlineArray = new InlineArrayBenchmark();
        if ((inlineArray.NewArray() != 28) || (inlineArray.Stackalloc() != 28) || (inlineArray.InlineArrayBuffer() != 28))
        {
            throw new InvalidOperationException("Verify failed. InlineArray");
        }

        var paramsSpan = new ParamsSpanBenchmark();
        paramsSpan.Setup();
        if (paramsSpan.ParamsArray() != paramsSpan.ParamsSpan())
        {
            throw new InvalidOperationException("Verify failed. ParamsSpan");
        }

        var immutable = new ImmutableBuildBenchmark { Count = 256 };
        immutable.Setup();
        if ((immutable.ToImmutableArrayExtension() != 256) || (immutable.BuilderToImmutable() != 256) || (immutable.BuilderMoveToImmutable() != 256))
        {
            throw new InvalidOperationException("Verify failed. ImmutableBuild");
        }

        var listReuse = new ListReuseBenchmark { Count = 256 };
        listReuse.Setup();
        if ((listReuse.NewListNoCapacity() != 256) || (listReuse.NewListWithCapacity() != 256) ||
            (listReuse.ReuseWithClear() != 256) || (listReuse.ReuseWithSetCountSpan() != 256))
        {
            throw new InvalidOperationException("Verify failed. ListReuse");
        }

        // TXT-07: 文字列組み立て 5 方式が同じ文字列を返すこと
        var stringBuild = new StringBuildBenchmark();
        stringBuild.Setup();
        var expectedText = stringBuild.Interpolation();
        if (!string.Equals(expectedText, stringBuild.Concat(), StringComparison.Ordinal) ||
            !string.Equals(expectedText, stringBuild.StringBuilderCapacity(), StringComparison.Ordinal) ||
            !string.Equals(expectedText, stringBuild.ValueStringBuilderBuild(), StringComparison.Ordinal) ||
            !string.Equals(expectedText, stringBuild.StringCreate(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verify failed. StringBuild");
        }

        // TXT-08: 検索位置の一致
        var searchValues = new SearchValuesBenchmark { Candidates = 8 };
        searchValues.Setup();
        if (searchValues.IndexOfAnyArray() != searchValues.IndexOfAnySearchValues())
        {
            throw new InvalidOperationException("Verify failed. SearchValues");
        }

        // TYP-06: 3 経路が同じ SQL を返すこと
        var staticArtifact = new StaticArtifactBenchmark();
        staticArtifact.Setup();
        if ((staticArtifact.BuildEveryCall() != staticArtifact.DictionaryCache()) ||
            (staticArtifact.BuildEveryCall() != staticArtifact.StaticGenericField()))
        {
            throw new InvalidOperationException("Verify failed. StaticArtifact");
        }
    }

    private static void VerifyUnmeasuredBatch2()
    {
        // DSP-05: 合成方式によらず同じ結果になること(((10+100)-3)*2)+1 = 215
        var pipeline = new PipelineComposeBenchmark();
        pipeline.Setup();
        if ((pipeline.ComposeEveryCall() != 215) || (pipeline.PreComposed() != 215) || (pipeline.TerminalDirect() != 110))
        {
            throw new InvalidOperationException("Verify failed. PipelineCompose");
        }

        // BUF-07: プール経由でも同じ文字列になること
        var pool = new ObjectPoolBenchmark();
        pool.Setup();
        if (!string.Equals(pool.NewEveryTime(), "key:customer:12345", StringComparison.Ordinal) ||
            !string.Equals(pool.ThreadStaticPool(), "key:customer:12345", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verify failed. ObjectPool");
        }

        // TXT-09: 3 方式が同じフィールド内容を生成し、トリムが一致すること
        var fixedField = new FixedFieldFormatBenchmark();
        fixedField.Setup();
        fixedField.TryFormatThenFill();
        var expectedField = fixedField.SnapshotField();
        fixedField.ManualLsbThenReverse();
        var lsbField = fixedField.SnapshotField();
        fixedField.ManualRightAlignShift();
        var shiftField = fixedField.SnapshotField();
        if (!string.Equals(expectedField, "12345678    ", StringComparison.Ordinal) ||
            !string.Equals(lsbField, expectedField, StringComparison.Ordinal) ||
            !string.Equals(shiftField, expectedField, StringComparison.Ordinal) ||
            (fixedField.TrimManualLoop() != 10) ||
            (fixedField.TrimVectorized() != 10))
        {
            throw new InvalidOperationException("Verify failed. FixedFieldFormat");
        }

        // ASY-05: 4 経路とも同じ合計になること
        var valueTask = new ValueTaskBenchmark();
        valueTask.Setup();
        if ((valueTask.TaskFromResult().GetAwaiter().GetResult() != 1234500L) ||
            (valueTask.ValueTaskDirect().GetAwaiter().GetResult() != 1234500L) ||
            (valueTask.AsyncMethodTask().GetAwaiter().GetResult() != 1234500L) ||
            (valueTask.AsyncMethodValueTask().GetAwaiter().GetResult() != 1234500L))
        {
            throw new InvalidOperationException("Verify failed. ValueTask");
        }

        // ASY-06: 通知が成立すること
        var scheduler = new SchedulerPrimitiveBenchmark();
        scheduler.TimerPerJob();
        if (!scheduler.TcsSwapNotify())
        {
            throw new InvalidOperationException("Verify failed. SchedulerPrimitive");
        }

        // ASY-07: 全読み・逐次読みの合計一致
        var streaming = new StreamBufferingBenchmark();
        streaming.Setup();
        if (streaming.FullBufferThenProcess() != streaming.StreamingPooledChunks())
        {
            throw new InvalidOperationException("Verify failed. StreamBuffering");
        }
    }

    private static void VerifyUnmeasuredBatch3()
    {
        // SEQ-05: 素朴版と増分版が同じ合計になること
        var ringSplit = new RingSplitBenchmark();
        ringSplit.Setup();
        if (ringSplit.NaiveRescanCompact() != ringSplit.IncrementalDeferredCompact())
        {
            throw new InvalidOperationException("Verify failed. RingSplit");
        }

        // DAT-01: 3 経路が同じ合計になること(id 合計 499500 + 名前長 5000 + 偶数フラグ 500)
        var ordinal = new OrdinalResolveBenchmark();
        ordinal.Setup();
        if ((ordinal.GetOrdinalPerRow() != 505000L) ||
            (ordinal.CachedOrdinalsStruct() != 505000L) ||
            (ordinal.CachedOrdinalsGetValueBoxing() != 505000L))
        {
            throw new InvalidOperationException("Verify failed. OrdinalResolve");
        }

        // GEN-01: 全ファクトリが正しい依存で GenService を構築すること
        var emit = new EmitStrategyBenchmark();
        emit.Setup();
        foreach (var created in new[] { emit.DirectLambda(), emit.EmitHolderField(), emit.EmitClosureArray(), emit.EmitChainedCallvirt(), emit.EmitChainedCall() })
        {
            if (created is not GenService { A: not null, B: not null })
            {
                throw new InvalidOperationException("Verify failed. EmitStrategy");
            }
        }
    }

    private static void VerifyImplementationBatch3()
    {
        // BUF-03: 3 実装が同じチェックサムになること(16 バイト刻み、チャンク合計 120 × 回数)
        var writerSlim = new BufferWriterSlimBenchmark { TotalBytes = 4096 };
        writerSlim.Setup();
        var expectedChecksum = 120 * (4096 / 16);
        if ((writerSlim.ArrayBufferWriter() != expectedChecksum) ||
            (writerSlim.PooledWriter() != expectedChecksum) ||
            (writerSlim.WriterSlim() != expectedChecksum))
        {
            throw new InvalidOperationException("Verify failed. BufferWriterSlim");
        }

        // BUF-04: 4 実装が同じ合計になること
        var memoryOwner = new MemoryOwnerBenchmark();
        var expectedSum = memoryOwner.NewArray();
        if ((memoryOwner.ArrayPoolRaw() != expectedSum) ||
            (memoryOwner.MemoryOwnerAllocate() != expectedSum) ||
            (memoryOwner.TemporaryBufferPooled() != expectedSum))
        {
            throw new InvalidOperationException("Verify failed. MemoryOwner");
        }

        // SEQ-04 / STK-03: 3 実装が同じ合計になること(0..1023 の総和)
        var batch = new BatchBenchmark();
        batch.Setup();
        var expectedTotal = 1023L * 1024 / 2;
        if ((batch.LinqChunk() != expectedTotal) ||
            (batch.ArrayBatch() != expectedTotal) ||
            (batch.SpanBatch() != expectedTotal))
        {
            throw new InvalidOperationException("Verify failed. Batch");
        }

        // TYP-02: 3 経路が同じ合計になること(0..15 の総和)
        var bitwise = new BitwiseComparerBenchmark();
        bitwise.Setup();
        if ((bitwise.DefaultComparerPlain() != 120) ||
            (bitwise.DefaultComparerEquatable() != 120) ||
            (bitwise.BitwiseComparerPlain() != 120))
        {
            throw new InvalidOperationException("Verify failed. BitwiseComparer");
        }
    }

    private static void VerifyStarFiveBatch()
    {
        // MEM-03: 初期化スキップの有無で結果が変わらないこと(書き込み位置のみ読む)
        var skipLocals = new SkipLocalsInitBenchmark { Size = 512 };
        if ((skipLocals.ZeroInit() != 3 * 16) || (skipLocals.SkipInit() != 3 * 16))
        {
            throw new InvalidOperationException("Verify failed. SkipLocalsInit");
        }

        // BIT-03: 3 方式が同じバケット合計になること
        var mask = new PowerOfTwoMaskBenchmark();
        mask.Setup();
        var expected = mask.RuntimeSizeModulo();
        if ((mask.PowerOfTwoMask() != expected) || (mask.ConstSizeModulo() != expected))
        {
            throw new InvalidOperationException("Verify failed. PowerOfTwoMask");
        }
    }

    private static void VerifyStarFourBatchA()
    {
        // BIT-01: 2 方式のカウント一致
        var range = new RangeCheckBenchmark();
        range.Setup();
        if (range.TwoComparisons() != range.UnsignedSingleComparison())
        {
            throw new InvalidOperationException("Verify failed. RangeCheck");
        }

        // STK-07: 遅延確保でも同じエラー数、全成功時はゼロ
        var lazy = new LazyAllocationBenchmark();
        lazy.Setup();
        if ((lazy.EagerList() != 10) || (lazy.LazyList() != 10) || (lazy.LazyListAllValid() != 0))
        {
            throw new InvalidOperationException("Verify failed. LazyAllocation");
        }

        var shared = new SharedEmptyBenchmark();
        if ((shared.NewEmptyArray() != 0) || (shared.SharedEmptyArray() != 0))
        {
            throw new InvalidOperationException("Verify failed. SharedEmpty");
        }

        // DSP-04: キャプチャ形と state 形の結果一致(0..15 の合計 × ループ回数/16)
        var lambda = new StaticLambdaBenchmark();
        lambda.Setup();
        if (lambda.CaptureLocal() != lambda.StaticWithState())
        {
            throw new InvalidOperationException("Verify failed. StaticLambda");
        }

        // STK-05: ボックス経路によらず合計一致(-1/0/1 の列)
        var boxing = new BoxingCacheBenchmark();
        boxing.Setup();
        if (boxing.DirectBoxing() != boxing.CachedBox())
        {
            throw new InvalidOperationException("Verify failed. BoxingCache");
        }

        // JIT-01: 3 方式の結果一致
        var inlining = new InliningBenchmark();
        inlining.Setup();
        if ((inlining.DefaultPolicy() != inlining.Aggressive()) || (inlining.DefaultPolicy() != inlining.NoInline()))
        {
            throw new InvalidOperationException("Verify failed. Inlining");
        }
    }

    private static void VerifyStarFourBatchB()
    {
        // MEM-04: 3 方式の合計一致(0..1023 の A + 2A)
        var structArray = new StructArrayRefBenchmark();
        structArray.Setup();
        var expectedEntries = 3L * 1023 * 1024 / 2;
        if ((structArray.ClassArray() != expectedEntries) ||
            (structArray.StructArrayCopy() != expectedEntries) ||
            (structArray.StructArrayRef() != expectedEntries))
        {
            throw new InvalidOperationException("Verify failed. StructArrayRef");
        }

        // MEM-01: 3 方式の合計一致
        var dualSpan = new DualSpanWalkBenchmark();
        dualSpan.Setup();
        if ((dualSpan.Indexed() != dualSpan.IndexedPreSliced()) || (dualSpan.Indexed() != dualSpan.RefWalk()))
        {
            throw new InvalidOperationException("Verify failed. DualSpanWalk");
        }

        // JIT-03: 特殊化経路が手書きと一致し、フォールバックは長さを返すこと
        var typeofBranch = new TypeofBranchBenchmark();
        typeofBranch.Setup();
        if ((typeofBranch.HandwrittenIntSum() != typeofBranch.GenericWithTypeofBranch()) ||
            (TypeofBranchBenchmark.SumFallback(new long[8]) != 8))
        {
            throw new InvalidOperationException("Verify failed. TypeofBranch");
        }

        // JIT-04: 両実装の書き込み数一致
        var coldPath = new ColdPathSplitBenchmark();
        coldPath.Setup();
        if ((coldPath.FatMethod() != 1024) || (coldPath.SplitColdPath() != 1024))
        {
            throw new InvalidOperationException("Verify failed. ColdPathSplit");
        }

        // STK-04: キャプチャ形と static 形の結果一致
        var localFunction = new LocalFunctionClosureBenchmark();
        localFunction.Setup();
        if (localFunction.CapturingLocalFunction() != localFunction.StaticLocalFunction())
        {
            throw new InvalidOperationException("Verify failed. LocalFunctionClosure");
        }

        // TXT-03: 例外形と Try 形の結果一致
        var tryPattern = new TryPatternBenchmark();
        tryPattern.Setup();
        if (tryPattern.ExceptionControlFlow() != tryPattern.TryPattern())
        {
            throw new InvalidOperationException("Verify failed. TryPattern");
        }

        // DSP-01: 3 方式の合計一致
        var devirt = new SealedDevirtBenchmark();
        devirt.Setup();
        var expectedSum = 1023L * 1024 / 2;
        if ((devirt.OpenInterface() != expectedSum) ||
            (devirt.SealedInterface() != expectedSum) ||
            (devirt.SealedConcrete() != expectedSum))
        {
            throw new InvalidOperationException("Verify failed. SealedDevirt");
        }

        // TYP-03: 3 経路の読み出し一致
        var accessor = new UnsafeAccessorBenchmark();
        accessor.Setup();
        if ((accessor.PublicProperty() != 4200) ||
            (accessor.UnsafeAccessorField() != 4200) ||
            (accessor.ReflectionGetValue() != 4200))
        {
            throw new InvalidOperationException("Verify failed. UnsafeAccessor");
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
