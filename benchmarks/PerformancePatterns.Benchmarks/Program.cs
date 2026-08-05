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
        // Verify all variants agree before measuring (benchmark-methodology.md)
        VerifySpanTokenizer();
        VerifyTemporaryBuffer();
        VerifyValueStringBuilder();
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
        VerifyStarThreeBatch();

        // Example: dotnet run -c Release --framework net10.0 -- --filter "*"
        BenchmarkSwitcher
            .FromTypes(
            [
                typeof(SpanTokenizerBenchmark),
#if NET9_0_OR_GREATER
                typeof(SpanTokenizerBclComparisonBenchmark),
#endif
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
                typeof(ChannelsBenchmark),
                typeof(PipelinesBenchmark),
                typeof(AsyncEnumerableBenchmark),
                typeof(DisposeGuardBenchmark),
                typeof(SliceStyleBenchmark),
                typeof(ArrayDataReferenceBenchmark),
                typeof(StructStreamIoBenchmark),
                typeof(FrozenBuildBenchmark),
                typeof(FrozenLookupBenchmark),
                typeof(UnsafeAsCastBenchmark),
                typeof(CallAbstractionBenchmark),
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
            // Verify against a copy to avoid interned literals
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
        // The bounds-check-elimination variants must produce the same sum
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

        // The copy variants must produce the same result
        var copyConstant = new CopyConstantBenchmark();
        copyConstant.Setup();
        if ((copyConstant.SpanCopyTo8() != copyConstant.CopyBlockUnaligned8()) ||
            (copyConstant.SpanCopyTo16() != copyConstant.CopyBlockUnaligned16()) ||
            (copyConstant.SpanCopyTo64() != copyConstant.CopyBlockUnaligned64()))
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
        // SetCount + Span writes must produce the same content as the Add loop
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

        // The concrete-type dispatch, iteration and dictionary counting variants must agree
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
        // The three token-matching approaches must agree (64 probes x 4 kinds, match ID total = 160)
        var tokenMatch = new TokenMatchBenchmark();
        tokenMatch.Setup();
        if ((tokenMatch.StringSwitch() != 160) || (tokenMatch.SequenceEqualChain() != 160) || (tokenMatch.UIntConstantCompare() != 160))
        {
            throw new InvalidOperationException("Verify failed. TokenMatch");
        }

        // The three UTF-8 formatting approaches must produce the same bytes
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

        // The three ASCII comparison approaches must agree (all 8 pairs)
        var ascii = new AsciiBenchmark();
        ascii.Setup();
        if ((ascii.StringEqualsIgnoreCase() != 8) || (ascii.AsciiEqualsIgnoreCase() != 8) || (ascii.ManualOr20Compare() != 8))
        {
            throw new InvalidOperationException("Verify failed. Ascii");
        }

        // The three BufferWriter approaches must agree in length and content
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
        // The four async forwarding approaches must produce the same result (42 x 100)
        var asyncElision = new AsyncElisionBenchmark();
        if ((asyncElision.TaskAwaitForward().GetAwaiter().GetResult() != 4200) ||
            (asyncElision.TaskDirectForward().GetAwaiter().GetResult() != 4200) ||
            (asyncElision.ValueTaskAwaitForward().GetAwaiter().GetResult() != 4200) ||
            (asyncElision.ValueTaskDirectForward().GetAwaiter().GetResult() != 4200))
        {
            throw new InvalidOperationException("Verify failed. AsyncElision");
        }

        // The two bit scanning / counting approaches must agree
        var bitOperations = new BitOperationsBenchmark();
        bitOperations.Setup();
        if ((bitOperations.SetBitScanLoop() != bitOperations.SetBitScanTzcnt()) ||
            (bitOperations.PopCountManual() != bitOperations.PopCountIntrinsic()))
        {
            throw new InvalidOperationException("Verify failed. BitOperations");
        }

        // The four pinned buffer approaches must produce the same result
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
        // The three SIMD approaches must produce the same sum
        var vectorSum = new VectorSumBenchmark();
        vectorSum.Setup();
        var expectedVector = vectorSum.ScalarSum();
        if ((vectorSum.EnumerableSum() != expectedVector) ||
            (vectorSum.VectorTSum() != expectedVector) ||
            (vectorSum.Vector256Sum() != expectedVector))
        {
            throw new InvalidOperationException("Verify failed. VectorSum");
        }

        // The three cursor approaches must produce the same sum
        var cursor = new RefFieldCursorBenchmark();
        cursor.Setup();
        var expectedCursor = 1023 * 1024 / 2;
        if ((cursor.SumSpanIndex() != expectedCursor) ||
            (cursor.SumSpanCursor() != expectedCursor) ||
            (cursor.SumRefFieldCursor() != expectedCursor))
        {
            throw new InvalidOperationException("Verify failed. RefFieldCursor");
        }

        // Channels / Pipe / IAsyncEnumerable must produce the same sum
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
        // TYP-01: All four paths must return the same value
        var typeMap = new TypeMapBenchmark();
        typeMap.Setup();
        if (!string.Equals(typeMap.DictionaryLookup(), "guid", StringComparison.Ordinal) ||
            !string.Equals(typeMap.FrozenLookup(), "guid", StringComparison.Ordinal) ||
            !string.Equals(typeMap.TypeMapGeneric(), "guid", StringComparison.Ordinal) ||
            !string.Equals(typeMap.TypeMapRuntimeType(), "guid", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verify failed. TypeMap");
        }

        // DSP-03: One notification must be delivered per subscriber
        var handlerList = new HandlerListBenchmark { Subscribers = 4 };
        handlerList.Setup();
        var afterMulticast = handlerList.MulticastDelegate();
        var afterArray = handlerList.HandlerArray();
        if ((afterMulticast != 4) || (afterArray != 8))
        {
            throw new InvalidOperationException("Verify failed. HandlerList");
        }

        // BIT-01 / COL-04: All paths must produce the same sum
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
        // MEM-04: Passing by value and passing by in must produce the same result
        var structPass = new StructPassBenchmark();
        structPass.Setup();
        if ((structPass.Size8ByValue() != structPass.Size8ByIn()) ||
            (structPass.Size32ByValue() != structPass.Size32ByIn()) ||
            (structPass.Size64ByValue() != structPass.Size64ByIn()) ||
            (structPass.InWithReadonlyMember() != structPass.InWithMutableMember()))
        {
            throw new InvalidOperationException("Verify failed. StructPass");
        }

        // STK-08 / STK-09 / COL-06: Every variant must produce the same result
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

        // TXT-07: All five string building approaches must return the same string
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

        // TXT-08: The search positions must agree
        var searchValues = new SearchValuesBenchmark { Candidates = 8 };
        searchValues.Setup();
        if (searchValues.IndexOfAnyArray() != searchValues.IndexOfAnySearchValues())
        {
            throw new InvalidOperationException("Verify failed. SearchValues");
        }

        // TYP-06: All three paths must return the same SQL
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
        // DSP-05: The result must be the same regardless of composition style: (((10+100)-3)*2)+1 = 215
        var pipeline = new PipelineComposeBenchmark();
        pipeline.Setup();
        if ((pipeline.ComposeEveryCall() != 215) || (pipeline.PreComposed() != 215) || (pipeline.TerminalDirect() != 110))
        {
            throw new InvalidOperationException("Verify failed. PipelineCompose");
        }

        // BUF-07: Going through the pool must produce the same string
        var pool = new ObjectPoolBenchmark();
        pool.Setup();
        if (!string.Equals(pool.NewEveryTime(), "key:customer:12345", StringComparison.Ordinal) ||
            !string.Equals(pool.ThreadStaticPool(), "key:customer:12345", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Verify failed. ObjectPool");
        }

        // TXT-09: All three approaches must produce the same field content and the same trim result
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

        // ASY-05: All four paths must produce the same sum
        var valueTask = new ValueTaskBenchmark();
        valueTask.Setup();
        if ((valueTask.TaskFromResult().GetAwaiter().GetResult() != 1234500L) ||
            (valueTask.ValueTaskDirect().GetAwaiter().GetResult() != 1234500L) ||
            (valueTask.AsyncMethodTask().GetAwaiter().GetResult() != 1234500L) ||
            (valueTask.AsyncMethodValueTask().GetAwaiter().GetResult() != 1234500L))
        {
            throw new InvalidOperationException("Verify failed. ValueTask");
        }

        // ASY-06: The notification must be delivered
        var scheduler = new SchedulerPrimitiveBenchmark();
        scheduler.TimerPerJob();
        if (!scheduler.TcsSwapNotify())
        {
            throw new InvalidOperationException("Verify failed. SchedulerPrimitive");
        }

        // ASY-07: Reading everything at once and reading in chunks must produce the same sum
        var streaming = new StreamBufferingBenchmark();
        streaming.Setup();
        if (streaming.FullBufferThenProcess() != streaming.StreamingPooledChunks())
        {
            throw new InvalidOperationException("Verify failed. StreamBuffering");
        }
    }

    private static void VerifyUnmeasuredBatch3()
    {
        // SEQ-05: The naive and incremental versions must produce the same sum
        var ringSplit = new RingSplitBenchmark();
        ringSplit.Setup();
        if (ringSplit.NaiveRescanCompact() != ringSplit.IncrementalDeferredCompact())
        {
            throw new InvalidOperationException("Verify failed. RingSplit");
        }

        // DAT-01: All three paths must produce the same sum (id total 499500 + name length 5000 + even flag 500)
        var ordinal = new OrdinalResolveBenchmark();
        ordinal.Setup();
        if ((ordinal.GetOrdinalPerRow() != 505000L) ||
            (ordinal.CachedOrdinalsStruct() != 505000L) ||
            (ordinal.CachedOrdinalsGetValueBoxing() != 505000L))
        {
            throw new InvalidOperationException("Verify failed. OrdinalResolve");
        }

        // GEN-01: Every factory must build GenService with the correct dependencies
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
        // BUF-03: All three implementations must produce the same checksum (16-byte steps, chunk total 120 x count)
        var writerSlim = new BufferWriterSlimBenchmark { TotalBytes = 4096 };
        writerSlim.Setup();
        var expectedChecksum = 120 * (4096 / 16);
        if ((writerSlim.ArrayBufferWriter() != expectedChecksum) ||
            (writerSlim.PooledWriter() != expectedChecksum) ||
            (writerSlim.WriterSlim() != expectedChecksum))
        {
            throw new InvalidOperationException("Verify failed. BufferWriterSlim");
        }

        // BUF-04: All four implementations must produce the same sum
        var memoryOwner = new MemoryOwnerBenchmark();
        var expectedSum = memoryOwner.NewArray();
        if ((memoryOwner.ArrayPoolRaw() != expectedSum) ||
            (memoryOwner.MemoryOwnerAllocate() != expectedSum) ||
            (memoryOwner.TemporaryBufferPooled() != expectedSum))
        {
            throw new InvalidOperationException("Verify failed. MemoryOwner");
        }

        // SEQ-04 / STK-03: All three implementations must produce the same sum (the total of 0..1023)
        var batch = new BatchBenchmark();
        batch.Setup();
        var expectedTotal = 1023L * 1024 / 2;
        if ((batch.LinqChunk() != expectedTotal) ||
            (batch.ArrayBatch() != expectedTotal) ||
            (batch.SpanBatch() != expectedTotal))
        {
            throw new InvalidOperationException("Verify failed. Batch");
        }

        // TYP-02: All three paths must produce the same sum (the total of 0..15)
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
        // MEM-01: Skipping initialization must not change the result (only written positions are read)
        var skipLocals = new SkipLocalsInitBenchmark { Size = 512 };
        if ((skipLocals.ZeroInit() != 3 * 16) || (skipLocals.SkipInit() != 3 * 16))
        {
            throw new InvalidOperationException("Verify failed. SkipLocalsInit");
        }

        // BIT-02: All three approaches must produce the same bucket total
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
        // BIT-01: The two approaches must produce the same count
        var range = new RangeCheckBenchmark();
        range.Setup();
        if (range.TwoComparisons() != range.UnsignedSingleComparison())
        {
            throw new InvalidOperationException("Verify failed. RangeCheck");
        }

        // STK-07: Lazy allocation must yield the same error count, and zero when everything succeeds
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

        // DSP-04: The capturing and state-passing forms must produce the same result (the total of 0..15 x loop count / 16)
        var lambda = new StaticLambdaBenchmark();
        lambda.Setup();
        if (lambda.CaptureLocal() != lambda.StaticWithState())
        {
            throw new InvalidOperationException("Verify failed. StaticLambda");
        }

        // STK-05: The sum must match regardless of the boxing path (a sequence of -1/0/1)
        var boxing = new BoxingCacheBenchmark();
        boxing.Setup();
        if (boxing.DirectBoxing() != boxing.CachedBox())
        {
            throw new InvalidOperationException("Verify failed. BoxingCache");
        }

        // JIT-01: The three approaches must produce the same result
        var inlining = new InliningBenchmark();
        inlining.Setup();
        if ((inlining.DefaultPolicy() != inlining.Aggressive()) || (inlining.DefaultPolicy() != inlining.NoInline()))
        {
            throw new InvalidOperationException("Verify failed. Inlining");
        }
    }

    private static void VerifyStarFourBatchB()
    {
        // MEM-02: The three approaches must produce the same sum (A + 2A over 0..1023)
        var structArray = new StructArrayRefBenchmark();
        structArray.Setup();
        var expectedEntries = 3L * 1023 * 1024 / 2;
        if ((structArray.ClassArray() != expectedEntries) ||
            (structArray.StructArrayCopy() != expectedEntries) ||
            (structArray.StructArrayRef() != expectedEntries))
        {
            throw new InvalidOperationException("Verify failed. StructArrayRef");
        }

        // MEM-01: The three approaches must produce the same sum
        var dualSpan = new DualSpanWalkBenchmark();
        dualSpan.Setup();
        if ((dualSpan.Indexed() != dualSpan.IndexedPreSliced()) || (dualSpan.Indexed() != dualSpan.RefWalk()))
        {
            throw new InvalidOperationException("Verify failed. DualSpanWalk");
        }

        // JIT-03: The specialized path must match the handwritten one, and the fallback must return the length
        var typeofBranch = new TypeofBranchBenchmark();
        typeofBranch.Setup();
        if ((typeofBranch.HandwrittenIntSum() != typeofBranch.GenericWithTypeofBranch()) ||
            (TypeofBranchBenchmark.SumFallback(new long[8]) != 8))
        {
            throw new InvalidOperationException("Verify failed. TypeofBranch");
        }

        // JIT-04: Both implementations must write the same number of items
        var coldPath = new ColdPathSplitBenchmark();
        coldPath.Setup();
        if ((coldPath.FatMethod() != 1024) || (coldPath.SplitColdPath() != 1024))
        {
            throw new InvalidOperationException("Verify failed. ColdPathSplit");
        }

        // STK-04: The capturing and static forms must produce the same result
        var localFunction = new LocalFunctionClosureBenchmark();
        localFunction.Setup();
        if (localFunction.CapturingLocalFunction() != localFunction.StaticLocalFunction())
        {
            throw new InvalidOperationException("Verify failed. LocalFunctionClosure");
        }

        // TXT-03: The exception-based and Try-based forms must produce the same result
        var tryPattern = new TryPatternBenchmark();
        tryPattern.Setup();
        if (tryPattern.ExceptionControlFlow() != tryPattern.TryPattern())
        {
            throw new InvalidOperationException("Verify failed. TryPattern");
        }

        // DSP-01: The three approaches must produce the same sum
        var devirt = new SealedDevirtBenchmark();
        devirt.Setup();
        var expectedSum = 1023L * 1024 / 2;
        if ((devirt.OpenInterface() != expectedSum) ||
            (devirt.SealedInterface() != expectedSum) ||
            (devirt.SealedConcrete() != expectedSum))
        {
            throw new InvalidOperationException("Verify failed. SealedDevirt");
        }

        // TYP-03: The three paths must read the same value
        var accessor = new UnsafeAccessorBenchmark();
        accessor.Setup();
        if ((accessor.PublicProperty() != 4200) ||
            (accessor.UnsafeAccessorField() != 4200) ||
            (accessor.ReflectionGetValue() != 4200))
        {
            throw new InvalidOperationException("Verify failed. UnsafeAccessor");
        }
    }

    private static void VerifyStarThreeBatch()
    {
        // MEM-03: The two notations must produce the same result
        var slice = new SliceStyleBenchmark();
        slice.Setup();
        if (slice.SliceMethod() != slice.RangeOperator())
        {
            throw new InvalidOperationException("Verify failed. SliceStyle");
        }

        // MEM-02: The two sequential approaches and the two random approaches must agree
        var arrayRef = new ArrayDataReferenceBenchmark();
        arrayRef.Setup();
        if ((arrayRef.SequentialFor() != arrayRef.SequentialRefWalk()) ||
            (arrayRef.RandomIndexed() != arrayRef.RandomRefAdd()))
        {
            throw new InvalidOperationException("Verify failed. ArrayDataReference");
        }

        // SEQ-03: The bulk-written image must match when it is read back field by field
        var structIo = new StructStreamIoBenchmark();
        structIo.Setup();
        var writtenBulk = structIo.WriteBulkCast();
        var writtenFields = structIo.WriteFieldByField();
        if (writtenBulk != writtenFields)
        {
            throw new InvalidOperationException("Verify failed. StructStreamIo (length)");
        }

        if ((structIo.ReadFieldByField() != 1023 * 7L) || (structIo.ReadBulkCast() != 1023 * 7L))
        {
            throw new InvalidOperationException("Verify failed. StructStreamIo (roundtrip)");
        }

        // COL-02: The build counts and the lookup totals must agree
        var frozenBuild = new FrozenBuildBenchmark { Count = 16 };
        frozenBuild.Setup();
        if ((frozenBuild.BuildDictionary() != 16) || (frozenBuild.BuildFrozen() != 16))
        {
            throw new InvalidOperationException("Verify failed. FrozenBuild");
        }

        var frozenLookup = new FrozenLookupBenchmark { Count = 16 };
        frozenLookup.Setup();
        if ((frozenLookup.LookupDictionary() != 120) || (frozenLookup.LookupFrozen() != 120))
        {
            throw new InvalidOperationException("Verify failed. FrozenLookup");
        }

        // TYP-05: The three approaches must produce the same sum
        var unsafeAs = new UnsafeAsCastBenchmark();
        unsafeAs.Setup();
        if ((unsafeAs.CastClass() != unsafeAs.IsPattern()) || (unsafeAs.CastClass() != unsafeAs.UnsafeAs()))
        {
            throw new InvalidOperationException("Verify failed. UnsafeAsCast");
        }

        // DSP-02: The five approaches must produce the same sum
        var abstraction = new CallAbstractionBenchmark();
        abstraction.Setup();
        var expected = 1023L * 1024 / 2;
        if ((abstraction.DirectSealed() != expected) ||
            (abstraction.ViaInterface() != expected) ||
            (abstraction.ViaAbstract() != expected) ||
            (abstraction.ViaDelegate() != expected) ||
            (abstraction.ViaFunctionPointer() != expected))
        {
            throw new InvalidOperationException("Verify failed. CallAbstraction");
        }
    }

    private static void VerifyValueStringBuilder()
    {
        var parts = new[] { new string('a', 24), new string('b', 24), new string('c', 24), new string('d', 24) };

        var expectedBuilder = new StringBuilder();
        var handler = new DefaultInterpolatedStringHandler(0, parts.Length, null, stackalloc char[8]);
        using var builder = new ValueStringBuilder(stackalloc char[8]); // Always exercises the Grow path
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
