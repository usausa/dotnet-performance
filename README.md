# dotnet-performance

[日本語](README.ja.md) | **English**

A collection of techniques for building .NET libraries tuned for speed and low allocation.

This README is the single source of the core knowledge (pattern taxonomy, index, and per-pattern commentary), structured so that an implementation decision can be made from this document alone. The goal is to let an AI reference this repository during library development and reproduce high-performance, AOT-ready implementations.

## 🧭 How to read this document

- Every pattern carries a unique ID (e.g. MEM-01). Examples, tests, benchmarks, and measurement results are cross-referenced by that ID
- In code samples, ✅ marks the recommended form and ❌ the form to avoid
- Measured numbers vary with environment and runtime generation. Re-measure on the target environment before adopting
- AOT markers: ✅ works as-is / ⚠️ may be incompatible depending on how it is implemented (see the per-pattern notes) / ❌ does not work under AOT
- The low-level optimizations here do not depend on reflection or dynamic code generation, so nearly all of them are AOT-ready. AOT-specific problems and workarounds are collected in [aot-compatibility.md](docs/aot-compatibility.md)

---

## 🗂️ Category structure

| Category | Contents |
|---|---|
| 💾 MEM | Memory access optimization (bounds-check elimination, data layout) |
| 🥞 STK | Stack usage and zero-allocation type design |
| 🧺 BUF | Buffer management and pooling |
| ⚙️ JIT | Assisting JIT optimization (inlining, branch elimination, specialization) |
| 🚦 DSP | Call abstraction and dispatch |
| 🏷️ TYP | Leveraging the type system (type dispatch, comparison, internal access) |
| 🔢 BIT | Bit manipulation and branchless optimization |
| 🧮 VEC | SIMD and vectorization |
| 📜 SEQ | Sequential read/write and sequence processing |
| 🗃️ COL | Collection optimization |
| 🔤 TXT | Strings and formatting |
| 🔄 ASY | Asynchrony |
| 🔒 CON | Concurrency and synchronization |
| 🖥️ SYS | System and OS facilities |
| 🗄️ DAT | Data access |
| 🏭 GEN | Code generation |

## 📋 Pattern index (summary)

| ID | Pattern | Goal | AOT | Example |
|---|---|---|:---:|:---:|
| [MEM-01](#-mem-01-skiplocalsinit) | SkipLocalsInit | Skip zero-initialization of locals | ✅ | [Verified](benchmarks/results/MEM-01-SkipLocalsInit.md) |
| [MEM-02](#-mem-02-struct-element-array--ref-access-data-oriented-layout) | struct element array + ref access | Eliminate per-element heap allocation and indirection | ✅ | [Implemented](src/PerformancePatterns/Typ/TypeMap.cs) |
| [MEM-03](#-mem-03-explicit-slicing-with-sliceoffset-length) | Explicit Slice(offset, length) | Tighter slicing codegen than the range operator | ✅ | [Verified](benchmarks/results/MEM-03-SliceStyle.md) |
| [MEM-04](#-mem-04-passing-struct-arguments-by-in--ref) | Passing struct arguments by in / ref | Avoid value copies of large structs | ✅ | [Verified](benchmarks/results/MEM-04-StructPass.md) |
| [MEM-05](#-mem-05-struct-layout-optimization-size-and-field-order) | Struct layout optimization | Cut footprint through size and field order | ✅ | [Verified](benchmarks/results/MEM-05-StructLayout.md) |
| [STK-01](#-stk-01-ref-struct-stack-only-type) | ref struct (stack-only type) | Ban heap escape at the type level | ✅ | [Implemented](src/PerformancePatterns/Txt/ValueStringBuilder.cs) |
| [STK-02](#-stk-02-zero-copy-access-with-spant--readonlyspant) | Span\<T\> / ReadOnlySpan\<T\> | Zero-copy typed view | ✅ | [Implemented](src/PerformancePatterns/Seq/SpanTokenizer.cs) |
| [STK-03](#-stk-03-struct-iterator-pattern) | struct iterator pattern | Remove virtual calls and heap allocation from foreach | ✅ | [Implemented](src/PerformancePatterns/Seq/BatchExtensions.cs) |
| [STK-04](#-stk-04-optimizing-iterators-with-static-local-methods) | static local method iterator | Eager validation plus closure prevention | ✅ | [Verified](benchmarks/results/STK-04-LocalFunctionClosure.md) |
| [STK-05](#-stk-05-boxing-avoidance-and-hot-value-caching) | Boxing avoidance and hot-value cache | Remove allocation at object boundaries | ✅ | [Verified](benchmarks/results/STK-05-BoxingCache.md) |
| [STK-06](#-stk-06-constant-size-stackalloc) | Constant-size stackalloc | Avoid localloc and control zero-initialization | ✅ | [Verified](benchmarks/results/STK-06-StackallocSize.md) |
| [STK-07](#-stk-07-lazy-allocation-and-shared-singletons) | Lazy allocation and shared singletons | Allocate only when used; share the empty instance | ✅ | [Verified](benchmarks/results/STK-07-LazyAllocation.md) |
| [STK-08](#-stk-08-fixed-length-buffers-inside-structs-with-inlinearray) | InlineArray | Fixed-length buffer inside a struct (.NET 8+) | ✅ | [Verified](benchmarks/results/STK-08-InlineArray.md) |
| [STK-09](#-stk-09-params-readonlyspant) | params ReadOnlySpan\<T\> | Remove the array allocation for variadic arguments (C# 13) | ✅ | [Verified](benchmarks/results/STK-09-ParamsSpan.md) |
| [STK-10](#-stk-10-ref-field-cursor-for-structured-reads) | ref field cursor | Sequential reads of differing field widths | ✅ | [Verified](benchmarks/results/STK-10-RefFieldStructRead.md) |
| [BUF-01](#-buf-01-buffer-reuse-with-arraypoolt) | ArrayPool\<T\> | Reduce GC pressure from throwaway buffers | ✅ | [Implemented](src/PerformancePatterns/Buf/TemporaryBuffer.cs) |
| [BUF-02](#-buf-02-ibufferwritert--getspan--advance-pattern) | IBufferWriter\<T\> + GetSpan / Advance | Write directly into the output buffer | ✅ | [Implemented](src/PerformancePatterns/Buf/PooledBufferWriter.cs) |
| [BUF-03](#-buf-03-bufferwriterslimt-stack-first-writing) | BufferWriterSlim\<T\> | Stack-first buffer writing | ✅ | [Implemented](src/PerformancePatterns/Buf/BufferWriterSlim.cs) |
| [BUF-04](#-buf-04-memoryownert-scoped-buffer-ownership) | MemoryOwner\<T\> | Add RAII scoping to pool rentals | ✅ | [Implemented](src/PerformancePatterns/Buf/MemoryOwner.cs) |
| [BUF-05](#-buf-05-tiered-temporary-buffer-strategy-stackalloc--arraypool-unified) | Tiered temporary buffer strategy | Unified threshold switch between stackalloc and pool | ✅ | [Implemented](src/PerformancePatterns/Buf/TemporaryBuffer.cs) |
| [BUF-06](#-buf-06-skipping-zero-init-with-gcallocateuninitializedarray) | GC.AllocateUninitializedArray | Skip zero-initialization when allocating large arrays | ✅ | [Verified](benchmarks/results/BUF-06-UninitializedArray.md) |
| [BUF-07](#-buf-07-reusing-reference-type-instances-with-objectpool) | ObjectPool | Reuse reference-type instances | ✅ | [Verified](benchmarks/results/BUF-07-ObjectPool.md) |
| [BUF-08](#-buf-08-working-with-memoryt-span-resolution-cost-and-array-interop) | Working with Memory\<T\> | .Span resolution cost and copy-free array interop | ✅ | [Verified](benchmarks/results/BUF-08-MemoryAccess.md) |
| [JIT-01](#️-jit-01-aggressiveinlining--aggressiveoptimization) | AggressiveInlining / AggressiveOptimization | Force inlining and optimization | ✅ | [Verified](benchmarks/results/JIT-01-Inlining.md) |
| [JIT-02](#️-jit-02-branch-elimination-via-iequatablet-constraints) | Branch elimination via IEquatable\<T\> constraint | Remove virtual dispatch from comparison | ✅ | [Verified](benchmarks/results/TYP-02-BitwiseComparer.md) |
| [JIT-03](#️-jit-03-generic-specialization-via-typeoft-branches) | typeof(T) branch specialization | Remove branches from generic conversion | ✅ | [Verified](benchmarks/results/JIT-03-TypeofBranch.md) |
| [JIT-04](#️-jit-04-cold-path-separation-throw-helpers--noinlining-on-grow) | Cold-path separation | Promote inlining of the hot path | ✅ | [Implemented](src/PerformancePatterns/Buf/BufferWriterSlim.cs) |
| [JIT-05](#️-jit-05-skipping-work-with-isreferenceorcontainsreferences) | IsReferenceOrContainsReferences branch | Skip cleanup for reference-free types | ✅ | [Verified](benchmarks/results/JIT-05-ReferenceContainsBranch.md) |
| [DSP-01](#-dsp-01-devirtualization-via-sealed) | Devirtualization via sealed | Turn virtual calls into direct calls | ✅ | [Verified](benchmarks/results/DSP-01-SealedDevirt.md) |
| [DSP-02](#-dsp-02-choosing-a-call-abstraction) | Choosing a call abstraction | When to use delegate / interface / function pointer | ✅ | [Verified](benchmarks/results/DSP-02-CallAbstraction.md) |
| [DSP-03](#-dsp-03-immutable-handler-arrays-avoiding-multicast-delegates) | Immutable array of handlers | Avoid multicast delegate degradation | ✅ | [Implemented](src/PerformancePatterns/Dsp/HandlerList.cs) |
| [DSP-04](#-dsp-04-static-lambdas-everywhere-threading-tstate-through) | static lambdas throughout | Make no-capture the default and pass state via TState | ✅ | [Verified](benchmarks/results/DSP-04-StaticLambda.md) |
| [DSP-05](#-dsp-05-precomposing-delegate-pipelines) | Pre-resolved delegate pipeline | Move runtime composition and branch resolution to initialization | ✅ | [Verified](benchmarks/results/DSP-05-PipelineCompose.md) |
| [DSP-06](#-dsp-06-index-forwarding-pipelines-delegate-free-chains) | Index-forwarding pipeline | Drop the next delegate from an interceptor chain entirely | ✅ | [Verified](benchmarks/results/DSP-06-IndexForward.md) |
| [TYP-01](#️-typ-01-static-type-slots-typemap--typeslot) | Static type slots (TypeMap / TypeSlot) | Turn Type-keyed dictionaries into array access | ⚠️ | [Implemented](src/PerformancePatterns/Typ/TypeMap.cs) |
| [TYP-02](#️-typ-02-bitwisecomparert-raw-byte-comparison) | BitwiseComparer\<T\> | Raw byte comparison of unmanaged value types | ✅ | [Implemented](src/PerformancePatterns/Typ/BitwiseComparer.cs) |
| [TYP-03](#️-typ-03-unsafeaccessor-direct-access-to-non-public-members) | UnsafeAccessor | Direct access to non-public members | ✅ | [Verified](benchmarks/results/TYP-03-UnsafeAccessor.md) |
| [TYP-04](#️-typ-04-per-type-caching-with-generic-static-classes) | Generic static per-type cache | Dictionary-free lookup of per-type artifacts | ✅ | [Implemented](src/PerformancePatterns/Typ/TypeSlot.cs) |
| [TYP-05](#️-typ-05-skipping-type-checks-on-casts-with-unsafeas) | Unsafe.As cast | Speed up casts already guaranteed by type | ✅ | [Verified](benchmarks/results/TYP-05-UnsafeAsCast.md) |
| [TYP-06](#️-typ-06-static-pre-assembly-of-per-type-artifacts) | Static pre-assembly of per-type artifacts | Fix per-type strings and SQL at initialization | ✅ | [Verified](benchmarks/results/TYP-06-StaticArtifact.md) |
| [TYP-07](#️-typ-07-choosing-the-hash-source-for-type-keys) | Hash source for Type keys | TypeHandle.Value over virtual GetHashCode | ⚠️ | [Verified](benchmarks/results/TYP-07-TypeHashSource.md) |
| [BIT-01](#-bit-01-lightweight-hashing-that-exploits-domain-constraints) | Lightweight hash exploiting domain constraints | O(1) hash for a known key set | ✅ | [Implemented](src/PerformancePatterns/Col/SampledNameTable.cs) |
| [BIT-02](#-bit-02-power-of-two-sizing-plus-masking-to-replace-modulo) | Power-of-two size + mask | Turn modulo (division) into a bitwise AND | ✅ | [Verified](benchmarks/results/BIT-02-PowerOfTwoMask.md) |
| [BIT-03](#-bit-03-bit-scanning-and-counting-with-bitoperations) | BitOperations | Hardware instructions for bit scanning and counting | ✅ | [Verified](benchmarks/results/BIT-03-BitOperations.md) |
| [BIT-04](#-bit-04-general-purpose-hashing-with-xxhash3) | XxHash3 | Faster non-cryptographic hashing | ✅ | [Verified](benchmarks/results/BIT-04-XxHash3.md) |
| [BIT-05](#-bit-05-order-preserving-digests-for-variable-length-key-search) | Order-preserving key digest | Decide binary-search probes without reading key bytes | ✅ | [Verified](benchmarks/results/BIT-05-OrderedDigestSearch.md) |
| [VEC-01](#-vec-01-explicit-simd-vectort--vector256) | Explicit SIMD | Bulk processing with Vector\<T\> / Vector256 | ✅ | [Verified](benchmarks/results/VEC-01-VectorSum.md) |
| [VEC-02](#-vec-02-fixed-width-intrinsics-byte-shuffle) | Fixed-width intrinsics | Lane permutation Vector\<T\> cannot express | ✅ | [Verified](benchmarks/results/VEC-02-VectorShuffle.md) |
| [SEQ-01](#-seq-01-spantokenizert) | SpanTokenizer\<T\> | General-purpose span splitting (zero allocation) | ✅ | [Implemented](src/PerformancePatterns/Seq/SpanTokenizer.cs) |
| [SEQ-02](#-seq-02-struct-io-over-stream) | Struct I/O over Stream | Direct binary read/write of structs | ✅ | [Verified](benchmarks/results/SEQ-02-StructStreamIo.md) |
| [SEQ-03](#-seq-03-lazy-sequence-processing-batch--segment--traverse) | Batch / Segment / Traverse | Low-allocation sequence processing | ✅ | [Implemented](src/PerformancePatterns/Seq/BatchExtensions.cs) |
| [SEQ-04](#-seq-04-ring-buffer-with-incremental-delimiter-search) | Ring buffer + incremental search | Splitting a streaming receive | ✅ | [Verified](benchmarks/results/SEQ-04-RingSplit.md) |
| [SEQ-05](#-seq-05-reading-an-unknown-length-stream-without-a-grow-copy-chain) | Unknown-length stream to ReadOnlySequence | Remove the grow-copy chain and the LOH traffic | ✅ | [Verified](benchmarks/results/SEQ-05-StreamSequence.md) |
| [COL-01](#️-col-01-direct-internal-access-with-collectionsmarshal) | CollectionsMarshal | Direct access to List/Dictionary internals | ✅ | [Verified](benchmarks/results/COL-01-CollectionsMarshal.md) |
| [COL-02](#️-col-02-conditional-adoption-of-frozendictionary) | Conditional use of FrozenDictionary | Faster lookup for immutable dictionaries | ✅ | [Verified](benchmarks/results/COL-02-FrozenCondition.md) |
| [COL-03](#️-col-03-span-key-lookups-with-getalternatelookup) | GetAlternateLookup | Dictionary lookup with a Span key | ✅ | [Verified](benchmarks/results/COL-04-SampledNameTable.md) |
| [COL-04](#️-col-04-choosing-a-lookup-strategy-for-small-element-counts) | Small-set lookup strategy | Choose the implementation by size and shape | ✅ | [Implemented](src/PerformancePatterns/Col/SampledNameTable.cs) |
| [COL-05](#️-col-05-concrete-type-dispatch-for-ienumerable-parameters) | IEnumerable concrete-type dispatch | Route List/array inputs onto a Span path | ✅ | [Verified](benchmarks/results/COL-05-EnumerableDispatch.md) |
| [COL-06](#️-col-06-shape-specialized-collection-conversion) | Shape-specialized collection conversion | Optimize the allocation and copy strategy for the destination | ✅ | [Verified](benchmarks/results/COL-06-CollectionConvert.md) |
| [COL-07](#️-col-07-existence-checked-ref-lookup-with-getvaluerefornullref) | GetValueRefOrNullRef | Fuse existence check and update into one probe | ✅ | [Verified](benchmarks/results/COL-07-ValueRefLookup.md) |
| [TXT-01](#-txt-01-formatting-and-conversion-with-lookup-tables) | Lookup-table formatting | Table-driven fixed-format formatting | ✅ | [Implemented](src/PerformancePatterns/Txt/Utf8DateTimeFormatter.cs) |
| [TXT-02](#-txt-02-stackalloc-first-string-building) | stackalloc-first string building | Low-allocation alternative to StringBuilder | ✅ | [Implemented](src/PerformancePatterns/Txt/ValueStringBuilder.cs) |
| [TXT-03](#-txt-03-avoiding-exceptions-with-the-try-pattern) | Try pattern | Do not use exceptions for control flow | ✅ | [Verified](benchmarks/results/TXT-03-TryPattern.md) |
| [TXT-04](#-txt-04-matching-byte-sequence-tokens-directly) | Direct byte-token matching | Match with u8/uint instead of materializing a string | ✅ | [Verified](benchmarks/results/TXT-04-TokenMatch.md) |
| [TXT-05](#-txt-05-direct-utf-8-formatting-with-utf8trywrite) | Utf8.TryWrite | Direct Span writes for UTF-8 interpolation | ✅ | [Verified](benchmarks/results/TXT-05-Utf8TryWrite.md) |
| [TXT-06](#-txt-06-ascii-specialized-comparison) | ASCII-specialized comparison | Case-insensitive handling via the Ascii class | ✅ | [Verified](benchmarks/results/TXT-06-Ascii.md) |
| [TXT-07](#-txt-07-stringcreate--tryformat--ispanformattable) | string.Create / TryFormat | Zero-allocation string creation | ✅ | [Verified](benchmarks/results/TXT-07-StringCreate.md) |
| [TXT-08](#-txt-08-searchvaluest) | SearchValues\<T\> | SIMD-optimized search over many candidates | ✅ | [Verified](benchmarks/results/TXT-08-SearchValues.md) |
| [TXT-09](#-txt-09-applied-idioms-for-fixed-length-formatting) | Advanced fixed-width formatting | TryFormat + Fill and vectorized trimming | ✅ | [Verified](benchmarks/results/TXT-09-FixedFieldFormat.md) |
| [TXT-10](#-txt-10-aggregating-string-matching-into-a-switch) | Switch aggregation for string matching | Compiler-generated length / character bucketing | ✅ | [Verified](benchmarks/results/TXT-10-StringSwitchDispatch.md) |
| [TXT-11](#-txt-11-type-fast-path-for-object--string-conversion) | Type fast path for object → string | Devirtualizing conversion that goes through an interface | ✅ | [Verified](benchmarks/results/TXT-11-ObjectToStringFastPath.md) |
| [ASY-01](#-asy-01-eliding-the-async-state-machine) | Eliding the async state machine | Return the Task directly for simple forwarding | ✅ | [Verified](benchmarks/results/ASY-01-AsyncElision.md) |
| [ASY-02](#-asy-02-producerconsumer-with-systemthreadingchannels) | System.Threading.Channels | Producer/consumer queue | ✅ | [Verified](benchmarks/results/ASY-02-Channels.md) |
| [ASY-03](#-asy-03-systemiopipelines) | System.IO.Pipelines | Pipelined I/O streaming | ✅ | [Verified](benchmarks/results/ASY-03-Pipelines.md) |
| [ASY-04](#-asy-04-knowing-the-cost-of-iasyncenumerable-and-when-to-use-it) | When to use IAsyncEnumerable | Per-element cost of await foreach | ✅ | [Verified](benchmarks/results/ASY-04-AsyncEnumerable.md) |
| [ASY-05](#-asy-05-valuetask--ivaluetasksource) | ValueTask / IValueTaskSource | Fewer allocations on async completion paths | ✅ | [Verified](benchmarks/results/ASY-05-ValueTask.md) |
| [ASY-06](#-asy-06-single-loop-scheduler) | Single-loop scheduler | Avoid a proliferation of timers | ✅ | [Verified](benchmarks/results/ASY-06-SchedulerPrimitive.md) |
| [ASY-07](#-asy-07-streaming-io) | Streaming I/O | Avoid buffering everything | ✅ | [Verified](benchmarks/results/ASY-07-StreamBuffering.md) |
| [CON-01](#-con-01-one-shot-guards-with-interlocked) | Interlocked one-shot guard | Lock-free run-once for Dispose and initialization | ✅ | [Verified](benchmarks/results/CON-01-DisposeGuard.md) |
| [CON-02](#-con-02-reference-counting-for-pinned-resources) | Reference counting for pinned resources | Batch retains per run; deterministic release | ✅ | [Verified](benchmarks/results/CON-02-RefCount.md) |
| [CON-03](#-con-03-false-sharing-and-cache-line-padding) | False sharing padding | Separate concurrent counters onto cache lines | ✅ | [Verified](benchmarks/results/CON-03-FalseSharing.md) |
| [SYS-01](#️-sys-01-low-cost-time-and-elapsed-time-reads) | Low-cost timestamps | Avoid DateTime.UtcNow | ✅ | [Verified](benchmarks/results/SYS-01-Timestamp.md) |
| [DAT-01](#️-dat-01-optimizing-column-resolution-in-db-access) | Optimized column resolution for DB access | Ordinal caching and single-pass column resolution | ✅ | [Verified](benchmarks/results/DAT-01-OrdinalResolve.md) |
| [GEN-01](#-gen-01-strategies-for-fast-emit-generated-code) | Speed strategies for Emit-generated code | Inlining of generated delegates and similar | ❌ | [Verified](benchmarks/results/GEN-01-EmitStrategy.md) |
| [GEN-02](#-gen-02-designing-source-generator-output) | Designing Source Generator output | Guide to what to generate for speed | ✅ | [Guide](docs/generated-code-patterns.md) |

## 💾 MEM: Memory access optimization

### 💾 MEM-01: SkipLocalsInit

**Goal:** Skip zero-initialization of locals (`.locals init`).

**Effect:**

- Removes the `memset` performed when the stack frame is set up
- Especially effective in methods that use `stackalloc` heavily
- Measured: a method containing a constant 512-byte stackalloc went from 6.6ns to 1.6ns (confirmed in the STK-06 verification)

**AOT:** ✅ No issues

**Example:**

```csharp
[SkipLocalsInit]
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public bool MoveNext()
{
    Span<byte> localBuffer = stackalloc byte[64]; // not zero-initialized
    // ...
}
```

**Use cases:** Extremely hot methods such as `MoveNext()`.

**Measured (net10 / x86-64-v4, a method call containing stackalloc byte[4096]):** 19.1 ns with zero-init drops to **1.6 ns with `[SkipLocalsInit]` (0.09x = roughly 11x)**, and code size drops from 604 B to 177 B (the memset path disappears). The cost is proportional to the stackalloc size (the allocated length, not the used length). → [Results](benchmarks/results/MEM-01-SkipLocalsInit.md)

**Caveats:**

- The project needs `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` (required just to use the attribute, even without writing unsafe code)
- Guarantee that nothing reads before writing, so uninitialized memory is never read. Consider combining it with `Unsafe.SkipInit(out value)`
- **A source generator emitting it into generated code needs a dedicated option plus an `AllowUnsafeBlocks` set by the package itself.** The required `AllowUnsafeBlocks` is a setting on the **consuming** project, so emitting it unconditionally breaks the build (CS0227) for every consumer that has not set it. But auto-detecting ("emit only if the consumer already enabled unsafe") means the attribute **never fires unless the consumer opts in explicitly** — in practice none of the target repositories' own test projects had it set, so it never fired once. The right shape is a `.props` / `.targets` shipped with the package that **defaults a dedicated option to `true` and sets `AllowUnsafeBlocks` under the same condition**:

```xml
<PropertyGroup>
  <MyGenerator_SkipLocalsInit Condition="'$(MyGenerator_SkipLocalsInit)' == ''">true</MyGenerator_SkipLocalsInit>
</PropertyGroup>
<PropertyGroup Condition="'$(MyGenerator_SkipLocalsInit)' == 'true'">
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
</PropertyGroup>
<ItemGroup>
  <CompilerVisibleProperty Include="MyGenerator_SkipLocalsInit" />
</ItemGroup>
```

  The generator only has to read `build_property.MyGenerator_SkipLocalsInit` from `AnalyzerConfigOptions`, keeping the pipeline `Compilation`-free. **Default the generator side to `false` when the value is absent**, so a project that never imported the `.props` / `.targets` does not hit CS0227. Consumers opt out with a single property
- **In generated code, also condition on whether that path actually stackallocs.** Branches that carry no scratch buffer gain nothing from the attribute

---

### 💾 MEM-02: struct element array + ref access (data-oriented layout)

**Goal:** Hold an entry sequence as an array of structs rather than an array of classes and operate on it via `ref`, removing per-element heap allocation and pointer chasing at the same time.

**Effect:**

- Measured: creating and walking class elements costs 63.6ns / 664B, while struct elements in a pooled array cost 9.8ns / 0B (16 elements). The cost stays nearly flat as the element count grows
- The walk itself does not get faster (within 3% of a freshly allocated class array). The win is **removing the per-element heap allocation** plus a contiguous layout that does not degrade as the heap ages

**AOT:** ✅ No issues

**Example:**

```csharp
private Entry[] entries; // Entry is a struct

for (var i = 0; i < entries.Length; i++)
{
    ref var entry = ref entries[i];   // operate in place, no copy
    entry.Value = Compute(entry.Key);
}
```

**Use cases:** Hash table entries, parser token sequences, column metadata — internal library data structures in general.

**Caveats:**

- Not receiving with `ref var` reintroduces a struct copy and can be counterproductive
- Adding ArrayPool alone while keeping class elements does not help (the per-element allocation remains). It only works together with the switch to structs
- Advanced form: a flat layout where a hash table stores the first element inline in a struct slot and puts only overflow elsewhere (at a stride within the same array, for example). It cuts pointer chasing further, but the gain is on the order of a few percent and shows up on collision access

**Implementation in this repo:** the `Entry[]` in [TypeMap.cs](src/PerformancePatterns/Typ/TypeMap.cs) (struct element array + copy-on-write)

**Measured (net10 / x86-64-v4, walking 1024 × 16-byte elements):** struct + ref access 412.9 ns ≒ struct copy access 414.7 ns ≒ class array 401.6 ns (all within ~3% — **a freshly, contiguously allocated class array has not yet lost locality, and a 16-byte copy is effectively free**). The structural difference this shape does not surface: at 1024 elements the structs occupy 16 KB contiguously, while the classes are roughly 40 KB of objects plus an 8 KB reference array, so **the class side degrades in locality as the heap ages** — that structural win, not the micro timing, is the reason to adopt. → [Results](benchmarks/results/MEM-02-StructArrayRef.md)

---

### 💾 MEM-03: Explicit slicing with Slice(offset, length)

**Goal:** Use `span.Slice(offset, length)` instead of the range operator `span[offset..]` to tighten the generated code for slice creation (**there is no time difference** — decide on code size and readability).

**Effect:**

- **The time difference cannot be resolved by measurement** (overlapping confidence intervals). What you get is one fewer instruction per iteration (15 vs 14 instructions, 103 vs 100 B), not speed
- The range operator computes and validates a "rest of the buffer" length, whereas an explicit length in `Slice` performs only the validation that is needed

**AOT:** ✅ No issues

**Example:**

```csharp
// Slow: range operator (computes the length to the end)
BinaryPrimitives.WriteInt32BigEndian(buffer[(i * 4)..], value);

// Fast: state the length being written
BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(i * 4, 4), value);
```

**Use cases:** Slicing inside serializer and encoder hot loops in general.

**Measured (net10 / x86-64-v4, 256 slices + endpoint reads):** `Slice(offset, 16)` 106.6 ns vs the range operator 107.0 ns (**CIs overlap — no time difference resolvable**). The generated code is not identical: the range operator carries an extra register shuffle per iteration (15 vs 14 instructions, 103 vs 100 B), but a wide out-of-order core absorbs it. Slice still produces the marginally tighter code, so preferring it in hot loops costs nothing — elsewhere choose readability. → [Results](benchmarks/results/MEM-03-SliceStyle.md)

**Caveats:** The readability difference is tiny, so `Slice(offset, length)` is a fine default on hot paths. For a one-off slice the difference is measurement noise.

---

### 💾 MEM-04: Passing struct arguments by in / ref

**Goal:** Avoid value copies when passing large structs as arguments.

**Effect:**

- By-value passing copies the whole struct on every call. It starts to matter once the struct no longer fits in registers (roughly above 16 bytes)
- `in` is a read-only by-reference pass. However, **putting `in` on a non-readonly struct causes a defensive copy on every member access and is counterproductive**

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Make large structs readonly struct and take them by in
public readonly struct RenderContext   // e.g. 40 bytes
{
    // ...
}

public void Draw(in RenderContext context) { ... }

// ✅ Use ref when the change has to flow back
public void Advance(ref Cursor cursor) => cursor.Position++;

// ❌ in on a non-readonly struct (defensive copy on every member access)
public void Draw(in MutableContext context) => context.Value.Use();
```

**Design guidance:**

- Small structs of 16 bytes or less are fine by value (the indirection cost of by-reference passing outweighs the copy)
- If you use `in`, make the type a `readonly struct`. For structs with fields, also apply the `readonly` member modifier
- The same goes for return values: consider `ref readonly` / `ref` returns for large structs

**Measured (net10 / x86-64-v4, non-inlined calls):**

| Struct size | By value | By in | Ratio |
|---:|---:|---:|---|
| 8 bytes | 1.23 ns | 1.24 ns | 1.01 |
| 16 bytes | 1.48 ns | 1.25 ns | **0.84** |
| 32 bytes | 1.31 ns | 1.24 ns | 0.94 |
| 64 bytes | 3.22 ns | 1.10 ns | **0.34** |
| 128 bytes | 2.49 ns※ | 1.19 ns | **0.48** |
| 256 bytes | 3.79 ns | 1.20 ns | **0.32** |
| in + readonly member | — | 1.19 ns | (baseline) |
| in + non-readonly member | — | 1.85 ns | **1.51 (❌ defensive copy)** |

※ the 128-byte by-value case is bimodal across launches (1.5-3.5 ns — the copy cost is alignment-sensitive)

**`in` stays flat (~1.2 ns) at every size, while by-value grows with size and fluctuates run to run** (the 64-byte case measured 1.24 ns in one run and 3.22 ns in another). From 64 bytes the win is decisive (0.32-0.48x = 2-3x), and 16 bytes already shows a small real win — predictability is itself part of the payoff. The defensive-copy trap remains: passing by `in` to a non-readonly member is 1.51x slower and roughly doubles code size (112 B → 219 B). → [Results](benchmarks/results/MEM-04-StructPass.md)

**Caveats:** The effect varies with size, call frequency, and whether the JIT inlines. Inlining can make the copy disappear entirely, so measure before and after.

---

### 💾 MEM-05: Struct layout optimization (size and field order)

**Goal:** Choose the struct's size deliberately — smaller to cut the footprint of an array walk, or a multiple of the widest vector move to cut the cost of passing it by value. **Those two pull in opposite directions**, and the choice sits **upstream** of MEM-02 (array of structs + ref access) and MEM-04 (pass by `in` above 16 bytes).

**Effect:**

- The same four fields come out as either 32 or 24 bytes depending on declaration order alone
- **What pays is crossing a cache-capacity boundary, not the byte count itself.** 0.67-0.71x on scattered access where only the smaller layout fits in L2; no measurable difference where both fit (0.99-1.00x on a machine with twice the L2)
- Sequential traversal shows no difference on either machine: the prefetcher absorbs the footprint gap
- **Shrinking does not make the by-value copy cheaper.** 24 bytes is not a multiple of the widest vector move, so the copy splits into 4 instructions where 32 bytes takes 2 — and which of the two wins reverses between microarchitectures

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ Alternating wide and narrow fields. C# emits Sequential for structs by default, so the padding stays (32 bytes)
internal struct Padded
{
    public long Id;      // offset 0
    public int Kind;     // offset 8  (+4 padding)
    public long Value;   // offset 16
    public int Flags;    // offset 24 (+4 padding)
}

// ✅ Order the fields wide-first yourself (24 bytes)
internal struct Packed
{
    public long Id;      // offset 0
    public long Value;   // offset 8
    public int Kind;     // offset 16
    public int Flags;    // offset 20
}

// ✅ Let the runtime reorder (24 bytes; only for internal types that are not tied to an external format)
[StructLayout(LayoutKind.Auto)]
internal struct Auto
{
    public long Id;
    public int Kind;
    public long Value;
    public int Flags;
}
```

**Use cases:** Hash table entries, parser token arrays, column metadata, arrays of value-type components — any struct a library lays out in bulk.

**Measured (net10 / x86-64-v4, 16,384 elements = 512 KB padded against 384 KB packed):** sizes confirmed with `Unsafe.SizeOf` — **Padded 32 B / Packed 24 B / Auto 24 B**.

| Traversal | Padded (32 B) | Packed (24 B) | Auto (24 B) |
|---|---:|---:|---:|
| Sequential | 13,185 ns (1.00) | 13,180 ns (1.00) | 13,116 ns (0.995) |
| Scattered | 14,061 ns (1.00) | 13,875 ns (0.99) | 14,031 ns (1.00) |
| **Scattered, x86-64-v3** | **26,213 ns (1.00)** | **18,516 ns (0.71)** | **17,662 ns (0.67)** |

**Both machines run byte-identical code (65 B sequential / 93 B scattered); what differs is the L2.** 512 KB against 384 KB straddles the 512 KB per-core L2 of the x86-64-v3 machine and fits entirely inside the 1 MB L2 of the x86-64-v4 one — so the win is decisive on the first and absent on the second (0.99-1.00x, CIs overlapping in two runs). By-value passing reverses as well: **1.292 vs 1.535 ns here in favour of the 32 B form**, against 2.034 vs 1.854 ns in favour of 24 B on x86-64-v3. → [Measurement](benchmarks/results/MEM-05-StructLayout.md)

**Caveats:**

- **The generated code is identical for all three types** (65 B sequential / 93 B scattered). The difference is on the **data** side, not the code side, so this book's "overlapping CIs plus identical code means no difference" rule **does not apply here**. Sequential showing no difference is the prefetcher at work, not evidence that layout is irrelevant
- **Never use `LayoutKind.Auto` on a type that is tied to an external format.** Binary I/O and interop layouts stay fixed with `Sequential` + `Pack`, as in SEQ-02
- Structs containing reference fields get their layout decided by the runtime regardless, so this technique mainly targets unmanaged field sets

---

## 🥞 STK: Stack usage and zero-allocation type design

### 🥞 STK-01: ref struct (stack-only type)

**Goal:** Forbid boxing and escape to the heap at the type-system level.

**Effect:**

- Zero GC pressure (nothing lands on the heap)
- Ideal for transient sequential-access types such as iterators and readers
- Combined with `foreach` duck typing (`GetEnumerator()`), iteration costs about the same as over an array

**AOT:** ✅ No issues

**Example:**

```csharp
public ref struct SpanTokenizer<T> where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> span;
    // ...
    public readonly SpanTokenizer<T> GetEnumerator() => this;
    public bool MoveNext() { ... }
    public ReadOnlySpan<T> Current { ... }
}
```

**Use cases:** Parsers, deserializers, tokenizing text.

**Implementation in this repo (ref struct examples):** [ValueStringBuilder.cs](src/PerformancePatterns/Txt/ValueStringBuilder.cs) / [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs) / [TemporaryBuffer.cs](src/PerformancePatterns/Buf/TemporaryBuffer.cs) / [SpanTokenizer.cs](src/PerformancePatterns/Seq/SpanTokenizer.cs) / [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs) (the SpanBatch enumerator)

**Caveats:**

- Constraints apply: it cannot be held as a field of a class, cannot cross `await` / `yield`, and so on (partially relaxed since C# 13)
- Since C# 13, ref structs can implement interfaces and the `allows ref struct` constraint is available
- **`scoped` (C# 11) is the modifier that relaxes the escape constraint a ref struct imposes on callers.** On a parameter it becomes a contract that the reference will not leave the method. **It has no effect on generated code whatsoever** — caller and callee disassembly were confirmed identical instruction for instruction — so it is neither something to add for speed nor something to avoid for speed → [Measurement](benchmarks/results/LAB-ScopedRef.md)
- **`[UnscopedRef]` (C# 11) is what lets a struct member return `ref this.field`.** Without it the member does not compile at all (CS8170). It is **not a performance pattern** though: a ref-returning accessor measures 1.07x against a get/set pair with code growing 85 → 88 B, so no axis improves ([R-20](docs/rejected-patterns.md))

---

### 🥞 STK-02: Zero-copy access with Span\<T\> / ReadOnlySpan\<T\>

**Goal:** Provide a typed view over the original buffer without copying the data.

**Effect:**

- Handles sources as varied as `string`, `byte[]`, `Memory<T>`, and stack variables without copying
- Creates array slices and string slices in O(1)

**AOT:** ✅ No issues

**Example:**

```csharp
// Tokenize a string without copying it
foreach (var token in new SpanTokenizer<char>(input.AsSpan(), ','))
{
    ProcessToken(token); // ReadOnlySpan<char> — zero allocation
}
```

**Use cases:** CSV/DSV parsers, protocol deserializers, text transformation pipelines.

**Design guidance:** Public library APIs should offer `ReadOnlySpan<T>` overloads alongside `string` / `T[]`, and internal processing should be uniformly Span-based.

**Implementation in this repo:** the foundation of every implementation here (representative: [SpanTokenizer.cs](src/PerformancePatterns/Seq/SpanTokenizer.cs) (0.30-0.34x) (zero-cost abstraction) / [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs) (Span key matching)). See each pattern's measurement results for individual numbers

---

### 🥞 STK-03: struct iterator pattern

**Goal:** Use a duck-typed struct iterator instead of `foreach` over `IEnumerable<T>`.

**Effect:**

- Removes virtual calls through the `IEnumerator<T>` interface
- Eliminates heap allocation of the iterator object
- Combined with `ref struct`, it runs entirely on the stack
- Measured: struct enumerable + struct enumerator (duck typing) is 1.4ns / 0B. Making the enumerable a class gives 2.9ns / 24B, and a `yield return` implementation 14.4ns / 56B (about 10x) — making the enumerable itself a struct matters, not just the enumerator

**AOT:** ✅ No issues

**Example:**

```csharp
// Call site (foreach triggers duck typing)
foreach (var line in text.SplitLines())
{
    // SplitLinesEnumerator is a ref struct with a GetEnumerator()
}
```

**Use cases:** Span and text processing, iteration inside game loops.

**Implementation in this repo:** [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs) (SEQ-03 chunking implemented with a struct enumerator) / [Tests](tests/PerformancePatterns.Tests/Seq/BatchTest.cs) / [Results](benchmarks/results/SEQ-03-Batch.md)

**Measured (net10 / x86-64-v4, from the SEQ-03 measurements):** foreach over a struct enumerator runs at 0.63-0.74x of `Enumerable.Chunk` (which goes through IEnumerator), with zero allocation and 1/12 to 1/16 the code size.

**Caveats:** Exposing a struct enumerator as `IEnumerable<T>` boxes it and erases the benefit. Expose a `GetEnumerator()` that returns the struct directly, and if `IEnumerable<T>` support is required, separate it out with an explicit implementation.

---

### 🥞 STK-04: Optimizing iterators with static local methods

**Goal:** In an iterator method containing `yield return`, achieve **eager argument validation** and **closure allocation prevention** at the same time.

**Effect:**

- Argument checks run before `foreach` starts (before enumeration begins)
- The `static` modifier makes the compiler forbid capturing variables from the enclosing scope, preventing needless closure object allocation
- The method signature (validation layer) and the implementation (iteration layer) are cleanly separated

**AOT:** ✅ No issues (compiler-generated state machines are AOT-compatible)

**Why it is needed:** A method containing `yield return` is rewritten by the compiler into a state machine class, so the body does not run at call time (deferred execution). Validation written inside the iterator does not throw until `foreach` begins.

**Example:**

```csharp
// ❌ Deferred validation: a null source goes unnoticed until foreach
public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int size)
{
    if (source is null) throw new ArgumentNullException(nameof(source)); // ← never runs
    foreach (var item in source)
    {
        yield return ...;
    }
}
```

```csharp
// ✅ Eager validation + static local method pattern
public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int size)
{
    ArgumentNullException.ThrowIfNull(source);          // ← runs immediately at call time
    ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);

    return BatchIterator(source, size);                 // ← just returns the iterator

    static IEnumerable<IReadOnlyList<T>> BatchIterator( // ← static: capture forbidden
        IEnumerable<T> source, int size)
    {
        List<T>? bucket = null;
        foreach (var item in source)
        {
            // ...
            yield return bucket;
        }
    }
}
```

**What static buys you (at the compiler level):**

| | Without static | With static |
|---|---|---|
| Capturing outer variables | Possible (risk of unintended capture) | Compile error |
| Generated class | Allocates when a closure is present | No closure, less allocation |
| Intent of the code | Unclear | States explicitly that it does not depend on outer state |

**Use cases:** Every extension method that returns `IEnumerable<T>`, and LINQ-style methods containing `yield return`.

**Implementation in this repo:** `ThrowIfInvalidSize` in [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs) (a static local throw helper — an example of separating out eager validation)

**Measured (net10 / x86-64-v4, converted to a delegate and passed):** a capturing local function costs 7.00 ns + 88 B per call; a static local function with a state argument costs 15.26 ns / **0 B**. The allocation-elimination claim holds, but **on a hot path where you pass a delegate, a static lambda + TState (DSP-04: 2.96 ns / 0 B) is faster**. Static local functions earn their keep in direct calls (which get inlined) and in iterator/validation separation; for cached-delegate scenarios use the DSP-04 shape. → [Results](benchmarks/results/STK-04-LocalFunctionClosure.md)

---

### 🥞 STK-05: Boxing avoidance and hot-value caching

**Goal:** Avoid — or pin down — the boxing allocation incurred when crossing an object boundary.

**Effect (measured):**

- Boxing onto the heap costs 1.73ns + 24B. However, if the box stays inside the method (does not escape), escape analysis stack-allocates it and the cost is nearly zero (0.004ns)
- A pre-boxed cache of hot values removes the runtime allocation
- For enums, the non-generic `(T)Enum.Parse(typeof(T), name)` is 1.3-2.1x slower due to boxing and always allocates — use the generic `Enum.Parse<T>` / `Enum.TryParse<T>`

**AOT:** ✅ No issues

**Example:**

```csharp
// Pre-box hot values (0 / 1 / -1 / true / false, etc.)
private static readonly object BoxedZero = 0;
private static readonly object BoxedOne = 1;

public static object Box(int value) => value switch
{
    0 => BoxedZero,
    1 => BoxedOne,
    _ => value,
};
```

**Use cases:** Boundaries with object-based legacy APIs (ADO.NET, older serializers), logger state arguments.

**Caveats:** If generic constraints (`where T : struct` plus an interface constraint) let you design the whole call chain without boxing, that is the real fix (see JIT-02).

**When the box already exists (`Unsafe.Unbox<T>`):** this pattern is about not boxing in the first place, but the situation is different when an **already boxed value is handed to you** — an `object` field, a `Dictionary<K, object>`, an interop boundary. Unbox / mutate the copy / rebox pays one allocation per update, so take a ref into the existing box with `Unsafe.Unbox<T>` instead.

```csharp
// ❌ A new box per update (8,192 B for 256 updates)
var value = (Counter)boxes[i];
value.Count++;
boxes[i] = value;

// ✅ Take a ref into the existing box: zero allocation, and the box identity is preserved
ref var value = ref Unsafe.Unbox<Counter>(boxes[i]);
value.Count++;
```

**Measured (net10 / x86-64-v4, updating 256 boxed structs):** the rebox form is 1,024.9 ns / 582 B / **8,192 B allocated**, against **171.2 ns (0.17x) / 251 B / 0 B** for `Unsafe.Unbox`. All three axes — time, code size, allocation — improve, and **the ratio barely moves between machines (0.18x on x86-64-v3) because what disappears is an allocation, not instructions**. Decide the aliasing question first, though: in-place update preserves box identity, so every other reference to that box sees the change. → [Measurement](benchmarks/results/LAB-UnboxInPlace.md)

**Caveat for `Unsafe.Unbox`:** a type mismatch raises `InvalidCastException` (unlike `Unsafe.As`, the type is checked), but passing a reference that is not a box is undefined. Restrict it to **boxes you created yourself**.

**Main sources of implicit boxing (review checklist):**

- Assigning a struct to an interface-typed variable or argument (`IComparer<T> c = myStructComparer`)
- Passing a value type to an `object` parameter (`string.Format` / `string.Concat` / legacy loggers / non-generic APIs such as `ArrayList`)
- Binding a delegate to a struct method
- The non-generic `Enum.Parse` on enums, and comparisons that go through the default `GetHashCode` / `Equals(object)` implementations of value types
- Expanding value types into `params object[]` (avoidable with C# 13 `params ReadOnlySpan<T>`)

**Measured (net10 / x86-64-v4, storing -1/0/1 into an object[]):** direct boxing costs 3.68 ns + 24 B per call, while the pre-cached switch is **2.54 ns / 0 B (0.69x)** — the cache wins on both time and allocation here. The branch-vs-allocation balance is CPU-dependent (a pointer-bump alloc can outpace the switch on some cores), but the GC-pressure removal holds regardless, so adopt it on long-lived, high-frequency paths. Non-escaping boxes are already stack-allocated by the JIT, so the target is strictly "known values that escape". → [Results](benchmarks/results/STK-05-BoxingCache.md)

---

### 🥞 STK-06: Constant-size stackalloc

**Goal:** Allocate stackalloc with a compile-time constant size (it becomes a fixed region in the frame) and slice off what you need. A variable-size allocation compiles to the costly `localloc` instruction.

**Effect (measured, net10 / x86-64-v4):**

| Allocation form | With SkipLocalsInit | With zero-init |
|---|---|---|
| Constant 512 (+ slice) | **0.27 ns** | 1.4 ns |
| Variable size 512 | 1.8 ns (about 6x) | **6.1 ns (about 4x)** |

- With a constant size the zero-init cost is fixed and predictable too. A variable size pays the localloc cost itself plus a slower form of zero-init — constant + SkipLocalsInit is the only combination that stays sub-nanosecond
- Also demonstrates the removal effect of MEM-01 (SkipLocalsInit): 1.4 → 0.27ns

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Allocate a constant and slice what you need (the BUF-05 threshold idiom is this shape)
Span<byte> buffer = stackalloc byte[512];
var span = buffer[..size];

// ❌ Variable-size stackalloc (compiles to localloc)
Span<byte> buffer = stackalloc byte[size];
```

**Use cases:** Every initial buffer allocation in BUF-03 / BUF-05 / TXT-02.

**Caveats:** Aim for a constant of roughly 256-512 bytes, and avoid allocating inside recursion or loops (stack usage is per call).

---

### 🥞 STK-07: Lazy allocation and shared singletons

**Goal:** Do not allocate things that "usually go unused" until they are used, and return a shared instance for "values with no content". Push the fixed cost of object creation onto only the paths that actually need it.

**Effect:**

- Error lists used only on failure, Disposables not needed until subscription, validation dictionaries not needed until an error occurs — all of these disappear entirely on the success path and in bulk-creation scenarios
- Sharing empty arrays, default delegates, empty EventArgs, and the like pins down the allocation on frequently taken paths

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Do not create the list until a failure happens
List<Error>? errors = null;
foreach (var item in items)
{
    if (!Validate(item, out var error))
    {
        (errors ??= []).Add(error);
    }
}

// ✅ Return a shared instance for empty (the caller's null check disappears too)
public IReadOnlyList<Error> Errors => errors ?? (IReadOnlyList<Error>)Array.Empty<Error>();

// ✅ Static sharing of default delegates and event args
private static readonly Func<bool> AlwaysTrue = static () => true;
private static readonly PropertyChangedEventArgs CountChangedEventArgs = new(nameof(Count));
```

**Use cases:** Error collection in Result/validation code, ViewModel-associated objects (Disposables and similar), notification event args (`PropertyChangedEventArgs` cached statically per property name), Null Object (a singleton empty implementation).

**Measured (net10 / x86-64-v4):**

- Error list (10% failure rate): lazy allocation 46.1 ns vs eager allocation 37.3 ns (1.24x — the null check plus branchy first-add costs a little when failures do occur), and both allocate 216 B as long as a failure occurs. **On the all-success path the lazy side is 0.57x and allocates nothing at all** (the eager side always allocates 216 B) — the win is structural in allocation, sized by how rare the failure path is
- Empty arrays: **on net10 even `new int[0]` allocates nothing in practice** (the runtime shares the empty array), and both `[]` and `new int[0]` now compile to the same 12 B shared-reference load — no difference in time, allocation, or code. Defaulting to `[]` / `Array.Empty<T>()` remains fine as a style/portability choice → [Results](benchmarks/results/STK-07-LazyAllocation.md)

**Caveats:** If a lazily allocated field needs thread safety, guard it with a lock or combine it with CON-01 (a single-threaded type can leave it as is).

---

### 🥞 STK-08: Fixed-length buffers inside structs with InlineArray

**Goal:** Embed a fixed-length element sequence inside a struct with no array object (.NET 8+).

**Effect:**

- Expresses in safe code — including reference types — what previously required a `fixed` buffer (unsafe only, unmanaged types only)
- Elements are embedded in the struct itself, so everything stays inside a stack local or a pooled entry with no separate heap allocation
- Usable as a Span via `MemoryMarshal.CreateSpan` or indexer access

**AOT:** ✅ No issues

**Example:**

```csharp
[InlineArray(8)]
public struct Slot8<T>
{
    private T element0;   // declare exactly one field
}

// Usage: indexing, foreach, and Span conversion all work
var slots = new Slot8<int>();
slots[0] = 1;
Span<int> span = slots;
```

**Use cases:** Small fixed-length work areas, inline storage of hash table entries (an extension of MEM-02), history buffers in state machines.

**Measured (net10 / x86-64-v4, writing and summing int×8):** against `new int[8]` at 4.81 ns / 56 B, stackalloc is 2.87 ns (0.60) and InlineArray 2.92 ns (0.61) — both with zero allocation and equal in time (CIs overlap); InlineArray's code is slightly smaller (112 vs 134 B). The value of InlineArray is that it can be held as a struct field. → [Results](benchmarks/results/STK-08-InlineArray.md)

**Caveats:** The element count is a compile-time constant. It cannot be used for variable lengths, so design the overflow path to switch to the BUF-05 tiered strategy.

---

### 🥞 STK-09: params ReadOnlySpan\<T\>

**Goal:** Remove the array allocation incurred on every variadic (`params`) call (C# 13 / .NET 9).

**Effect:**

- `params T[]` allocates a heap array on every call. `params ReadOnlySpan<T>` makes the compiler use a temporary region on the stack, so it is **allocation-free**
- Call sites need no change (existing calls such as `Log("a", "b")` simply get faster)
- The BCL takes the same approach, adding overloads to `string.Concat` and `string.Format`

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ Allocates an array on every call
public static void Trace(params object[] values) { ... }

// ✅ No array allocation (C# 13). With value types, watch out for STK-05 boxing
public static void Trace(params ReadOnlySpan<string> values)
{
    foreach (var value in values)
    {
        Write(value);
    }
}
```

**Use cases:** Logging and diagnostics APIs, variadic key joining, utilities that take several values.

**Measured (net10 / x86-64-v4, 3 arguments):** `params T[]` 4.46 ns / 48 B → `params ReadOnlySpan<T>` **1.10 ns / 0 B (0.25x)**. The allocation disappears with no change to the call syntax. → [Results](benchmarks/results/STK-09-ParamsSpan.md)

**Caveats:** When replacing `params T[]` in a public library API, consider keeping both overloads for compatibility with existing calls that pass an array explicitly.

---

### 🥞 STK-10: ref field cursor for structured reads

**Goal:** In a sequential parse that reads fields of differing widths, hold the cursor position as **the ref itself** (C# 11 ref fields) rather than as an index.

**Effect:**

- **0.81x** against index arithmetic written out at the call site, and the code is smaller too: 163 → **111 B** (0.75x on x86-64-v3 with byte-identical code — the ratio shrinks on wider cores, the ranking does not)
- Even without reaching for ref fields, the re-slicing form gets to **0.86x** at 128 B

**AOT:** ✅ No issues

**Scope (the line against R-12 is the point of this pattern):**

| Shape | Verdict | Why |
|---|:---:|---|
| Whole-element iteration (read N values of one type) | ❌ [R-12](docs/rejected-patterns.md) | Indexed form gets bounds-check elimination and auto-vectorization. The ref form is 1.21x slower |
| **Field-granular structured reads** (each step reads a different width) | ✅ This pattern | The loop is not a counted loop, so the indexed form has no advantage to give |

**Example:**

```csharp
// Record layout: [byte tag][ushort length][length bytes payload]
internal ref struct FieldRefReader
{
    private readonly ref byte end;

    private ref byte current;

    public FieldRefReader(ReadOnlySpan<byte> source)
    {
        current = ref MemoryMarshal.GetReference(source);
        end = ref Unsafe.Add(ref current, source.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadHeader(out byte tag, out int length)
    {
        if (!Unsafe.IsAddressLessThan(ref current, ref end))
        {
            tag = 0;
            length = 0;
            return false;
        }

        tag = current;
        length = Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref current, 1));
        current = ref Unsafe.Add(ref current, 3);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadPayload(int length)
    {
        var payload = MemoryMarshal.CreateReadOnlySpan(ref current, length);
        current = ref Unsafe.Add(ref current, length);
        return payload;
    }
}
```

**Use cases:** Frame parsing for custom protocols, variable-length binary records, TLV decoders.

**Measured (net10 / x86-64-v4, parsing 512 records):**

| Form | Time | Ratio | Code size |
|---|---:|---|---:|
| Index arithmetic written at the call site (baseline) | 789.4 ns | 1.00 | 163 B |
| Cursor holding span + position | 776.4 ns | 0.98 | 153 B |
| **Cursor that re-slices the remainder** | **676.2 ns** | **0.86** | **128 B** |
| **ref field cursor** | **641.6 ns** | **0.81** | **111 B** |

Confidence intervals do not overlap (629-654 vs 786-793 ns). **As a staged move, switching to the re-slicing cursor alone already buys 0.86x.** The code sizes are byte-identical on x86-64-v3, where the same comparison measured **0.75x** — what this removes is per-field address arithmetic, and a wider core hides more of it, so expect the ratio to shrink on newer hardware while the ranking holds. → [Measurement](benchmarks/results/STK-10-RefFieldStructRead.md)

**Caveats:**

- **Do not use this for whole-element iteration.** Per R-12 it loses to the indexed form
- ref fields can only live in a `ref struct`, and cannot cross `await` / `yield`
- `Unsafe.ReadUnaligned` reads in machine byte order. Use `BinaryPrimitives` when the external format fixes the endianness (same caveat as SEQ-02)
- End detection uses `Unsafe.IsAddressLessThan`. Getting the end ref wrong makes the check always true, the JIT removes the check entirely, and the result is an "impossibly fast" wrong number. Pin the correctness down with Verify (measurement pitfall 3)

---

## 🧺 BUF: Buffer management and pooling

### 🧺 BUF-01: Buffer reuse with ArrayPool\<T\>

**Goal:** Reduce GC pressure from frequently discarded buffers.

**Effect:**

- Keeps large short-lived arrays (serialization buffers, for example) off the LOH
- `Rent` / `Return` are very cheap (essentially lock-free)

**AOT:** ✅ No issues

**Example:**

```csharp
var buffer = ArrayPool<byte>.Shared.Rent(size);
try
{
    // work using buffer
}
finally
{
    ArrayPool<byte>.Shared.Return(buffer);
}
```

**Use cases:** Network I/O buffers, serialization, temporary data conversion.

**Caveats:**

- The rented array is at least the requested size (usually a power of two). Length-dependent work must slice to the requested size
- When handling sensitive data, clear it with `Return(buffer, clearArray: true)`
- Failing to return is not fatal since the GC reclaims it, but pool efficiency drops. See BUF-04 for scope management

**Implementation in this repo (types backed by ArrayPool):** [TemporaryBuffer.cs](src/PerformancePatterns/Buf/TemporaryBuffer.cs) (BUF-05) / [MemoryOwner.cs](src/PerformancePatterns/Buf/MemoryOwner.cs) (BUF-04) / [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs) (BUF-03) / [PooledBufferWriter.cs](src/PerformancePatterns/Buf/PooledBufferWriter.cs) (BUF-02)

**Measured:** a bare Rent/Return over a 4 KB lifecycle allocates nothing (`new byte[]` allocates 4,120 B). Time is dominated by the fill, so the difference against the wrappers is measurement noise → [BUF-04-MemoryOwner.md](benchmarks/results/BUF-04-MemoryOwner.md)

---

### 🧺 BUF-02: IBufferWriter\<T\> + GetSpan / Advance pattern

**Goal:** Abstract the write destination and write directly into the output buffer with zero copies.

**Effect:**

- No intermediate array. `GetSpan` hands you a slice of the destination and `Advance` moves the cursor
- Works over many backends: `PooledBufferWriter`, `PipeWriter`, `ArrayBufferWriter`, and others

**AOT:** ✅ No issues

**Example:**

```csharp
// Write a typed value straight into the buffer writer
public static void Write<T>(this IBufferWriter<byte> writer, T value)
    where T : unmanaged
{
    var span = writer.GetSpan(Unsafe.SizeOf<T>());
    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
    writer.Advance(Unsafe.SizeOf<T>());
}
```

**Use cases:** Protocol encoders, binary serializers.

**Design guidance:** The output API of a serializer library should take an `IBufferWriter<byte>` rather than return a `byte[]`.

**Implementation in this repo:** [PooledBufferWriter.cs](src/PerformancePatterns/Buf/PooledBufferWriter.cs) (ArrayPool-backed + JIT-04 Grow separation + JIT-05 per-type clearing) / [Tests](tests/PerformancePatterns.Tests/Buf/PooledBufferWriterTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Buf/BufferWriterBenchmark.cs) / [Results](benchmarks/results/BUF-02-BufferWriter.md)

**Measured (net10 / x86-64-v4, writing 64 chunks of 16B):** against `MemoryStream` + ToArray, `ArrayBufferWriter` is 0.68x and `PooledBufferWriter` **0.57x — the fastest of the three** while also cutting **allocation from 2,976B to 32B (the writer object only)**, zeroing GC pressure across repeated writes.

---

### 🧺 BUF-03: BufferWriterSlim\<T\> (stack-first writing)

**Goal:** Small payloads on the stack, large payloads from the pool — a zero-allocation-first design.

**Effect:**

- Completely allocation-free while the initial (stackalloc) buffer suffices
- Rents from `ArrayPool` only on overflow, copying the initial buffer's contents across

**AOT:** ✅ No issues

**Example:**

```csharp
Span<byte> stack = stackalloc byte[256];
var writer = new BufferWriterSlim<byte>(stack);
writer.Write(someHeader);
// Small data stays entirely within stack
writer.Dispose(); // returns the rental if the pool was used
```

**Use cases:** Assembling log messages, building small binary packets.

**Implementation in this repo:** [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs) / [Tests](tests/PerformancePatterns.Tests/Buf/BufferWriterSlimTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Buf/BufferWriterSlimBenchmark.cs) / [Results](benchmarks/results/BUF-03-BufferWriterSlim.md)

**Measured (net10 / x86-64-v4, write lifecycle of N × 16 bytes):**

| Approach | 64 B (fits the stack) | 4096 B (growth path) |
|---|---|---|
| `ArrayBufferWriter` (baseline) | 25.2 ns / 312 B | 1,427 ns / **8,056 B** |
| PooledBufferWriter (BUF-02) | 24.3 ns / 32 B | 1,328 ns / 32 B |
| **BufferWriterSlim** | **19.0 ns** / **0 B** | **1,283 ns** / **0 B** |

Slim wins on both axes: 0.76x at 64 B and 0.90x on the growth path (non-overlapping CIs), with allocation at 312 B → 0 and 8,056 B → 0. Use Slim within a synchronous scope; choose BUF-02 when you need to pass it as `IBufferWriter<T>` or hold it in a field.

**Caveats:** Aim for a stackalloc size of roughly 256-512 bytes, and avoid allocating inside recursion or loops (to guard against stack overflow).

---

### 🧺 BUF-04: MemoryOwner\<T\> (scoped buffer ownership)

**Goal:** Give an `ArrayPool` rental an RAII (using) scope.

**Effect:**

- The type system prevents a missing `Dispose` (`using` can be enforced)
- Hides the gap between the requested length and the actual rental length (always a power of two), exposing an exact `Span` / `Memory`

**AOT:** ✅ No issues

**Example:**

```csharp
using var owner = MemoryOwner<byte>.Allocate(requestedSize);
await socket.ReceiveAsync(owner.Memory, cancel);
ParsePacket(owner.Span);
// } ← Dispose returns it automatically
```

**Use cases:** Async I/O buffers, file reads, protocol receive buffers.

**Implementation in this repo:** [MemoryOwner.cs](src/PerformancePatterns/Buf/MemoryOwner.cs) (conforms to `IMemoryOwner<T>`; double Dispose is handled by the CON-01 Interlocked guard) / [Tests](tests/PerformancePatterns.Tests/Buf/MemoryOwnerTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Buf/MemoryOwnerBenchmark.cs) / [Results](benchmarks/results/BUF-04-MemoryOwner.md)

**Measured (net10 / x86-64-v4, rent → write → aggregate → release of 4 KB):** time is dominated by fill+sum and lands at 1.63-1.65 μs, and **MemoryOwner cannot be separated from a bare Rent/Return because the ranges overlap (➖ measurement noise = the wrapper cost is below the measurement resolution)**. Allocation is 4,120 B for `new byte[]`, 0 B for bare ArrayPool, **32 B for MemoryOwner (the owner object only)**, and 0 B for TemporaryBuffer. The value is on the design side: enforced using, exact lengths, and double-Dispose safety. Within a synchronous scope use BUF-05 (TemporaryBuffer); across an async boundary use this type.

**Notes:** Conforming to the `IMemoryOwner<T>` interface allows interop with the `MemoryPool<T>` family of APIs. Since it cannot be a ref struct when crossing async methods, implement it as a class or struct.

---

### 🧺 BUF-05: Tiered temporary buffer strategy (stackalloc / ArrayPool unified)

**Goal:** Unify temporary buffer allocation behind a threshold switch — stackalloc when small, ArrayPool when large — with a ref struct managing the scope.

**Effect:** The overwhelming majority of calls (small sizes) become fully allocation-free, and large sizes carry zero GC pressure. This is the general form of BUF-03 (write-specialized) and TXT-02 (string-specialized).

**AOT:** ✅ No issues

**Example:**

```csharp
public ref struct TemporaryBuffer<T>
{
    private T[]? pooled;

    public TemporaryBuffer(Span<T> initial, int length)
    {
        Span = initial[..length];
    }

    public TemporaryBuffer(int length)
    {
        pooled = ArrayPool<T>.Shared.Rent(length);
        Span = pooled.AsSpan(0, length);
    }

    public Span<T> Span { get; }

    public void Dispose()
    {
        var toReturn = pooled;
        if (toReturn is not null)
        {
            pooled = null;
            ArrayPool<T>.Shared.Return(toReturn);
        }
    }
}

// Call site: the threshold switches between stackalloc and the pool (Span is exactly the requested length on either path)
using var buffer = size <= 512
    ? new TemporaryBuffer<char>(stackalloc char[512], size)
    : new TemporaryBuffer<char>(size);
Process(buffer.Span);
```

**Use cases:** Encoding conversion, P/Invoke buffers, temporary work areas in general.

**Implementation in this repo:** [TemporaryBuffer.cs](src/PerformancePatterns/Buf/TemporaryBuffer.cs) / [Tests](tests/PerformancePatterns.Tests/Buf/TemporaryBufferTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Buf/TemporaryBufferBenchmark.cs) / [Results](benchmarks/results/BUF-05-TemporaryBuffer.md)

**Measured (net10 / x86-64-v4):** at 4096 elements it runs at 0.09x of `new T[]` (about 11x faster, from removing the zero-init cost) with 0B. The 64-element stackalloc path is slightly slower than `new` (2.3ns vs 2.0ns) but goes from 88B to 0B — **the value at small sizes is not speed but zeroing GC pressure**. Compared with using `ArrayPool` directly it wins at small sizes (the stackalloc path avoids the pool access: 2.3 vs 4.2 ns).

**Caveats:**

- A variant reuses a `[ThreadStatic]` static buffer (fully allocation-free), but watch for reentrancy, retention across async boundaries, and per-thread memory sitting around. Accessing a ThreadStatic field itself has a cost, so hoist it into a local before the loop
- Aim for a stackalloc threshold of roughly 256-512 elements (same as BUF-03)

---

### 🧺 BUF-06: Skipping zero-init with GC.AllocateUninitializedArray

**Goal:** Skip zero-initialization when allocating a heap array (the heap counterpart of SkipLocalsInit). For temporary buffers you are certain to overwrite in full.

**Effect (measured, net10 / x86-64-v4, vs `new byte[N]`):**

| Size | Ratio | Verdict |
|---|---|---|
| 256B / 2048B | 0.98 / 0.94 | At small sizes the zero-init being skipped is too small to matter |
| 4096B | 0.60 | Effective |
| 64KB | **0.18 (about 5x)** | The sweet spot |
| 1MB (LOH class) | 0.98 | Per-allocation GC cost dominates and the difference vanishes |

**AOT:** ✅ No issues

**Example:**

```csharp
// Receive buffers and the like that are certain to be fully written immediately after
var buffer = GC.AllocateUninitializedArray<byte>(length);
stream.ReadExactly(buffer);
```

**Use cases:** One-off allocation of a largish buffer (4KB to a few hundred KB). For repeated allocation, prefer BUF-01 (ArrayPool).

**Caveats:**

- Guaranteeing that uninitialized memory is never read is the caller's responsibility (always write the whole region before reading)
- POH allocation via `GC.AllocateArray(pinned: true)` costs about 17.5x a normal allocation (measured). Reserve it for avoiding fragmentation with long-lived I/O buffers, and use it once at startup (see R-13 in the rejected list)

---

### 🧺 BUF-07: Reusing reference-type instances with ObjectPool

**Goal:** Reuse expensive-to-construct reference types (parser state, builders, context objects) to reduce allocation and GC.

**Effect:**

- Where `ArrayPool` (BUF-01) is buffer-only, this targets arbitrary reference-type instances
- It only pays off for objects whose construction cost is measurably significant and whose lifetime is well defined. For simple small objects the GC is usually cheaper

**AOT:** ✅ No issues

**Example:**

```csharp
// [ThreadStatic] single-slot pool: minimal and thread-safe (falls back to a normal allocation on reentry)
[ThreadStatic]
private static StringBuilder? cached;

public static StringBuilder Rent()
{
    var builder = cached;
    if (builder is null)
    {
        return new StringBuilder(DefaultCapacity);
    }

    cached = null;   // null it out while checked out, to survive reentry
    return builder;
}

public static void Return(StringBuilder builder)
{
    // do not keep holding a buffer that has grown large
    if (builder.Capacity <= MaxRetainedCapacity)
    {
        builder.Clear();
        cached = builder;
    }
}
```

**Use cases:** Reusing builders and contexts; design guidance when adopting a general-purpose pool implementation.

**Caveats:**

- **Missing returns, double returns, and use-after-return** become hard-to-trace bugs. Wrap it in a `using` scope (the same idea as BUF-04)
- Without a cap on the retained size, an instance that once grew large stays resident
- A pool holding reference types extends object lifetimes unless internal references are cleared on return

**Measured (net10 / x86-64-v4, assembling a key string with StringBuilder):** against 19.97 ns + 648 B for a fresh `new StringBuilder(256)` each time, the `[ThreadStatic]` single-slot pool costs **13.51 ns + 64 B (0.68x; the allocation is the result string only = 0.10x)**. → [Results](benchmarks/results/BUF-07-ObjectPool.md)

---

### 🧺 BUF-08: Working with Memory\<T\> (Span resolution cost and array interop)

**Goal:** `Memory<T>` is not "a `Span<T>` that survives `await`". `.Span` is a property call that resolves the backing store (array / `string` / `MemoryManager<T>`), and it shows up once it is inside a loop.

**Effect:**

- Resolving `.Span` per element costs **3.67x** against hoisting it once out of the loop
- Even at chunk granularity, `memory.Slice(...).Span` loses to slicing a hoisted `Span` by 1.10-1.13x
- `MemoryMarshal.TryGetArray` bridges to APIs that only accept `byte[]` + offset + count **with no copy** (4,120 → 0 B allocated)
- `MemoryManager<T>` is the only way to publish an unmanaged region as `Memory<T>`

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ Resolving .Span on every iteration
for (var offset = 0; offset < length; offset += ChunkSize)
{
    Process(memory.Slice(offset, ChunkSize).Span);
}

// ✅ Resolve once, then slice
var span = memory.Span;
for (var offset = 0; offset < length; offset += ChunkSize)
{
    Process(span.Slice(offset, ChunkSize));
}

// ✅ Bridge to a legacy byte[] + offset + count API without copying
if (MemoryMarshal.TryGetArray<byte>(memory, out var segment))
{
    legacy.Write(segment.Array!, segment.Offset, segment.Count);
}
else
{
    var buffer = memory.ToArray();   // not array backed, so the copy has to stay
    legacy.Write(buffer, 0, buffer.Length);
}
```

```csharp
// Publishing an unmanaged region as Memory<T>
internal sealed unsafe class NativeMemoryManager<T> : MemoryManager<T>
    where T : unmanaged
{
    private readonly int length;

    private T* pointer;

    public NativeMemoryManager(int length)
    {
        this.length = length;
        pointer = (T*)NativeMemory.Alloc((nuint)length, (nuint)sizeof(T));
    }

    public override Span<T> GetSpan() => new(pointer, length);

    public override MemoryHandle Pin(int elementIndex = 0) => new(pointer + elementIndex);

    public override void Unpin()
    {
        // Unmanaged memory never moves, so there is nothing to release
    }

    protected override void Dispose(bool disposing)
    {
        if (pointer is not null)
        {
            NativeMemory.Free(pointer);
            pointer = null;
        }
    }
}
```

**Use cases:** Buffer handling in async I/O, connecting to existing `byte[]`-based APIs, exposing mmap or native interop regions.

**Measured (net10 / x86-64-v4, 4,096 bytes):**

| Form | Resolutions | Time | Ratio | Code size | Allocated |
|---|---:|---:|---|---:|---:|
| `.Span` hoisted + chunk Slice (baseline) | 1 | 843.5 ns | 1.00 | 248 B | 0 B |
| `memory.Slice(...).Span` per chunk | 256 | 887.1 ns | 1.05 | 268 B | 0 B |
| `.Span` hoisted + element access | 1 | 842.3 ns | 1.00 | 201 B | 0 B |
| **`memory.Span[i]` per element** | 4,096 | **2,626.7 ns** | **3.11** | 178 B | 0 B |
| MemoryManager backed, hoisted | 1 | 802.8 ns | 0.95 | 297 B | 0 B |
| MemoryManager backed, resolved per chunk | 256 | 892.2 ns | 1.06 | **518 B** | 0 B |
| `ToArray()` + legacy API (baseline) | — | 1,095.8 ns | 1.00 | 934 B | **4,120 B** |
| **`TryGetArray` + legacy API** | — | **922.8 ns** | **0.84** | **414 B** | **0 B** |

**The penalty is per `.Span` resolution, and the core hides it in proportion to the work behind it.** The same 4,096 bytes cost 3.11x at one resolution per element and 1.05x at one per 16-byte chunk — so when an `await` forces you to keep `Memory<T>`, size the chunk by how much work each resolution feeds. **MemoryManager backing is not a throughput penalty** (0.95x hoisted here, 0.97x on x86-64-v3); its cost is the resolution code, 518 vs 297 B, which only matters where the resolution is not hoisted. → [Measurement](benchmarks/results/BUF-08-MemoryAccess.md)

**Caveats:**

- If the work completes inside a synchronous scope, take a `Span<T>` in the first place (BUF-05). `Memory<T>` is only needed across an async boundary (BUF-04)
- `TryGetArray` returns false for `MemoryManager`-backed and `string`-backed memory. **Always keep the copy fallback reachable**
- `MemoryManager<T>` implements `IDisposable` explicitly, so `manager.Dispose()` resolves to the protected `Dispose(bool)` and fails to compile. Write `((IDisposable)manager).Dispose()`
- Native allocations are outside the GC, so R-13's argument about POH allocation cost does not apply — but freeing becomes your responsibility

---

## ⚙️ JIT: JIT optimization support

### ⚙️ JIT-01: AggressiveInlining / AggressiveOptimization

**Goal:** Force the JIT to inline a method, or to compile it fully optimized from the start.

**Effect:**

- `AggressiveInlining`: Removes the call cost entirely. Ideal for wrapper methods on hot paths
- `AggressiveOptimization`: Bypasses tiered compilation and compiles optimized code from the first call

**AOT:** ✅ No issues, and **this is where the attribute earns its keep.** Measured under NativeAOT the default policy declines this inline - `DefaultPolicy` and `NoInline` come out identical (1,392 vs 1,395 ns) - while `AggressiveInlining` is worth **0.69x**, beating even the JIT's inlined default. Free to add under JIT, decisive under AOT. `AggressiveOptimization` is a separate attribute and effectively meaningless (but harmless) under AOT, which has no tiered compilation. → [Results](benchmarks/results/JIT-01-Inlining.md)

**Example:**

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool TryGetValue<TKey>(...)
{
    // Expanded into the caller; the call instruction disappears
}
```

**Use cases:** Helper methods of one to a few instructions such as `TryGetValue`, `Read`, and `Write`.

**Caveats:**

- Overusing `AggressiveInlining` bloats code size and can hurt instruction-cache efficiency. Limit it to small hot methods
- On .NET 8+, `AggressiveOptimization` disables Dynamic PGO (optimization driven by runtime profiles), so it can end up slower. Always confirm with a benchmark before using it

**Whether `AggressiveOptimization` is safe to remove depends on the shape of the method (measured while applying this to 6 production repositories):** the catalog's "as a rule, do not use it under JIT" holds for PGO-sensitive shapes; **removing it everywhere makes things slower.** Removing the attribute from existing code split cleanly into two groups.

| Shape of the method | Result of removing it | Example |
|---|:---:|---|
| Hot path contains a **delegate / virtual dispatch** the PGO can speculatively devirtualize | **0.84-0.96x (faster)** | Row mapper behind an abstract base, `Func<T, TResult>` accessors in a loop, Span read/write |
| **Straight-line code** (lock + bounds check + return) | +2.3-3.3% (slower) | Slot lookup in a DI container |
| **Small helper that gets inlined** | +11.4% (slower) | `Add` on a pooled list |
| **Large single-pass method** (one walk over the data) | **+16.5-30.9% (slower)** | Report row processing, address parsing |

**How to decide:** look for a dispatch the PGO can speculate on in the hot path. Without one, `AggressiveOptimization` is doing the job of skipping tier-0 and reaching optimized code immediately, and removing it exposes the full promotion cost. The larger the method, the larger the penalty (up to 1.31x here).

**Measured (net10 / x86-64-v4, helper containing a loop x 1024 calls):** NoInlining 1.180 μs vs. default 0.943 μs / Aggressive 0.959 μs. **Inlining itself is a real difference** (NoInline is +25% over the default, with non-overlapping CIs), but **the caller's codegen is byte-identical at 100 B for default and Aggressive** — net10's default policy (PGO) already inlines helpers containing loops, so treat **the attribute as insurance for shapes the heuristics decline**. → [Results](benchmarks/results/JIT-01-Inlining.md)

---

### ⚙️ JIT-02: Branch elimination via IEquatable\<T\> constraints

**Goal:** Add an `IEquatable<T>` constraint to a generic type parameter so the JIT emits dedicated comparison code.

**Effect:**

- The virtual dispatch through `EqualityComparer<T>.Default` is eliminated
- For primitive types it expands to a direct `==` instruction
- Span searches such as `IndexOf` select a type-specialized SIMD implementation
- Taking a struct through a constrained generic (`where TComparer : IComparer<T>` and the like) produces a constrained call and type-specialized code with no boxing. Taking it as an interface-typed parameter (`IComparer<T> comparer`) boxes the struct implementation on every call — "accept comparers and strategies as a struct behind a generic constraint" is the rule of thumb

**AOT:** ✅ No issues. Generics over value types are fully specialized per type at AOT compile time, so the same optimizations apply as under the JIT

**Example:**

```csharp
public ref struct SpanTokenizer<T> where T : IEquatable<T>
{
    public bool MoveNext()
    {
        // T.IndexOf → the JIT selects a type-specialized SIMD implementation
        var index = span[newStart..].IndexOf(separator);
        // ...
    }
}
```

**Use cases:** General-purpose algorithms such as collections, searches, and splitters.

**Measured (from the TYP-02 run):** For dictionary lookups on a 16-byte struct key, the default comparer over a struct that implements `IEquatable<T>` costs **3.7 ns / zero allocation** (a struct without it costs 15.8 ns + 96 B of boxing per lookup — 4.3x). That is precisely the payoff of constraint-driven devirtualization and boxing avoidance → [TYP-02-BitwiseComparer.md](benchmarks/results/TYP-02-BitwiseComparer.md)

---

### ⚙️ JIT-03: Generic specialization via typeof(T) branches

**Goal:** Write `if (typeof(T) == typeof(int))` branches inside a generic method so the JIT's constant folding produces per-type specialized code.

**Effect:**

- The JIT evaluates `typeof(T)` comparisons as compile-time constants and deletes the untaken branches outright. Lining up 10 branches barely adds any cost (measured: 2.20ns vs 2.38ns)
- Boxing-based conversions such as `Convert.ChangeType` (measured: 3.39ns + 24B) are avoided completely

**AOT:** ✅ No issues (value types are fully specialized under AOT too, so the same folding happens)

**Example:**

```csharp
public static T Convert<T>(int value)
{
    if (typeof(T) == typeof(int))
    {
        return Unsafe.As<int, T>(ref value);
    }
    if (typeof(T) == typeof(long))
    {
        var l = (long)value;
        return Unsafe.As<long, T>(ref l);
    }
    // ...
    throw new NotSupportedException();
}
```

**Use cases:** Type-conversion layers; primitive specialization in serializers and formatters.

**Measured (net10 / x86-64-v4, summing int[1024]):** The generic version with typeof(T) branches runs 212.4 ns vs. 213.7 ns for a hand-written int version — **the branches cost nothing** (code size 35 vs 32 B, essentially identical: the JIT folds `typeof(T) == typeof(int)` to a constant per instantiation and removes the branch). Correctness of the fallback path is confirmed by Verify. → [Results](benchmarks/results/JIT-03-TypeofBranch.md)

**Related finding:** Caching `typeof(X)` in a `static readonly Type` field is pointless (the JIT turns `typeof` itself into a constant; measurements show identical time and code size). Prefer readability.

**How to write the reinterpretation:** the example above uses `Unsafe.As<int, T>(ref value)`, but **for same-sized value types make `Unsafe.BitCast<int, T>(value)` the default.** The generated code is identical (21 B in the generic form; with `T = int` the reinterpretation disappears entirely), while `BitCast` rejects a size mismatch at compile time or run time. Keep `Unsafe.As<TFrom, TTo>` for **reinterpretations where the sizes differ** (looking at a prefix, reading as a wider type). → [Measurement](benchmarks/results/LAB-BitCast.md)

---

### ⚙️ JIT-04: Cold-path separation (throw helpers / NoInlining on Grow)

**Goal:** Move rarely taken code such as exception throws and buffer growth into a separate method, shrinking the hot path's code size to encourage inlining.

**Effect:**

- The hot method gets smaller, making the JIT's inlining decision more likely to go your way
- Methods containing `throw` are never inlined, so moving the throw into a helper makes the hot side inlinable
- **That rationale does not apply to methods marked `[MethodImpl(AggressiveInlining)]`.** Inlining is forced there, so whether it is allowed never comes up; the benefit comes from something else — **the exception construction stops being duplicated into every call site**. Even for an `[AggressiveInlining]` argument-validation wrapper, separating the throw takes the call site from **125 B to 81 B (−35%)** and the time from 1.219 to 1.160 ns (0.95x, non-overlapping confidence intervals). **A rationale that does not apply is not a reason to skip the pattern**
- The same design used by the BCL's ThrowHelper and `ArgumentNullException.ThrowIfNull`

**AOT:** ✅ No issues

**Example:**

```csharp
public void Append(char c)
{
    if ((uint)length < (uint)buffer.Length)
    {
        buffer[length++] = c;   // Hot path: keep it small
        return;
    }

    GrowAndAppend(c);           // Cold path: split out and kept out of line
}

[MethodImpl(MethodImplOptions.NoInlining)]
private void GrowAndAppend(char c)
{
    Grow();
    Append(c);
}

[DoesNotReturn]
private static void ThrowInvalidState() => throw new InvalidOperationException(...);
```

**Use cases:** Grow handling in builders/writers, argument validation, and rare error paths in general.

**Implementation in this repo:** [BufferWriterSlim.cs](src/PerformancePatterns/Buf/BufferWriterSlim.cs) / [ValueStringBuilder.cs](src/PerformancePatterns/Txt/ValueStringBuilder.cs) (both split Grow out with NoInlining)

**Measured (net10 / x86-64-v4, isolated microbenchmark):** A fat Write with growth inlined (569 B, not inlined) runs 635.1 ns, while the split version with AggressiveInlining (103 B on the hot side) runs **631.1 ns (0.99x)** — equal time with a 5.5x smaller hot method. A single call per Write is cheap, so the split does not show up as time in isolation. **The value of this pattern is that it makes inlining into the caller possible and unlocks the optimizations beyond it — it is not magic that always makes things faster** — apply it together with measurement. → [Results](benchmarks/results/JIT-04-ColdPathSplit.md)

---

### ⚙️ JIT-05: Skipping work with IsReferenceOrContainsReferences

**Goal:** For a type `T` that contains no references, branch around the cleanup (array clearing and the like) that exists only to release GC references.

**Effect (measured, net10 / x86-64-v4):**

- The JIT constant-folds `RuntimeHelpers.IsReferenceOrContainsReferences<T>()` per type and deletes the untaken branch outright
- Clearing `int[1024]`: 19.0ns unconditionally → **0.008ns** with the branch (the work vanishes entirely; code size 510B → 28B)
- For reference types (`string[]`, the side that does need clearing) the check costs nothing measurable (101.5ns vs 102.4ns, identical code size)

**AOT:** ✅ No issues (value types are fully specialized and folded to a constant under AOT as well)

**Example:**

```csharp
public void Return(T[] array)
{
    // For types with no references, clearing for the GC is unnecessary (the same call the BCL's ArrayPool/List makes)
    if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
    {
        Array.Clear(array);
    }

    pool.Push(array);
}
```

**Use cases:** Clearing on pool return, collection Clear/Remove, serializer buffer cleanup, and switching copy/compare strategies by type.

**Caveats:** Only clearing whose purpose is "let the GC release references" may be skipped. Clearing for security reasons, such as wiping sensitive data, must always run regardless of the type.

---

## 🚦 DSP: Call abstraction and dispatch

### 🚦 DSP-01: Devirtualization via sealed

**Goal:** Mark implementation classes `sealed` so the JIT can replace virtual and interface calls with direct calls plus inlining (devirtualization).

**Effect:**

- Calls through a variable of a sealed type have a known runtime type, so the JIT can lower them to direct calls
- The benefit is context-dependent (measurements show no difference where inlining or guarded devirtualization is already in play), but it costs nothing

**AOT:** ✅ No issues, but **the "worth more under AOT" reasoning did not survive measurement.** Sealing an interface-typed call site is worth nothing on either runtime (0.99x JIT, 1.00x AOT), and the concrete-type win *shrinks* here (0.92x → 0.96x). What does grow under AOT is the cost of interface dispatch itself, measured at 1.06x → 1.30x in [DSP-02](#-dsp-02-choosing-a-call-abstraction) - so hold the **concrete** type on hot paths, not merely a sealed one. See [Results](benchmarks/results/DSP-01-SealedDevirt.md) for why the two benchmarks disagree on the size of that cost

**Example:**

```csharp
public sealed class BinaryFormatter : IFormatter { ... }
```

**Measured (net10 / x86-64-v4, calls through an interface with a single implementation x 1024):**

| How it is held | Time | Ratio |
|---|---:|---|
| Interface reference (non-sealed impl) | 220.7 ns | 1.00 |
| Interface reference (sealed impl) | 221.9 ns | 1.01 (➖ measurement noise; identical code size of 84 B) |
| **Concrete sealed-type reference** | 215.2 ns | **0.98** (27 B, direct call + inlining) |

**On net10, calls through an interface reference did not get faster from sealed** (identical codegen size). The disassembly shows why: on the interface path, **PGO's guarded devirtualization already inlines the body behind a per-iteration type guard** (`cmp [rcx], MT` + inlined add), and that guard predicts perfectly — so the only thing the concrete sealed reference removes is the guard itself, worth ~2% (and a hoisted null check replaces the per-iteration field reload). What the concrete type does deliver is **code size (27 B vs 84 B, a 6-instruction tight loop) and inlining headroom**. Under AOT or without dynamic PGO there is no guarded devirtualization, so the interface path pays a real virtual stub call per iteration — that is where the concrete/sealed form matters most. sealed itself is free, so making it the default stands unchanged. → [Results](benchmarks/results/DSP-01-SealedDevirt.md)

**Design guidance:** Make sealed the default for every library implementation class except those that deliberately allow inheritance by design (the BCL follows the same policy).

---

### 🚦 DSP-02: Choosing a call abstraction

**Goal:** Choose how callbacks, factories, and strategies are held (delegate / interface / function pointer) based on measurements.

**Findings (measured):**

- On recent runtimes (.NET 9/10), calls through an interface or abstract base are as fast as or faster than delegate calls (197μs vs 227μs over one million calls). The old wisdom that "delegates are lighter" does not hold
- A delegate bound directly to a static method can be the slowest form of all (it goes through a thunk that shuffles the `this` argument). If it has to be a delegate, a compiler-cached lambda (the `static (x) => Foo(x)` form) is sometimes faster
- Use a static local function rather than a lambda for small in-method logic (measured: code size 185B vs 6B; the local function is fully inlined and both the delegate allocation and the call disappear)
- Function pointers (`delegate*<T>`) have the smallest code size but **can be the slowest form on net10** (see the measurements below). Only adopt them on the strength of a benchmark

**Measured (net10 / x86-64-v4, addition x 1024):**

| How it is held | Time | Ratio | Code size |
|---|---:|---|---:|
| **Held as a concrete sealed type** | **215.8 ns** | **1.00** | 27 B |
| Through an abstract base | 223.6 ns | 1.04 | 81 B |
| Through an interface | 224.3 ns | 1.04 | 84 B |
| Delegate (static lambda) | 254.6 ns | 1.18 | 85 B |
| **Function pointer `delegate*`** | **1,250.7 ns** | **5.80 (❌ slowest)** | 42 B |

**Why the function pointer comes out slowest:** The JIT cannot inline `calli`, and Dynamic PGO's speculative optimization (guarded devirtualization) does not apply either. A delegate's `Invoke`, by contrast, lets PGO guess the target and inline it, so **"a raw pointer must be faster" does not hold on net10**. Function pointers belong at interop boundaries, under AOT, and on polymorphic targets where speculation cannot help — they are not a general-purpose speed tool.

A well-predicted monomorphic virtual call is nearly free (~4%), so delegate ≒ abstract ≒ interface — the old wisdom that "delegates are heavier than interfaces" is false. → [Results](benchmarks/results/DSP-02-CallAbstraction.md)

**AOT:** ✅ Compatible, but **the ranking in this table is JIT-only and it reverses.** Measured under NativeAOT the delegate becomes the cliff at 5.62x while the function pointer stays at 4.82x - the delegate loses guarded devirtualization and regresses 4.7x in absolute terms, whereas the function pointer barely moves. Interface and abstract dispatch roughly double as well (1.06x → 1.30x). Holding the concrete sealed type is the one row unchanged on both runtimes. → [Results](benchmarks/results/DSP-02-CallAbstraction.md)

**Use cases:** Factory tables in DI containers, formatter resolution in serializers, and holding pipeline stages.

---

### 🚦 DSP-03: Immutable handler arrays (avoiding multicast delegates)

**Goal:** Hold and invoke events/callbacks that may have multiple subscribers with an immutable array plus `Volatile.Read` instead of a multicast delegate (`+=`).

**Effect:**

- Multicast delegates degrade roughly in proportion to the subscriber count. The break-even point is two subscribers; at four, the immutable-array foreach is 2.75x faster (measured below)
- The publish side needs no lock (copy-on-write swaps the array only when subscriptions change)

**AOT:** ✅ No issues

**Example:**

```csharp
private readonly object sync = new();
private Action<T>[] handlers = [];

public void Subscribe(Action<T> handler)
{
    lock (sync)
    {
        var current = handlers;
        var next = new Action<T>[current.Length + 1];
        current.CopyTo(next, 0);
        next[^1] = handler;
        Volatile.Write(ref handlers, next);
    }
}

public void Publish(T value)
{
    foreach (var handler in Volatile.Read(ref handlers))
    {
        handler(value);
    }
}
```

**Use cases:** Event mechanisms with multiple subscribers such as message buses, observers, and change notification.

**Implementation in this repo:** [HandlerList.cs](src/PerformancePatterns/Dsp/HandlerList.cs) / [Tests](tests/PerformancePatterns.Tests/Dsp/HandlerListTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Dsp/HandlerListBenchmark.cs) / [Results](benchmarks/results/DSP-03-HandlerList.md)

**Measured (net10 / x86-64-v4, by subscriber count):**

| Subscribers | Multicast | Immutable array | Ratio |
|---:|---:|---:|---|
| 1 | 0.12 ns | 0.68 ns | 5.72 (❌ the array is slower) |
| 2 | 3.49 ns | 1.11 ns | **0.32** |
| 4 | 6.05 ns | 1.85 ns | 0.31 |
| 8 | 11.00 ns | 3.47 ns | 0.32 |

Multicast degrades roughly in proportion to the subscriber count, while the array grows only gently. **The break-even point is two subscribers.**

**Caveats:** Where a single subscriber dominates, a plain delegate remains fastest (a single delegate invokes directly with no loop). If unsubscribes are frequent, factor in the cost of rebuilding the array.

---

### 🚦 DSP-04: static lambdas everywhere (threading TState through)

**Goal:** **Make `static` the default for lambdas and local functions.** The `static` modifier makes the compiler forbid captures; when state is needed, pass it explicitly as a `TState` argument instead of capturing it.

**Effect:**

- A lambda that captures outer variables can allocate a display class plus a delegate (per call inside loops and on hot paths). With `static`, an accidental capture becomes a compile error, and the compiler caches the delegate for zero allocation
- "Does not depend on outer state" becomes explicit in the signature, which makes review and codegen verification easier
- The BCL itself provides state-carrying APIs (`ConcurrentDictionary.GetOrAdd(key, factory, state)`, `string.Create(length, state, action)`, `CancellationToken.Register(callback, state)`, `Task.ContinueWith(action, state)`)

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ Capturing lambda: allocates a closure and can pull in unintended dependencies
var found = list.Find(x => x.Id == targetId);

// ✅ Add static first. If state is needed, pass it as TState
var found = list.Find(targetId, static (x, id) => x.Id == id);

// Carry multiple values as a tuple in the state
var item = cache.GetOrAdd(key, static (k, s) => s.factory.Create(k, s.options), (factory, options));

// ✅ API side: give public APIs that take a callback a TState overload as well
public T? Find<TState>(TState state, Func<T, TState, bool> predicate) { ... }
```

**Use cases:** LINQ-style utilities, collection searches, dictionary GetOrAdd, continuation/callback registration, and Map/Bind on Result/Option types.

**Measured (net10 / x86-64-v4, a search whose predicate uses a local that changes each iteration):** A capturing lambda costs 7.09 ns + **88 B per call** (closure + delegate), while a static lambda with TState costs **2.96 ns / 0 B (0.42x)** (the compiler caches the delegate). → [Results](benchmarks/results/DSP-04-StaticLambda.md)

**Design guidance:** Codify the sequence "① put `static` on the lambda first → ② if it fails to compile, reconsider whether that state is really needed → ③ if it is, pass it as `TState`". This is the same principle as STK-04 (static local method iterators) applied more broadly; public APIs that take callbacks should always offer a TState overload so callers can follow the rule.

---

### 🚦 DSP-05: Precomposing delegate pipelines

**Goal:** Move the composition, branch resolution, and delegate creation currently done on every call into a single pass at initialization.

**Effect:**

- Assembling the middleware/filter chain at startup eliminates the per-request composition cost and delegate allocation
- With zero elements you can bypass entirely: build no delegation at all and call the body directly
- An implementation that passes a fresh lambda on every render or call changes the reference each time, which also defeats downstream caching and change detection (especially visible in UI frameworks)

**AOT:** ✅ No issues

**Example:**

```csharp
public sealed class Pipeline
{
    private readonly Func<Context, ValueTask>? composed;

    public Pipeline(IReadOnlyList<IFilter> filters, Func<Context, ValueTask> terminal)
    {
        // ✅ Compose once at startup. Create no delegate when empty
        composed = filters.Count == 0 ? null : Compose(filters, terminal);
        this.terminal = terminal;
    }

    public ValueTask InvokeAsync(Context context)
        => composed is null ? terminal(context) : composed(context);
}

// ✅ Fix callbacks and render fragments in the constructor, then pass the same reference forever after
private readonly Action<int> onChanged;
public Widget() => onChanged = HandleChanged;
```

**Use cases:** Middleware/filter chains, UI render fragments (RenderFragment), command CanExecute, and resolving conditional branches at initialization.

**Related trick:** In APIs where resubscription or reconfiguration runs every time (`OnParametersSet` and friends), compare against the previous target with `ReferenceEquals` and **skip the whole operation when nothing changed**.

**Measured (net10 / x86-64-v4, three middleware stages):** Composing on every call costs 19.7 ns + 264 B (3 closures + delegate creation), while **precomposition costs 1.27 ns / 0 B (0.064x = about 16x)**. A bare terminal call is ~0 ns, so traversing the precomposed chain fits in about 1.3 ns across three stages. → [Results](benchmarks/results/DSP-05-PipelineCompose.md)

**Caveats:** The benefit scales with the complexity of the composition and the call frequency.

---

### 🚦 DSP-06: Index-forwarding pipelines (delegate-free chains)

**Goal:** Remove the `next` delegate from an interceptor chain entirely. Instead of handing each hop a continuation, hand it the pipeline plus its own index and let it call forward.

**Effect:**

- The hop becomes an ordinary interface call with two extra arguments - there is no delegate to create, capture, or keep alive
- The chain position travels as a by-value `int`, so the pipeline object stays stateless and reentrant: one instance serves concurrent invocations
- Beats [DSP-05](#-dsp-05-precomposing-delegate-pipelines)'s precomposed chain, which is already the good answer - precomposition removes the *allocation*, index forwarding removes the *delegate*

**AOT:** ✅ No issues (fewer delegates than DSP-05, so if anything it is friendlier)

**Example:**

```csharp
public sealed class Pipeline
{
    private readonly IInterceptor[] interceptors;
    private readonly Func<int, ValueTask> terminal;

    public ValueTask InvokeAsync(int command) => ProcessNext(command, 0);

    // ✅ The hop receives the pipeline and its own index - no delegate is created anywhere
    public ValueTask ProcessNext(int command, int index)
        => index < interceptors.Length
            ? interceptors[index].InvokeAsync(command, this, index)
            : terminal(command);
}

public sealed class LoggingInterceptor : IInterceptor
{
    // ✅ Forward by advancing the index, not by invoking a captured continuation
    public ValueTask InvokeAsync(int command, Pipeline pipeline, int index)
        => pipeline.ProcessNext(command + 1, index + 1);
}
```

**Use cases:** Middleware stacks, HTTP/RPC client policy chains (retry, logging, auth), in-process message bus interceptors - anywhere the chain is fixed at build time but invoked per request or per message.

**Measured (net10 / x86-64-v4, 4 pass-through interceptors):** Allocating a closure per hop costs 32.5 ns / 488 B. A context-cached continuation delegate is 13.6 ns (0.42x), DSP-05's precomposed chain is 11.1 ns (0.34x), and **index forwarding is 8.85 ns (0.27x)**. All three non-naive forms drop to 72 B, which is the caller's own `Task` return rather than per-hop cost - none of them allocates per hop. → [Results](benchmarks/results/DSP-06-IndexForward.md)

**Caveats:**

- The interceptor signature grows by two parameters and names the concrete pipeline type, so the interceptor interface becomes coupled to the pipeline. DSP-05's `Func<T, ValueTask>` stays decoupled - that is the trade
- **Do not hold the index in a field on a shared context.** That is the "cached continuation" variant measured above: it is the slowest of the three and it forces a per-invocation context object (and therefore a pool) behind it
- With one or two hops the difference disappears into the surrounding work. This is for chains on a hot path

---

## 🏷️ TYP: Type system techniques

### 🏷️ TYP-01: Static type slots (TypeMap / TypeSlot)

**Goal:** Replace a dictionary keyed by `Type` with array index access that needs no hashing.

**Effect:**

- The JIT treats `TypeSlot<T>.Index` as a constant, so this becomes effectively "a direct index into an array"
- No hashing, collision resolution, or locking
- Measured: about 6x faster than a thread-safe `Dictionary<Type, T>`-based implementation (single-threaded reads)

**AOT:** ⚠️ Conditional

- Access through the generic API (`TryGetValue<T>()`) works fine under AOT (generic static fields are AOT-compatible)
- An implementation that uses `typeof(TypeSlot<>).MakeGenericType(type)` to allocate a slot from a runtime `Type` object hits **IL3050 (AOT-incompatible)**. Implement the runtime `Type` path with a `Dictionary<Type, int>` fallback

**Example:**

```csharp
internal static class TypeSlot
{
    private static int nextIndex = -1;

    public static int Next() => Interlocked.Increment(ref nextIndex);
}

internal static class TypeSlot<T>
{
    // Numbered once per type. The JIT treats this as a constant
    public static readonly int Index = TypeSlot.Next();
}

// Call site
// JIT-resolved path (type argument known)
map.TryGetValue<MyService>(out var svc);

// Runtime-resolved path (dynamic type; Dictionary fallback)
map.TryGetValue(typeof(MyService), out var svc);
```

**Use cases:** DI containers, type-based handler/factory registration, and component caches.

**Implementation in this repo:** [TypeMap.cs](src/PerformancePatterns/Typ/TypeMap.cs) / [TypeSlot.cs](src/PerformancePatterns/Typ/TypeSlot.cs) / [Tests](tests/PerformancePatterns.Tests/Typ/TypeMapTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Typ/TypeMapBenchmark.cs) / [Results](benchmarks/results/TYP-01-TypeMap.md)

**Measured (net10 / x86-64-v4, resolving against 8 registered types):**

| Path | Time | Ratio | Code size |
|---|---:|---|---:|
| `Dictionary<Type, T>` (baseline) | 2.47 ns | 1.00 | 921 B |
| `FrozenDictionary` | 3.07 ns | 1.25 (❌ slower) | 45 B |
| **TypeMap generic path** | **0.23 ns** | **0.09 (about 11x)** | 34 B |
| TypeMap runtime-Type path | 10.4 ns | 4.22 (❌ slower) | 3,486 B |

**The value is in the generic path only** (the slot number becomes a JIT constant, making it effectively "an index into an array"). The runtime-Type path is a dictionary lookup followed by an array access, so it is slower than a plain Dictionary; adopt this only when you can design the primary path around calls whose type is known statically.

**Caveats:** Grow the slot array under a lock with an array swap (copy-on-write) so the read path stays lock-free. The implementation above keeps the runtime Type → slot mapping in a `Dictionary<Type, int>`, so it avoids `MakeGenericType` and is **AOT-safe**.

---

### 🏷️ TYP-02: BitwiseComparer\<T\> (raw byte comparison)

**Goal:** Compare `unmanaged` value types for equality and ordering over their raw bytes, ignoring any `Equals` override.

**Effect:**

- Lets a value type with a custom `Equals` serve as a dictionary/set key with the intended semantics
- Fast comparison via the SIMD-optimized `SequenceEqual` / `SequenceCompareTo`

**AOT:** ✅ No issues

**Example:**

```csharp
var dict = new Dictionary<MyStruct, string>(BitwiseComparer<MyStruct>.Instance);
```

```csharp
public sealed class BitwiseComparer<T> : IEqualityComparer<T>
    where T : unmanaged
{
    public static BitwiseComparer<T> Instance { get; } = new();

    public bool Equals(T x, T y) =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref x, 1))
            .SequenceEqual(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref y, 1)));

    // GetHashCode is implemented by hashing the byte sequence
}
```

**Use cases:** Cases where identity should be decided by bit pattern (colors, flags, vectors, and so on).

**Implementation in this repo:** [BitwiseComparer.cs](src/PerformancePatterns/Typ/BitwiseComparer.cs) (`IEqualityComparer<T>` + `IComparer<T>`, hashing via `HashCode.AddBytes`) / [Tests](tests/PerformancePatterns.Tests/Typ/BitwiseComparerTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Typ/BitwiseComparerBenchmark.cs) / [Results](benchmarks/results/TYP-02-BitwiseComparer.md)

**Measured (net10 / x86-64-v4, per dictionary lookup on a 16-byte struct key):**

| Comparer | Time | Ratio | Allocation |
|---|---:|---|---:|
| Default comparer + struct without IEquatable (baseline) | 15.8 ns | 1.00 | **96 B (❌ boxing)** |
| **BitwiseComparer + the same struct** | 8.4 ns | **0.54** | 0 B |
| Default comparer + struct implementing IEquatable | 3.7 ns | 0.23 | 0 B |

**Using a struct that does not implement IEquatable as a dictionary key with the default comparer boxes on every lookup.** BitwiseComparer brings that to 0.54x with zero allocation, without writing an Equals. A hand-written `IEquatable` implementation (0.23) is still fastest, though, so **if you own the type, implementing IEquatable is the first choice**. This comparer is for external types, for bypassing a custom Equals, and for generated code that swaps the comparer through a type parameter.

**Caveats:** Structs containing padding can compare unequal despite being logically equal, because the padding bytes are uninitialized. Restrict use to types with a padding-free layout (or `Pack = 1`).

---

### 🏷️ TYP-03: UnsafeAccessor (direct access to non-public members)

**Goal:** Access private/internal fields, methods, and constructors directly, without reflection (.NET 8+).

**Effect:**

- The signature is resolved at compile time, giving the speed of a direct call or direct field access (orders of magnitude faster than going through `MethodInfo.Invoke`)
- The boxing and argument-array allocation that come with reflection calls disappear
- Also usable to call optimized BCL-internal methods directly (ones the public API only reaches through extra branching, for instance)

**AOT:** ✅ No issues. Because binding happens at compile time it works under Native AOT, and trimming preserves the referenced members (an AOT-compatible replacement for private reflection)

**Example:**

```csharp
// Call a BCL-internal static method directly
// (for a static method, the first parameter's type names the target type; pass null as the value)
[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetHashCodeOrdinalIgnoreCase")]
private static extern int GetHashCodeOrdinalIgnoreCase(string? self, ReadOnlySpan<char> value);

var hash = GetHashCodeOrdinalIgnoreCase(null, span);
```

```csharp
// ref access to a non-public field
[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_message")]
private static extern ref string? GetMessageField(Exception exception);
```

**Use cases:** Fast calls into BCL and third-party internal APIs, non-public field access in serializers and the like, and test accessors.

**Caveats:**

- The target member is named by string, so an internal implementation change in the referenced library becomes a runtime error (`MissingFieldException` / `MissingMethodException`). Assume internal APIs carry no compatibility contract, and put a process in place so tests catch this on version upgrades
- Generic types and generic methods are supported from .NET 9 on (not in .NET 8). To work with a non-public *type* itself, use .NET 10's `UnsafeAccessorTypeAttribute`
- Within your own codebase prefer plain internal plus `InternalsVisibleTo`; treat UnsafeAccessor as the tool for "external code you cannot change"

**Measured (net10 / x86-64-v4, reading a non-public int field):** UnsafeAccessor 0.192 ns = public property 0.192 ns (**identical code size of 23 B = compiled to a direct field load**). `FieldInfo.GetValue` costs 4.77 ns + **24 B of boxing per call** (24.9x). → [Results](benchmarks/results/TYP-03-UnsafeAccessor.md)

---

### 🏷️ TYP-04: Per-type caching with generic static classes

**Goal:** Hold an artifact computed once per type (converter, delegate, metadata) in a static field of a `static class Cache<T>` and retrieve it without a dictionary lookup.

**Effect (measured):** Calling `TypeDescriptor.GetConverter` every time costs 36.3ns → 7.96ns with a static cache (about 4.6x). TYP-01 (TypeSlot) is an application of this pattern.

**AOT:** ✅ The pattern itself is fine (generic static fields are AOT-compatible). If the cached content is produced by reflection (`TypeDescriptor` and the like), that API needs its own trimming handling ([aot-compatibility.md](docs/aot-compatibility.md) AOTP-05)

**Example:**

```csharp
private static class ConverterCache<T>
{
    public static readonly TypeConverter Converter = TypeDescriptor.GetConverter(typeof(T));
}

public static T? Convert<T>(string value)
    => (T?)ConverterCache<T>.Converter.ConvertFromInvariantString(value);
```

**Use cases:** Type-conversion layers, formatter resolution in serializers, and holding type metadata.

**Implementation in this repo:** [TypeSlot.cs](src/PerformancePatterns/Typ/TypeSlot.cs) (`TypeSlot<T>.Index` — the minimal form of this pattern, and the basis for TYP-01)

**Measured:** In the equivalent run (TYP-06), reading a generic static field costs **~0 ns / 6 B of code** (below measurement resolution; the `Dictionary<Type, T>` cache costs 2.7 ns) → [TYP-06-StaticArtifact.md](benchmarks/results/TYP-06-StaticArtifact.md)

**Caveats:** Static constructor initialization runs only once per type, on first use. If you put fallible initialization in it, the `TypeInitializationException` stays cached from then on, so design it to store a fallback value meaning "unsupported" on failure.

---

### 🏷️ TYP-05: Skipping type checks on casts with Unsafe.As

**Goal:** Where the registry design structurally guarantees the type correspondence, drop the runtime type check of a normal cast by using `Unsafe.As`.

**Effect (measured):**

- `(Action<object?>)obj` 3.43ns → `Unsafe.As<Action<object?>>(obj)` 1.59ns (about 2x), code size 498B → 67B
- Typed resolution in a DI registry (`Resolve<T>`) is about 1.7x as well, and it curbs the cast-code bloat per generic instantiation

**Measured (net10 / x86-64-v4, a 1024-element object[] whose types are structurally guaranteed):**

| Approach | Time | Ratio | Code size |
|---|---:|---|---:|
| `(string)value` (castclass) | 335.5 ns | 1.00 | 274 B |
| `is string text` pattern | 324.7 ns | 0.97 | 57 B |
| **`Unsafe.As<string>(value)`** | **212.8 ns** | **0.63** | **33 B** |

castclass drags in a cast helper and an exception path, making the code 8x larger. Use Unsafe.As **only where the registry design can guarantee the type invariant** (a wrong type means silent memory corruption). → [Results](benchmarks/results/TYP-05-UnsafeAsCast.md)

**AOT:** ✅ No issues

**Example:**

```csharp
private readonly Dictionary<Type, object> factories = new();

public void Register<T>(Func<T> factory) => factories[typeof(T)] = factory;

public T Resolve<T>()
{
    // Registration is only possible through Register<T>, so the typeof(T) → Func<T> mapping is structurally guaranteed
    var factory = factories[typeof(T)];
    return Unsafe.As<Func<T>>(factory)();
}
```

**Use cases:** The resolution path of type-keyed registries (DI, formatter tables, handler tables).

**Caveats:**

- If the type correspondence breaks, you do not get an `InvalidCastException` — it silently corrupts (undefined behavior). Enforce type safety in the registration API and confine `Unsafe.As` behind a private boundary
- Verifying with a normal cast plus `Debug.Assert` in Debug builds and using `Unsafe.As` only in Release is also a workable arrangement
- **For value-to-value reinterpretation use `Unsafe.BitCast`, not `Unsafe.As<TFrom, TTo>`.** This pattern (`Unsafe.As<T>(object)`) is a reference cast and a different thing entirely, but the shared name makes the two easy to confuse. Bit reinterpretation of same-sized value types can move to the safe form at no cost in generated code (JIT-03 / [Measurement](benchmarks/results/LAB-BitCast.md))

---

### 🏷️ TYP-06: Static pre-assembly of per-type artifacts

**Goal:** Fix strings and metadata determined by type (SQL fragments, type names, formats) **once in a generic static initializer**, rather than reassembling them at runtime.

**Effect:**

- At runtime it is just a static field read — no dictionary lookup, no string concatenation (the "artifact is a string or SQL" variant of TYP-04)
- The type initializer runs once per type, so the initialization cost is amortized
- The concatenation folds into a single `String.Concat`. Always appending the separator and doing `Length -= n` at the end removes the first-element branch

**AOT:** ✅ No issues (generic statics are AOT-compatible; if the content is built with reflection, that API needs its own trimming handling)

**Example:**

```csharp
internal static class SqlInsert<T>
{
    // Assembled once in the type initializer; afterwards it is only a static field read
    public static readonly string Sql = Build();

    private static string Build()
    {
        var builder = new StringBuilder();
        builder.Append("INSERT INTO ").Append(TableName<T>.Value).Append(" (");
        foreach (var column in Columns<T>.All)
        {
            builder.Append(column.Name).Append(", ");   // Always append the separator
        }

        builder.Length -= 2;                             // Trim it off in one go at the end
        return builder.Append(") VALUES (...)").ToString();
    }
}

// The call site is a static read with no dictionary lookup
var sql = SqlInsert<Order>.Sql;
```

**Use cases:** SQL generation in O/R mappers, log and diagnostic strings containing type names, and serializer schema fragments.

**Measured (net10 / x86-64-v4, obtaining a SQL fragment):**

| Approach | Time | Ratio | Allocation | Code size |
|---|---:|---|---:|---:|
| Rebuild every time (baseline) | 57.0 ns | 1.00 | 760 B | 5,220 B |
| `Dictionary<Type, string>` cache | 2.7 ns | 0.048 | 0 B | 936 B |
| **Generic static field** | **~0 ns** | **0.000** | **0 B** | **6 B** |

Reading a static field is essentially free — below measurement resolution (the same shape as TYP-01's generic path). Use the dictionary only for calls where the type is not known statically. → [Results](benchmarks/results/TYP-06-StaticArtifact.md)

**Caveats:** If the type initializer throws, the `TypeInitializationException` stays cached from then on. Design any fallible construction to store a fallback value meaning "unsupported" (the same caveat as TYP-04).

---

### 🏷️ TYP-07: Choosing the hash source for Type keys

**Goal:** When the type is only known at runtime and you have to hash a `Type`, pick the cheapest source of identity. TYP-01's static slot does not apply here - this is the fallback path it tells you to avoid, made as cheap as it can be.

**Effect (measured, net10 / x86-64-v4, 32-entry node map, 32 hit probes / 8 miss probes):**

| Hash source | Hit | Miss | Code size |
|---|---|---|---|
| `type.GetHashCode()` | 1.362 ns | 1.251 ns | 403 B |
| `RuntimeHelpers.GetHashCode(type)` | 1.116 ns (0.82x) | 1.197 ns (0.96x) | 355 B |
| **`type.TypeHandle.Value`** | **0.848 ns (0.62x)** | **0.812 ns (0.65x)** | **192 B** |

The bucket layout is held identical across the three so that only the acquisition path varies. The virtual call keeps its dispatch; `RuntimeHelpers.GetHashCode` inlines but still reads the object header and checks whether a hash has been assigned; `TypeHandle.Value` is a plain field read of the type handle with no branch, which is why its code is less than half the size.

**AOT:** ⚠️ **The ranking reverses.** Measured on a NativeAOT publish, the plain virtual `GetHashCode` is the *fastest* of the three and both alternatives are slower than it (identity 1.18x hit / 1.23x miss, handle 1.08x / 1.19x). Under AOT the closed type graph lets `RuntimeType.GetHashCode` be statically devirtualized, so the dispatch that makes it slowest under JIT is gone. Pick by target; if you ship both, the virtual call is the safe default because it is never worst on either runtime

**Example:**

```csharp
// ❌ Virtual dispatch on every lookup
var index = key.GetHashCode() & mask;

// ⚠️ Better, but still an object-header read plus an "is a hash assigned yet" check
var index = RuntimeHelpers.GetHashCode(key) & mask;

// ✅ Plain field read. The handle is pointer-aligned, so the low 3 bits are always
//    zero - shift them off or a power-of-two table uses only 1/8 of its slots
var index = (int)((ulong)key.TypeHandle.Value.ToInt64() >> 3) & mask;
```

**Use cases:** Runtime type dispatch tables - serializer formatter lookup, DI resolution caches, message handler registries: anywhere a `Type` arrives as data rather than as a generic argument.

**Measured (net10 / x86-64-v4):** Under JIT, `TypeHandle.Value` is **0.62x on hit and 0.65x on miss** against the virtual `GetHashCode`, with code size dropping 403 B → 192 B. Under **NativeAOT the order reverses** and the virtual call wins (see AOT above). → [Results](benchmarks/results/TYP-07-TypeHashSource.md)

**Caveats:**

- **The win is JIT-only.** On NativeAOT this pattern is a pessimization - measure your actual target before adopting
- **Do not forget the shift.** The handle is pointer-aligned, so without `>> 3` only 8 of 64 buckets are reachable and the longest chain grows to 8. Correctness will not catch this: the same shift is applied at insert and lookup, so lookups stay right and only the distribution collapses. The alignment itself was confirmed to hold on an AOT publish (40/40 handles 8-byte aligned)
- The handle is stable for the process lifetime of a loaded type. Under a collectible `AssemblyLoadContext` an unloaded type's address can be reused, so this is only safe while the table holds a strong `Type` reference for every live entry
- If the type *is* known statically, none of this applies - use [TYP-01](#️-typ-01-static-type-slots-typemap--typeslot)'s generic slot at 0.09x instead

---

## 🔢 BIT: Bit manipulation and branchless optimization

### 🔢 BIT-01: Lightweight hashing that exploits domain constraints

**Goal:** Give up the "every character contributes, high distribution quality" guarantees of a general-purpose hash (`string.GetHashCode` etc.) where domain constraints allow, and replace it with an O(1) purpose-built hash.

**Effect:**

- Produces a hash in constant time regardless of string length
- Measured: about 8.5x faster than `string.GetHashCode(ReadOnlySpan<char>)`, about 4x faster than the OrdinalIgnoreCase variant
- Case-insensitive use only needs the sampled characters normalized (no full ToUpper scan)

**AOT:** ✅ No issues

**Example:**

```csharp
// Combine the length with just three characters (first, middle, last) via shift/XOR
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int GetHashCode(ReadOnlySpan<char> value)
{
    var length = value.Length;
    if (length is 0)
    {
        return 0;
    }

    ref var head = ref MemoryMarshal.GetReference(value);
    var first = Unsafe.Add(ref head, 0);
    var middle = Unsafe.Add(ref head, length >> 1);
    var last = Unsafe.Add(ref head, length - 1);
    return (length << 16) ^ (first << 8) ^ (middle << 4) ^ last;
}
```

```csharp
// Case-insensitive variant: normalize only the three sampled characters
return (length << 16)
    ^ (char.ToUpperInvariant(first) << 8)
    ^ (char.ToUpperInvariant(middle) << 4)
    ^ char.ToUpperInvariant(last);
```

**Use cases:** Reverse lookup of enum names, keyword tables, protocol header names — any lookup keyed by "short identifiers drawn from a small, known set".

**Design guidance:** Ask whether the quality a general-purpose implementation guarantees (all characters contributing, distribution, collision resistance) is actually needed in this domain, and drop it if not. The essence of the pattern is turning the constraint "keys are short, few, and known" into performance.

**Caveats:**

- Collisions obviously happen (e.g. `AxxxBxxxC` and `AyyyByyyC` hash the same). Always pair a hash match with a full equality comparison, and check the collision rate against the real key set
- With no seed or randomization it is defenseless against hash flooding (deliberately injecting colliding keys). Do not use it for general-purpose hash tables that accept external input as keys — reserve it for closed, known sets
- The implementation’s `CalculateHash` reads its three characters through a manual ref (`GetReference` + `Unsafe.Add`). **The indexed form cannot eliminate one bounds check on `value[length >> 1]`** (the Tier1 code keeps an RNGCHKFAIL path: 128 B vs 115 B, 56 vs 49 instructions) while the time difference is below measurement resolution — kept as the R-02 exception (sampling access whose range is guaranteed by construction)

**Implementation in this repo:** `CalculateHash` in [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs) (measurements in [COL-04](benchmarks/results/COL-04-SampledNameTable.md))

**Measured findings on applicability:**

- The comparison against the C# compiler's string switch was re-measured in [TXT-10](#-txt-10-aggregating-string-matching-into-a-switch). **The compiler picks its strategy by keys-per-length-bucket, not by key count** - only at 128 keys does it switch to a full-text FNV hash plus a **191-node binary search** over the hashes; it never becomes a jump table
- The winner changes with size: **at 4 keys the generated code is identical to a hand-written `Equals` chain** (60 instructions, 263 B); **up to 64 keys the compiler's switch and a sampled-hash switch are level** (1.06 hit / 1.00 miss at 64); **above 64 the sampled hash wins** (at 128 the compiler's switch degrades to 1.73x hit / 3.79x miss). **Key length is not a criterion** (no reversal even at 16 keys of 58-62 characters)
- The runtime table (`SampledNameTable`) loses to the compiler's switch from 16 to 128 keys (1.09-1.41x hit). **Use a switch (or a generated sampled-hash switch) when the key set is fixed at compile time, and the runtime table only when the set is known only at run time**
- For case-insensitive enum name parsing it is overwhelming: 0.11-0.24x versus `Enum.TryParse` (ignoreCase). A plain string switch is Ordinal and therefore unusable for ignoreCase, which leaves this pattern the only option. For a handful of entries a chain of `Equals(OrdinalIgnoreCase)` ifs (0.17x) is enough
- For key sets where fixed first/middle/last sampling collides, search for non-colliding sampling positions at code-generation time (Source Generator) and bake them in as constants

---

### 🔢 BIT-02: Power-of-two sizing plus masking to replace modulo

**Goal:** Avoid the integer division of `% length` in index computations such as hash tables by rounding the size to a power of two and replacing it with the mask `& (length - 1)`.

**Effect:** An integer division (tens of cycles) becomes a bitwise AND (one cycle). Signed `%` is heavier still because of the correction instructions for negative values.

**AOT:** ✅ No issues

**Example:**

```csharp
var size = (int)BitOperations.RoundUpToPowerOf2((uint)requested);
var mask = size - 1;
// ...
var index = hash & mask;
```

**Use cases:** Hand-rolled hash tables, ring buffers, pool bucket computation.

**Implementation in this repo:** [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs) (rounds the bucket count to a power of two and indexes with `hash & mask`)

**Measured (net10 / x86-64-v4, 1024 bucket index computations):**

| Approach | Time | Ratio |
|---|---:|---|
| `%` with a runtime size (division instruction) | 1,203.5 ns | 1.00 |
| **Power-of-two mask `&`** | 215.3 ns | **0.18** |
| `%` with a constant size | 213.3 ns | 0.18 |

**Manual masking is only needed when the size is decided at runtime.** For `%` against a constant power of two the JIT already lowers to an AND, so write it as-is (mask and constant-modulo produce the same 51 B of code and identical time). → [Results](benchmarks/results/BIT-02-PowerOfTwoMask.md)

**Caveats:**

- The JIT cannot lower `/ 2` or `% 2` on a signed int to a plain shift (negative-value correction gets inserted). Where non-negativity is guaranteed, switch to uint or the unsigned right shift `>>>` (C# 11)
- Unconditional uint-cast tricks aimed at eliminating bounds checks have been measured to no longer help on recent runtimes (hand-rewriting range checks is also auto-fused by the JIT — R-18)

---

### 🔢 BIT-03: Bit scanning and counting with BitOperations

**Goal:** Replace naive loops for bitmap scanning and bit counting with hardware instructions (`TrailingZeroCount` / `PopCount` / `Log2` etc.).

**Effect (measured, net10 / x86-64-v4, 64 sparse ulongs with 7 bits set):**

- Scanning set bits: full 64-bit loop 1,056ns → TZCNT approach **141ns (0.13, 7.5x)**
- Counting bits: manual loop 854ns → `PopCount` **12.8ns (0.01, 67x)**

**AOT:** ✅ No issues (hardware instructions on supporting CPUs, software fallback elsewhere)

**Example:**

```csharp
// Walk only the set bits: take the lowest set bit position, then clear it with mask &= mask - 1
while (mask != 0UL)
{
    var bit = BitOperations.TrailingZeroCount(mask);
    ProcessSlot(bit);
    mask &= mask - 1;
}
```

**Use cases:** Free-slot search in bitmap allocators and pools, sparse sets, flag aggregation, size computation via `RoundUpToPowerOf2` (BIT-02) or `Log2`.

---

### 🔢 BIT-04: General-purpose hashing with XxHash3

**Goal:** Delegate non-cryptographic hashing (cache keys, checksums, duplicate detection) to an optimized implementation (`System.IO.Hashing`) instead of a hand-rolled FNV-1a or `string.GetHashCode`.

**Effect:**

- XxHash3 has high throughput on long inputs and is available through the static `HashToUInt64` / `Hash` APIs
- A `char` sequence can be reinterpreted as bytes with `MemoryMarshal.Cast<char, byte>`, and that conversion is **measured to be zero-cost** (no different from a `fixed` pointer). Note though that `Cast` **changes the length when element sizes differ and silently truncates the remainder**, and **performs no alignment check** (it can raise `DataMisalignedException` on Arm), so the caller has to guarantee that the byte length is a multiple of the element size
- `string.GetHashCode` is randomized per process, so it **cannot be used where a value must be stable across processes or persisted**. XxHash3 is stable

**AOT:** ✅ No issues (NuGet: System.IO.Hashing)

**Example:**

```csharp
using System.IO.Hashing;

// Reinterpret the char sequence as bytes and hash it (no copy)
var hash = XxHash3.HashToUInt64(MemoryMarshal.AsBytes(value.AsSpan()));
```

**Use cases:** Distributed cache keys, file and buffer checksums, sharding.

**Choosing between them:**

- Small, known key set → BIT-01 (sampled hash). Faster still because it never reads the whole text
- Short ASCII token matching → TXT-04 (direct byte comparison)
- General purpose, long inputs, or a stable value is required → this pattern

**Measured (net10 / x86-64-v4, versus string.GetHashCode):**

| Implementation | 8 chars | 64 chars | 512 chars |
|---|---|---|---|
| `string.GetHashCode` (baseline) | 1.00 | 1.00 | 1.00 |
| **XxHash3 (via Cast)** | **0.26** | **0.21** | **0.09** |
| XxHash3 (via fixed) | 0.25 | 0.21 | 0.07 |
| FNV-1a hand-written loop | 0.45 | **1.05 (❌)** | **1.53 (❌)** |
| Sampled hash (BIT-01) | 0.06 | 0.008 | 0.001 |

XxHash3 is already faster at 8 characters and the gap widens with length. `MemoryMarshal.AsBytes` and `fixed` are equivalent (no pinning required, so prefer the cast) — the reinterpretation is zero-cost. A hand-written hash loop (FNV-1a) is beaten by the vectorized BCL from 64 characters on, so do not roll your own. → [Results](benchmarks/results/BIT-04-XxHash3.md)

**Caveats:** Being a non-cryptographic hash, it cannot be used for tamper detection or signatures.

---

### 🔢 BIT-05: Order-preserving digests for variable-length key search

**Goal:** In a binary search over sorted variable-length byte keys, decide most probes without touching the key bytes. Pack the first 8 bytes of each key big-endian into a `ulong` - for byte-lexicographic ordering that packing **preserves order**, so comparing two digests as integers gives the same answer as comparing the keys, whenever the digests differ.

**Effect:**

- A probe becomes one `ulong` load and one integer compare instead of a `SequenceCompareTo` call plus a pointer chase into the key array
- Only when two digests are equal does the search fall back to a full byte comparison
- The gain grows with table size, because a deeper search means more probes that never touch key bytes - and misses gain the most, since a miss walks the full depth and never needs the bytes at all

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Big-endian packing with zero padding preserves byte-lexicographic order
private static ulong GetDigest(ReadOnlySpan<byte> key)
{
    Span<byte> buffer = stackalloc byte[8];
    buffer.Clear();
    key[..Math.Min(key.Length, 8)].CopyTo(buffer);
    return BinaryPrimitives.ReadUInt64BigEndian(buffer);
}

// Built once alongside the sorted key array
var mid = (lo + hi) >>> 1;
var digest = digests[mid];
int compared;
if (digest != probeDigest)
{
    compared = digest < probeDigest ? 1 : -1;   // decided without reading key bytes
}
else
{
    compared = probe.SequenceCompareTo(keys[mid]);   // fall back only on a tie
}
```

**Use cases:** Storage-engine index blocks - binary search over a sorted run of variable-length keys, where the structure has to stay ordered because range scans, prefix scans and successor lookups go through it too.

**Measured (net10 / x86-64-v4, 64 / 256 / 1024 keys):** With keys that differ inside the first 8 bytes, hit is **0.74x / 0.68x / 0.65x** and miss is **0.64x / 0.57x / 0.54x**. With every key sharing an 8-byte prefix, the sign reverses: hit **1.31x / 1.27x / 1.30x**, miss **1.36x / 1.26x / 1.22x**. → [Results](benchmarks/results/BIT-05-OrderedDigestSearch.md)

**Caveats:**

- **This is conditional, and the worst case is a real regression, not a wash.** If keys share an 8-byte prefix, every digest ties, the fallback runs anyway, and the digest is pure added work at 1.2-1.4x. Identifier-like keys with a common namespace prefix land exactly there - measure the actual prefix distribution before adopting
- If ordering is not required, a hash table beats binary search outright and this pattern has no place
- Costs one `ulong` per key of extra memory, and roughly doubles the code size of the search (the packing inlines into the probe)
- The order-preserving property depends on **unsigned** big-endian packing. Little-endian, or a signed comparison, breaks it silently

---

## 🧮 VEC: SIMD and vectorization

### 🧮 VEC-01: Explicit SIMD (Vector\<T\> / Vector256)

**Goal:** Run data-parallel work — aggregation, transformation, search — in bulk with hardware SIMD instructions.

**Effect (measured, net10 / x86-64-v4 (AVX-512), sum of int[4096]):**

| Implementation | Ratio |
|---|---|
| Scalar loop | 1.00 (826ns) |
| `Enumerable.Sum` (BCL, already vectorized) | 0.31 |
| `Vector256` directly | 0.22 |
| **`Vector<T>` (width-agnostic)** | **0.14 (7.0x)** |

Explicit SIMD beats the scalar loop by a wide margin on any SIMD-capable CPU — that policy does not depend on hardware. **Which form wins does:** `Vector<T>` follows the hardware width (16 int lanes on AVX-512) while `Vector256` is pinned at 8 lanes, so the width-agnostic form wins here; on AVX2-only CPUs both run 8 wide and can come out even. **Default to width-agnostic `Vector<T>`** and hardcode a width only when the algorithm needs specific lane semantics.

**AOT:** ✅ No issues (AOT also emits SIMD instructions for the target ISA. Provide an `IsHardwareAccelerated` guard plus a scalar fallback)

**Example:**

```csharp
// Width-agnostic: Vector<int>.Count follows the hardware (8 lanes on AVX2, 16 on AVX-512)
var span = values.AsSpan();
var acc = Vector<int>.Zero;
var i = 0;
for (; i <= span.Length - Vector<int>.Count; i += Vector<int>.Count)
{
    acc += new Vector<int>(span.Slice(i, Vector<int>.Count));
}

var total = Vector.Sum(acc);
for (; i < span.Length; i++)
{
    total += span[i]; // handle the remainder with scalar code
}
```

**Design guidance (important):** First look for a vectorized BCL API (`Enumerable.Sum`, `IndexOf`/`SequenceEqual`, `SearchValues`, `Ascii`, `TensorPrimitives`) — over 4x versus scalar without writing anything. Reserve hand-written SIMD for work with no matching BCL API, and write it width-agnostic with `Vector<T>`; drop to `Vector128/256` only when the algorithm needs specific lane semantics (fixed-width shuffles and the like).

**Use cases:** Checksums and aggregation, custom encode/decode, bulk numeric conversion.

**Caveats:** Always have tests for the remainder path and the unsupported-CPU fallback (cross-check every path against the scalar implementation in Verify).

---

### 🧮 VEC-02: Fixed-width intrinsics (byte shuffle)

**Goal:** Perform a lane permutation that `Vector<T>` cannot express, using a `Vector128` shuffle. This is **the place VEC-01 points to** when it says to drop to a fixed width only when the algorithm requires a specific lane arrangement.

**Effect:**

- **0.46x** against a scalar loop for `uint` endianness reversal (a pure lane permutation)
- Even without a shuffle, the `Vector<T>` arithmetic form (building the permutation from shifts and masks) reaches 0.50x — but its code is 1.75x larger (333 vs 190 B)

**AOT:** ✅ No issues (always provide the `IsSupported` / `IsHardwareAccelerated` guard and a scalar fallback)

**Example:**

```csharp
// Swapping byte positions cannot be written with Vector<T>. Use Vector128.Shuffle with a constant mask
private static readonly Vector128<byte> ReverseMask = Vector128.Create(
    (byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12);

public static void ReverseEndianness(ReadOnlySpan<uint> source, Span<uint> destination)
{
    var i = 0;
    if (Vector128.IsHardwareAccelerated)
    {
        ref var srcHead = ref MemoryMarshal.GetReference(source);
        ref var dstHead = ref MemoryMarshal.GetReference(destination);
        var lanes = Vector128<uint>.Count;
        for (; i <= source.Length - lanes; i += lanes)
        {
            var loaded = Vector128.LoadUnsafe(ref srcHead, (nuint)i);
            Vector128.Shuffle(loaded.AsByte(), ReverseMask).AsUInt32().StoreUnsafe(ref dstHead, (nuint)i);
        }
    }

    for (; i < source.Length; i++)   // scalar tail
    {
        destination[i] = BinaryPrimitives.ReverseEndianness(source[i]);
    }
}
```

**Use cases:** Endianness conversion, nibble expansion (hex / Base-N encoders), byte-level table lookup, fixed permutation patterns.

**Measured (net10 / x86-64-v4, endianness reversal of 1,021 `uint` values):**

| Implementation | Time | Ratio | Code size |
|---|---:|---|---:|
| Scalar (`BinaryPrimitives`) (baseline) | 213.05 ns | 1.00 | 145 B |
| **`Vector128.Shuffle` (portable)** | **62.51 ns** | **0.29** | 190 B |
| `Ssse3.Shuffle` (raw ISA intrinsic) | 61.06 ns | 0.29 | 190 B |
| `Vector<T>` arithmetic form (no shuffle) | 93.88 ns | 0.44 | 321 B |

→ [Measurement](benchmarks/results/VEC-02-VectorShuffle.md)

**⚠️ There is no performance reason to drop to raw ISA intrinsics — and the x86-64-v3 figure that suggested otherwise was code placement:**

The two forms produce **byte-identical disassembly** (59 instructions / 190 B each, the same `vpshufb`) on both machines. On x86-64-v3 they measured 121.53 vs 64.35 ns — a reproducible **1.76x** — because the portable form's hot loop straddled a 64-byte instruction-fetch boundary (`9FBC`-`9FD8` across `9FC0`) while the ISA form's fitted inside one (`9F5C`-`9F78`); byte-identical duplicate methods each reproduced their original's address and time, and swapping the declaration order did not move the placement.

| | x86-64-v3 | x86-64-v4 run 1 | x86-64-v4 run 2 |
|---|---:|---:|---:|
| `Vector128.Shuffle` | 121.53 ns | 62.51 ns | 64.69 ns |
| `Ssse3.Shuffle` | 64.35 ns | 61.06 ns | 62.21 ns |
| Gap | **1.76x, CIs disjoint** | 1.02x, CIs disjoint | **1.04x, CIs overlap** |

**On this machine the portable form lands well and the gap collapses to 1-4%, which the second run cannot even resolve** — the 121.53 ns was the anomaly, not the 64.35 ns. So **default to the portable `Vector128.Shuffle`**, and treat any large gap between byte-identical code as a placement finding to re-measure rather than a result to quote (pitfall 10).

**Caveats:**

- Look for a vectorized BCL API first (VEC-01's design guidance). This pattern is only for "no BCL API exists, and `Vector<T>` cannot express it either"
- Always test the tail and the unsupported-CPU fallback. Making the element count **not** a multiple of the vector width keeps the tail path live on every run
- `Vector128.Shuffle` normalizes indices (out of range gives 0). With a constant mask the JIT folds it, so that safety costs nothing

---

## 📜 SEQ: Sequential I/O and sequence processing

### 📜 SEQ-01: SpanTokenizer\<T\>

**Goal:** A general-purpose zero-allocation tokenizer that splits a span of any `IEquatable<T>` type on a delimiter element.

**Effect:**

- Unlike `string.Split` it produces no array
- The same code works for non-char types (int, byte, and so on)
- Natural syntax through `foreach` duck typing

**AOT:** ✅ No issues

**Example:**

```csharp
foreach (var token in new SpanTokenizer<char>(line.AsSpan(), ','))
{
    // ReadOnlySpan<char> — zero allocation
}
```

**Use cases:** CSV parsing, protocol header splitting, command argument parsing.

**Related / positioning vs the BCL:** A combined application of STK-01 (ref struct) + STK-03 (struct iterator) + JIT-02 (IEquatable constraint). **On .NET 9+, `MemoryExtensions.Split` provides functionally equivalent zero-allocation splitting — if you target .NET 9+ only, use the BCL API.** What keeps this implementation in the catalog: it works on .NET 8 and earlier targets (multi-targeting libraries), and it measures 4-13% faster with smaller code (707 B vs 910 B) — a real but small edge. Treat it as a compatibility/reference pattern, not something the BCL lacks.

**Implementation in this repo:** [SpanTokenizer.cs](src/PerformancePatterns/Seq/SpanTokenizer.cs) / [Tests](tests/PerformancePatterns.Tests/Seq/SpanTokenizerTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Seq/SpanTokenizerBenchmark.cs) / [Results](benchmarks/results/SEQ-01-SpanTokenizer.md)

**Measured (net10 / x86-64-v4):** Versus `string.Split`, 0.47x with 4 tokens — but **1.15x (slower) with 64 tokens**, where string.Split's vectorized scan wins; allocation goes from 216B-3,096B → **0B** in every case. Against `MemoryExtensions.Split` on .NET 9+ it is 4-13% faster with a smaller code size (707B vs 910B). Choose it for allocation elimination and short token counts, not as a blanket speed win.

---

### 📜 SEQ-02: Struct I/O over Stream

**Goal:** Read and write unmanaged structs directly against a `Stream`, eliminating intermediate buffers and serializers.

**Effect:**

- The struct's memory layout is used as the byte sequence as-is, so conversion cost is zero
- Reading or writing a header or fixed-length record completes in a single I/O call

**AOT:** ✅ No issues

**Example:**

```csharp
public static T Read<T>(this Stream stream) where T : unmanaged
{
    Unsafe.SkipInit(out T value);
    var span = MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), Unsafe.SizeOf<T>());
    stream.ReadExactly(span);
    return value;
}

public static void Write<T>(this Stream stream, in T value) where T : unmanaged
{
    var span = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in value)), Unsafe.SizeOf<T>());
    stream.Write(span);
}
```

**Use cases:** Binary file formats, fixed-length record I/O, custom protocols.

**Measured (net10 / x86-64-v4, 16 bytes x 1024 records):**

| Approach | Time | Ratio |
|---|---:|---|
| Write: `BinaryWriter` field by field | 10,199 ns | 1.00 |
| **Write: bulk `MemoryMarshal.AsBytes`** | **154.0 ns** | **0.015 (about 66x)** |
| Read: `BinaryReader` field by field | 4,609 ns | 1.00 |
| **Read: `ReadExactly` + bulk reinterpretation** | **93.8 ns** | **0.020 (about 49x)** |

**The largest improvement in this catalog.** Field-by-field I/O passes through buffer bounds checks and formatting on every call, whereas bulk reinterpretation is a single memcpy. → [Results](benchmarks/results/SEQ-02-StructStreamIo.md)

**Caveats:** The memory layout becomes the external format verbatim, so pin it with `[StructLayout(LayoutKind.Sequential, Pack = 1)]` or similar and make endianness and padding explicit design decisions. When compatibility across environments is needed, use explicit conversion via `BinaryPrimitives`.

**Note (`Unsafe.As` vs `Unsafe.BitCast`):** the example above views `T` as a byte sequence — a reinterpretation between different sizes — so `Unsafe.As<T, byte>` is the correct tool. **When you are only swapping between same-sized value types, make `Unsafe.BitCast` the default** instead: the generated code is identical and it rejects a size mismatch. → [Measurement](benchmarks/results/LAB-BitCast.md)

---

### 📜 SEQ-03: Lazy sequence processing (Batch / Segment / Traverse)

**Goal:** Chunk sequences and traverse hierarchies lazily and with low allocation, without materializing the whole sequence.

**Effect:**

- Avoids turning the entire input into an array or list, keeping the working set constant
- Combined with STK-04 (static local method iterators), closure allocation is zero as well

**AOT:** ✅ No issues

**Example:**

```csharp
// Process 1000 items at a time (without listing the whole source)
foreach (var chunk in source.Batch(1000))
{
    BulkInsert(chunk);
}

// Tree traversal (no recursion stack, no intermediate collections)
foreach (var node in root.TraverseDepthFirst(static x => x.Children))
{
    Visit(node);
}
```

**Use cases:** Bulk processing, paging, tree and graph traversal.

**Implementation in this repo:** [BatchExtensions.cs](src/PerformancePatterns/Seq/BatchExtensions.cs) (Span version = ref struct enumerator that only slices; array version = returns ArraySegment) / [Tests](tests/PerformancePatterns.Tests/Seq/BatchTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Seq/BatchBenchmark.cs) / [Results](benchmarks/results/SEQ-03-Batch.md)

**Measured (net10 / x86-64-v4, 1024 elements in groups of 100):**

| Approach | Time | Ratio | Allocation | Code size |
|---|---:|---|---:|---:|
| `Enumerable.Chunk` (baseline) | 359 ns | 1.00 | 4,424 B | 1,769 B |
| **Array Batch (ArraySegment)** | 266 ns | 0.74 | **0 B** | 141 B |
| **Span Batch (slice)** | **227 ns** | **0.63** | **0 B** | 108 B |

`Chunk` allocates a new array per chunk and copies into it. Batch just returns a view (slice / ArraySegment), so neither allocation nor copying happens.

**Related:** Prefer the standard APIs such as `Enumerable.Chunk` / `Index` on .NET 9+ when they suffice, and switch to a view-returning implementation like this one on high-frequency paths where allocation becomes a problem.

---

### 📜 SEQ-04: Ring buffer with incremental delimiter search

**Goal:** Minimize rescanning and copying when splitting records out of a streaming source (serial port, socket).

**Effect:**

- Keeps the search start position so **regions already scanned are never rescanned** (on a miss it advances to `count - delimiter.Length + 1`)
- When there is no wraparound, the contiguous region can be handed to the callback as-is (**fully zero-copy**); a joining copy is needed only when it wraps
- Splitting the ring into two contiguous segments lets the vectorized `IndexOf` do the work, with manual matching only for candidate positions that straddle the boundary

**AOT:** ✅ No issues

**Example (skeleton):**

```csharp
// Accumulate received data into a ring rented from ArrayPool and signal whenever a line is complete
private int head;      // start of the valid data
private int count;     // length of the valid data
private int search;    // relative position where the next scan resumes

private bool TryReadLine(out ReadOnlySpan<byte> line)
{
    var index = IndexOfDelimiter(search);
    if (index < 0)
    {
        // do not rescan what was already searched
        search = Math.Max(0, count - Delimiter.Length + 1);
        line = default;
        return false;
    }

    line = SliceWithoutCopyIfContiguous(index);
    search = 0;
    return true;
}
```

**Use cases:** Line splitting over serial links, TCP framing, log tailing.

**Related:** ASY-07 (System.IO.Pipelines) is the more full-featured option. Choose a hand-rolled ring when you want no extra dependency, a fixed size, and your own overflow discard policy.

**Measured (net10 / x86-64-v4, 16 lines of 2 KB received in 256 B chunks):** Rescanning the whole buffer every time plus compacting after every line takes 1.70 μs, against **1.13 μs (0.67x) for incremental search plus deferred compaction**. Both are zero-allocation; the difference comes from not rescanning already-scanned bytes and from moving data only when necessary instead of per line (the two-segment wraparound path was not measured; the figures come from a flat buffer). → [Results](benchmarks/results/SEQ-04-RingSplit.md)

**Caveats:** Decide the buffer-overflow policy explicitly (drop old data / throw / grow).

---

### 📜 SEQ-05: Reading an unknown-length stream without a grow-copy chain

**Goal:** Stop the "accumulate into a `MemoryStream`, then `ToArray`" shape. That form pays a reallocate-and-copy every time the buffer doubles, then one more full copy at the end, and lands every buffer past 85 KB on the LOH. Read into pooled chunks and expose them as a `ReadOnlySequence<byte>` instead.

**Effect:**

- One copy out of the source instead of one copy plus the grow-copy chain plus a final `ToArray`
- No LOH traffic regardless of payload size, because no single buffer ever exceeds the chunk size
- The consumer works against `ReadOnlySequence<byte>`, which most modern parsing APIs already accept (`Utf8JsonReader`, `SequenceReader<T>`)

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ Grow-copy chain plus a final full copy, and the result is on the LOH
using var destination = new MemoryStream();
source.CopyTo(destination);
var array = destination.ToArray();

// ✅ PipeReader hands you a ReadOnlySequence over its own pooled buffers
var reader = PipeReader.Create(source, new StreamPipeReaderOptions(bufferSize: 32 * 1024));
while (true)
{
    var result = await reader.ReadAsync().ConfigureAwait(false);
    Process(result.Buffer);
    reader.AdvanceTo(result.Buffer.End);
    if (result.IsCompleted)
    {
        break;
    }
}

await reader.CompleteAsync().ConfigureAwait(false);
```

**Use cases:** Pulling an unknown-length payload out of a stream - a request body, a blob read, a file of unknown size.

**Measured (net10 / x86-64-v4, 256 KB):** `MemoryStream` accumulate + `ToArray` costs 167.3 μs and **524,520 B**, with Gen0/Gen1/Gen2 all at 166.5 collections (the large arrays survive to Gen2). `PipeReader` is **57.4 μs (0.34x) / 504 B**, and a hand-rolled pooled-chunk `ReadOnlySequence` builder is 56.2 μs (0.34x) / 64 B. → [Results](benchmarks/results/SEQ-05-StreamSequence.md)

**Caveats:**

- **`PipeReader` is the answer here, not a hand-rolled builder.** The custom builder measured 0.98x against `PipeReader` - a real difference, and a 2% one, against ~90 lines of segment pooling you then own. Reach for your own only when the sequence must outlive the read loop, or when the source is not a `Stream`
- All variants still copy the payload once, out of the source into the read buffer. What is removed is the *extra* copies, not the first one
- `AdvanceTo` must be called for every `ReadAsync`, and the buffer is invalid afterwards. Anything you need to keep must be copied out before advancing
- See [ASY-03](#-asy-03-systemiopipelines) for the Pipelines mechanics and the 64 KB deadlock caveat

---

## 🗃️ COL: Collection optimization

### 🗃️ COL-01: Direct internal access with CollectionsMarshal

**Goal:** Bypass the public APIs of `List<T>` / `Dictionary<TKey, TValue>` and take a Span or ref straight to the internal storage.

**Effect (measured):**

- `CollectionsMarshal.AsSpan(list)` makes List iteration about 2x faster (0.52 measured locally on net10, for both for and foreach). Plain foreach / for over `List<T>` are equally fast and the slowest
- `GetValueRefOrAddDefault` makes a dictionary read-modify-write (equivalent to `map[key]++`) 0.66x (locally on net10; hashing and probing go from twice to once)
- Bulk construction with `SetCount` (.NET 8+) plus Span writes is 0.22-0.26x versus an Add loop (about 4x). That is another 2x faster than Add with a preset capacity (0.47-0.60x), and removing growth reallocation halves allocation as well

**AOT:** ✅ No issues

**Example:**

```csharp
// Iterating a List as a Span
foreach (ref var item in CollectionsMarshal.AsSpan(list))
{
    item.Value++;
}

// Dictionary counter increment: read and write with a single probe
ref var count = ref CollectionsMarshal.GetValueRefOrAddDefault(map, key, out _);
count++;

// Bulk construction with a known size: fix Count first, then write through a Span instead of Add (.NET 8+)
var list = new List<int>();
CollectionsMarshal.SetCount(list, size);
var span = CollectionsMarshal.AsSpan(list);
for (var i = 0; i < span.Length; i++)
{
    span[i] = Compute(i);
}
```

**Use cases:** Aggregation, cache hit counters, bulk updates of internal models.

**Implementation in this repo:** [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Lab/CollectionsMarshalBenchmark.cs) ([SetCount](benchmarks/PerformancePatterns.Benchmarks/Lab/ListSetCountBenchmark.cs)) / [Results](benchmarks/results/COL-01-CollectionsMarshal.md)

**Caveats:**

- Do not Add to the List while holding an `AsSpan` result (the internal array is swapped out and the Span points at the old one)
- The benefit lies in fusing the read and the write. For add-only there is no difference from `TryAdd`, and inserting a `GetValueRefOrNullRef` + `Unsafe.IsNullRef` existence check erodes the gain
- Memory grown by `SetCount` is not guaranteed to be zero-initialized for value types (old values can show through). Use it only where the whole region is written before it is read

---

### 🗃️ COL-02: Conditional adoption of FrozenDictionary

**Goal:** Turn dictionaries that never change after construction into a `FrozenDictionary` to speed up lookups.

**Effect and adoption conditions (measured):**

- Lookups are 2-4x faster than `Dictionary` (1024 entries). But **construction is 15-20x slower** and allocates far more — only for tables built once at startup and read forever
- For some key sets the lookup advantage reverses as well (measured: 1.15-1.31x slower than Dictionary for 64 enum names). Measure with real data before adopting

**Measured (net10 / x86-64-v4, string keys, non-interned probes):**

| Aspect | 16 entries | 256 entries | 1024 entries |
|---|---|---|---|
| Construction (Frozen / Dictionary) | **10.0x** (817 vs 82 ns) | **8.5x** (11,091 vs 1,309 ns) | **5.3x** (32.5 vs 6.2 μs) |
| Construction allocation | 4.25x | 4.25x | 3.95x (122 KB) |
| Lookup (Frozen / Dictionary) | 1.07 | 0.97 (➖ measurement noise) | **1.19 (❌ slower, real)** |

**For string keys there is no measurable lookup gain at any size — and at 1024 entries Frozen lookup is measurably slower.** Scaling up does not rescue the trade-off; the 5-10x construction cost is never amortized. Adopt it only when a lookup win is measurable on real data. For name resolution over a known key set, COL-04 (sampled hash table, 0.60-0.62x versus Dictionary) is the surer bet. The general record on the rejection side is R-08. → [Results](benchmarks/results/COL-02-FrozenCondition.md)
- For dictionaries keyed by `Type`, a purpose-built implementation (TYP-01 style type slots, or an open-addressed type hash map) is about 3x faster than FrozenDictionary
- A `ReadOnlyDictionary` wrapper is reliably slower by the cost of the wrapper (to express immutability, expose `FrozenDictionary` or `IReadOnlyDictionary` instead)

**AOT:** ✅ No issues

**Use cases:** Configuration tables, keyword dictionaries, static mappings.

---

### 🗃️ COL-03: Span-key lookups with GetAlternateLookup

**Goal:** Look up a `Dictionary<string, TValue>` with a `ReadOnlySpan<char>` directly, eliminating the `ToString()` allocation for the key (.NET 9+).

**Effect (measured):** Calling `span.ToString()` before the lookup is 2.4-3.1x slower and always allocates. AlternateLookup is nearly as fast as a string-key lookup (1.05-1.21x) with zero allocation.

**AOT:** ✅ No issues

**Example:**

```csharp
private readonly Dictionary<string, int> map = new(StringComparer.Ordinal);
private readonly Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup;

public Resolver()
{
    lookup = map.GetAlternateLookup<ReadOnlySpan<char>>();
}

public bool TryResolve(ReadOnlySpan<char> name, out int value)
    => lookup.TryGetValue(name, out value);
```

**Use cases:** Keyword resolution in parsers, protocol header resolution, every name-lookup API that takes a `ReadOnlySpan<char>`.

**Measured (net10 / x86-64-v4, included in the COL-04 measurements):** A span-key AlternateLookup costs about the same as a direct string-key lookup (1.0x at 4 / 16 / 32 entries). Its value is **being able to look up without the `ToString()` allocation (plus copy) when a span is all you have**; for a known key set the sampled hash table (COL-04) is faster still, at 0.59-0.75x of AlternateLookup. → [Results](benchmarks/results/COL-04-SampledNameTable.md)

**Caveats:** The comparer must implement `IAlternateEqualityComparer` (the default string comparer and `StringComparer.Ordinal(IgnoreCase)` already do). The same API exists on `FrozenDictionary` and `HashSet`.

---

### 🗃️ COL-04: Choosing a lookup strategy for small element counts

**Goal:** Select between a dictionary, linear search, a branch chain, and a hash switch according to element count, key characteristics, and access pattern.

**Findings (measured):**

- Up to about 8 entries: a chain of `string.Equals` ifs is among the fastest (0.17x versus `Enum.TryParse` for enum name resolution). At small sizes even a linear array scan beats a dictionary
- From roughly a dozen entries: the sampled-hash switch (BIT-01) is consistently fast. An Equals chain is fast when access follows declaration order but degrades 3-5x for reverse or partial access — choose for stability across access shapes, not for the average
- FrozenDictionary rarely comes out fastest at this scale (up to ~32 entries); it was never the fastest at any column count in the measurements

**AOT:** ✅ No issues

**Use cases:** Name-to-index resolution emitted by a Source Generator (DB columns, property names, enum names), protocol header dispatch.

**Design guidance:** In generated code the element count is known at generation time, so ideally emit an Equals chain (small) or a hash switch (medium and up) depending on the count.

**If the key set is fixed at compile time, write a plain `switch` first** - up to 64 entries it beats every implementation in this table → [TXT-10](#-txt-10-aggregating-string-matching-into-a-switch). Use this pattern when there are **more than 64 entries, the set is only known at runtime, or code size is the constraint**.

**Implementation in this repo:** [SampledNameTable.cs](src/PerformancePatterns/Col/SampledNameTable.cs) (BIT-01 hash + BIT-02 mask + Ordinal confirmation inside the bucket) / [Tests](tests/PerformancePatterns.Tests/Col/SampledNameTableTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Col/SampledNameTableBenchmark.cs) / [Results](benchmarks/results/COL-04-SampledNameTable.md)

**Measured (net10 / x86-64-v4, name resolution by element count):**

| Implementation | 4 entries | 16 entries | 32 entries |
|---|---|---|---|
| `Dictionary` (string key, baseline) | 1.00 | 1.00 | 1.00 |
| Linear search | 0.62 | 1.77 (❌) | 3.23 (❌) |
| Sampled hash table | **0.60** | **0.62** | **0.75** |
| `Dictionary` AlternateLookup (span-key baseline) | 1.00 | 1.00 | 1.00 |
| `FrozenDictionary` AlternateLookup | 1.03 | 0.90 | 0.89 |
| **Sampled hash table (span key)** | **0.59** | **0.60** | **0.75** |

The sampled hash table is consistently fast at every size, and on span-key comparisons it beats FrozenDictionary too. Linear search matches it at 4 entries but degrades quickly beyond that. Its code size is smaller as well, 692-706 B against Dictionary (about 1.1KB) and Frozen (about 2.1KB).

---

### 🗃️ COL-05: Concrete-type dispatch for IEnumerable parameters

**Goal:** Inside APIs that take `IEnumerable<T>`, check the runtime type and route `List<T>` (and `T[]` where useful) onto a Span path — the standard trick used inside LINQ.

**Effect (measured, net10 / x86-64-v4, sum of 1024 elements) — the applicable range is narrow; adopt only where it fits:**

- **Where it pays: `List<T>`-dominant inputs (0.83x via the CollectionsMarshal.AsSpan branch), and AOT builds** (no dynamic PGO, so both the List and the array branch do work the runtime cannot do for you)
- Everywhere else it does not: array sources gain nothing under the JIT (213.8 vs 209.8ns — guarded devirtualization already specializes them), and **lazy-iterator sources pay 1.13x** (486.7 vs 552.0ns, non-overlapping CIs) for type tests they never use
- Rule of thumb: apply on hot APIs whose callers you know pass Lists (or when targeting AOT); do not sprinkle it on general-purpose IEnumerable parameters

**AOT:** ✅ No issues. AOT has no runtime-profile-driven devirtualization, so **it is worth more than under the JIT, array branch included**

**Example:**

```csharp
public static int Sum(IEnumerable<int> source)
{
    if (source is int[] array)          // no gain under the net10 JIT, but harmless as AOT insurance
    {
        return SumSpan(array);
    }

    if (source is List<int> list)       // still about 1.8x on net10
    {
        return SumSpan(CollectionsMarshal.AsSpan(list));
    }

    var total = 0;
    foreach (var value in source)       // fallback (the type-check penalty measures as zero)
    {
        total += value;
    }

    return total;
}
```

**Use cases:** Collection utilities, input acceptance in serializers and mappers, LINQ-style operators.

**Related:** The same branching idea applies to pre-sizing via `TryGetNonEnumeratedCount` (.NET 6+) (get the count without enumerating → feed `new List<T>(count)` / COL-01 SetCount).

**Implementation in this repo:** [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Lab/EnumerableDispatchBenchmark.cs) / [Results](benchmarks/results/COL-05-EnumerableDispatch.md)

---

### 🗃️ COL-06: Shape-specialized collection conversion

**Goal:** In collection conversion (map / copy), match how the destination is allocated and copied to the shape of the input and the type of the output.

**Effect:**

- When the count is known, pre-size the destination to remove growth reallocation (`TryGetNonEnumeratedCount` / `ICollection<T>.Count`)
- For `ImmutableArray` with a known count, `CreateBuilder(count)` + **`MoveToImmutable()` skips the final copy** (`ToImmutable()` copies)
- When refilling an existing instance, `EnsureCapacity` + `Clear()` **reuses the already-allocated capacity** (`Clear` keeps the capacity, so every round after the first allocates nothing)
- When the concrete type (`List<T>` / `HashSet<T>`) is known, call it as the concrete type rather than through an interface (devirtualization and inlining kick in)

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Known count → reserve capacity + SetCount + direct Span writes (COL-01)
var list = new List<TDestination>(count);
CollectionsMarshal.SetCount(list, count);
var destination = CollectionsMarshal.AsSpan(list);
for (var i = 0; i < source.Length; i++)
{
    destination[i] = Convert(source[i]);
}

// ✅ For ImmutableArray with a known count, MoveToImmutable skips the copy
var builder = ImmutableArray.CreateBuilder<T>(count);
// ... Add every element ...
var immutable = builder.MoveToImmutable();

// ✅ Reuse an existing collection (refill while keeping the capacity)
existing.Clear();
existing.EnsureCapacity(count);
```

**Use cases:** Object mappers, DTO conversion, the collection-conversion part of generated code.

**Related:** COL-05 (concrete-type dispatch) for detecting the input shape, COL-01 (SetCount + AsSpan) for writing elements.

**Measured (net10 / x86-64-v4):**

ImmutableArray construction (16 elements):

| Approach | Time | Allocation |
|---|---:|---:|
| **`ToImmutableArray()` from an array** | **4.0 ns** | 88 B |
| Builder + `ToImmutable()` | 14.3 ns | 176 B |
| Builder + `MoveToImmutable()` | 11.3 ns | 88 B |

**When a contiguous region (array/Span) already exists, the bulk copy of `ToImmutableArray()` wins outright** (the Builder's per-element Add is the bottleneck). The Builder is the tool for when elements arrive one at a time, and in that case `MoveToImmutable` halves the allocation (176 B → 88 B).

List refill (16 elements / 256 elements):

| Approach | 16 elements | 256 elements | Allocation |
|---|---|---|---|
| `new List()` (no capacity, baseline) | 1.00 | 1.00 | 216 B / 2,232 B |
| `new List(capacity)` | 0.51 | 0.81 | about half |
| Reuse (Clear + EnsureCapacity) | 0.63 | 0.70 | **0 B** |
| **Reuse + SetCount + direct Span writes (COL-01)** | **0.19** | **0.26** | **0 B** |

→ [Results](benchmarks/results/COL-06-CollectionConvert.md)

**Caveats:** `MoveToImmutable` **requires Count and Capacity to match exactly** (it throws if short or over). Use `ToImmutable()` when the count is not fixed.

---

### 🗃️ COL-07: Existence-checked ref lookup with GetValueRefOrNullRef

**Goal:** Do "update if present, otherwise nothing" in a **single** hash probe. This is the **non-inserting** counterpart to COL-01's `GetValueRefOrAddDefault`.

**Effect:**

- The update path runs at **0.44-0.59x** against `TryGetValue` + indexer write-back (two probes)
- **Code size drops from 8,270 to 1,080 B.** The indexer setter drags the whole insert path (`TryInsert` / `Resize` / hash helpers) into the caller; the ref-returning form needs none of it
- The gain is **the single probe**, not the avoided value copy — the ratio is the same for an 8-byte and a 32-byte value

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ Two probes, and the indexer setter inlines the whole insert path
if (map.TryGetValue(key, out var value))
{
    map[key] = value + 1;
}

// ✅ One probe. Take a ref to the existing slot and update in place
ref var slot = ref CollectionsMarshal.GetValueRefOrNullRef(map, key);
if (!Unsafe.IsNullRef(ref slot))
{
    slot++;
}
```

**Use cases:** Hit counters and statistics, bulk updates restricted to existing entries, refreshing cache timestamps.

**Measured (net10 / x86-64-v4, 256 probes into a 1,024-entry dictionary):**

| Probe | Form | Time | vs the two-probe form | Code size |
|---|---|---:|---|---:|
| All hit | `TryGetValue` + indexer update | 3.769 μs | 1.00 | **8,351 B** |
| All hit | **`GetValueRefOrNullRef` update** | **1.654 μs** | **0.44** | **1,155 B** |
| Half miss | `TryGetValue` + indexer update | 2.286 μs | 1.00 | 8,124 B |
| Half miss | **`GetValueRefOrNullRef` update** | **1.347 μs** | **0.59** | 967 B |
| 32-byte value | `TryGetValue` + indexer update | 3.552 μs | 1.00 | 3,364 B |
| 32-byte value | **`GetValueRefOrNullRef` update** | **1.809 μs** | **0.51** | 1,159 B |

**On the read path the gain is one folded memory operand per field the loop reads out of the slot** — the ref form reads the slot as `add rsi,[r13]` where `TryGetValue` emits a `mov` and an `add`. Reading a single field is therefore a tie on both machines (0.99-1.04x, overlapping CIs, measured twice), while reading two fields out of a 32-byte value resolves at **0.93-0.94x on x86-64-v4** (disjoint CIs in two runs) against 1.00x on x86-64-v3. **Count the field reads, not the value's size** — and treat an older core's "no difference" as unresolved rather than settled. → [Measurement](benchmarks/results/COL-07-ValueRefLookup.md)

**Caveats:**

- **For reads alone, keep `TryGetValue`.** The value of this pattern is fusing the read and the write
- Do not modify the dictionary while holding the ref (a resize swaps the internal array and the ref points at the old storage — same constraint as COL-01)
- If insertion is also needed, use `GetValueRefOrAddDefault` (COL-01). Pick the API by whether "missing" means insert or means skip
- The `ContainsKey` + indexer shape is two probes and is rejected at build time by CA1854. It is not a candidate to compare against

---

## 🔤 TXT: Strings and formatting

### 🔤 TXT-01: Formatting and conversion with lookup tables

**Goal:** Replace number-to-string formatting (two-digit decimal, hex, etc.) with copies out of a precomputed table.

**Effect (measured):** For UTF-8 formatting of a DateTime in a fixed format (`yyyyMMddHHmmss`), the two-digit table approach takes about a third of the time of `ToString` + `Encoding.GetBytes` (0.34x). It is about 2x faster than `Utf8Formatter.TryFormat` as well.

**AOT:** ✅ No issues

**Example:**

```csharp
// Static table of the two-digit ASCII forms of 00-99 (byte[100 * 2])
private static readonly byte[] DigitTable = CreateDigitTable();

[MethodImpl(MethodImplOptions.AggressiveInlining)]
private static void Write2(Span<byte> destination, int value)
    => DigitTable.AsSpan(value * 2, 2).CopyTo(destination);

// A u8 literal makes the conversion table reference the assembly data section directly (no array allocation)
private static ReadOnlySpan<byte> HexTable => "0123456789ABCDEF"u8;
```

**Use cases:** Fixed-format date/time and numeric output, hex and Base-family encoders, protocol constant output.

**Notes:** A `static ReadOnlySpan<byte>` property with a u8 literal (or a directly returned `new byte[] {...}`) is turned by the compiler into a direct data-section reference, so make this the default way to define static tables.

**Caveat (do not change the access width):** Before converting an existing table to a u8 literal, check that the **read width stays the same**. Replacing a `ushort[100]` two-digit pair table (high byte = tens, low byte = ones) with a 200-byte u8 table turns **one 2-byte read into two 1-byte reads plus an index doubling**. Measurements show no difference for short numbers, but as the digit count grows the inner loop dominates and it becomes a **1.11x regression** (`FormatInt32` with `Int32.MaxValue`: 7.751 → 8.636 ns).

To drop the `fixed` pinning on a static array while keeping the read width, use **`MemoryMarshal.GetArrayDataReference`** rather than a u8 literal. In the same measurement that gave 4.861 → **4.386 ns (0.90x, non-overlapping confidence intervals)**. Converting to a u8 literal pays off only for tables that were **already read byte by byte**, like `FastDateTimeByteHelper`. → [R-09](docs/rejected-patterns.md)

**Implementation in this repo:** [Utf8DateTimeFormatter.cs](src/PerformancePatterns/Txt/Utf8DateTimeFormatter.cs) / [Tests](tests/PerformancePatterns.Tests/Txt/Utf8DateTimeFormatterTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Txt/Utf8DateTimeFormatterBenchmark.cs) / [Results](benchmarks/results/TXT-01-Utf8DateTimeFormatter.md)

**Measured (net10 / x86-64-v4, yyyyMMddHHmmss):** 0.41x versus `ToString` + `Encoding.GetBytes` (about 2.5x faster), 56B → 0B, code size about 10KB → 0.9KB. `DateTime.TryFormat` + encoding is a modest 0.90x over `ToString`, and the table approach is still ~2.2x faster than it.

---

### 🔤 TXT-02: Stackalloc-first string building

**Goal:** Replace short-lived string assembly with a stackalloc initial buffer plus a pool fallback.

**Effect (measured):** For four concatenations of 32 characters, a `StringBuilder` with no capacity takes 53.4ns, versus 19.7ns with a capacity (2.7x) and 13.2-13.7ns (about 4x) for ValueStringBuilder / a pooled builder / an interpolated string handler with stackalloc.

**AOT:** ✅ No issues

**Example:**

```csharp
// Pass a stackalloc initial buffer to the interpolated string handler
var handler = new DefaultInterpolatedStringHandler(0, 0, null, stackalloc char[128]);
handler.AppendLiteral(name);
handler.AppendFormatted(value);
var result = handler.ToStringAndClear();
```

ValueStringBuilder (a ref struct with a stackalloc initial buffer that grows into ArrayPool) mirrors the BCL's internal implementation and is worth writing yourself as the string-specialized form of BUF-03 / BUF-05.

**Implementation in this repo:** [ValueStringBuilder.cs](src/PerformancePatterns/Txt/ValueStringBuilder.cs) / [Tests](tests/PerformancePatterns.Tests/Txt/ValueStringBuilderTest.cs) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Txt/ValueStringBuilderBenchmark.cs) / [Results](benchmarks/results/TXT-02-ValueStringBuilder.md)

**Measured (net10 / x86-64-v4, four concatenations of 24 characters):** Versus a `StringBuilder` with no capacity, ValueStringBuilder is 0.30x (about 3.4x faster) and allocation drops from 760B → 216B (the result string only). It is on par with the stackalloc interpolated handler and faster still than a capacity-specified `StringBuilder` (0.45x).

**Use cases:** Log messages, key string generation, short assembly of SQL, paths and the like.

**Caveats:**

- At the very least, always specify a capacity when using `StringBuilder` (that alone is 2.7x)
- Split the Grow path out with NoInlining per JIT-04

---

### 🔤 TXT-03: Avoiding exceptions with the Try pattern

**Goal:** Handle operations where failure is part of the normal flow (parsing, conversion, lookup) with a bool return value instead of exceptions.

**Effect (measured):**

- `int.Parse` + try/catch is about 2.5x slower than `TryParse` even on success. On failure it is about 540x (1,222ns vs 2.27ns) plus 464B of allocation
- A single thrown exception costs on the order of microseconds and completely swallows the surrounding optimizations (measured: a 4.6x cache optimization gap vanished entirely on the conversion-failure path)

**AOT:** ✅ No issues

**Design guidance:**

- Make `TryXxx(out T result)` the canonical public API of a library and provide the throwing version (`Xxx`) as a wrapper over the Try version
- Internally too, use the BCL's Try APIs (`int.TryParse`, `Utf8Parser.TryParse`, etc.) and keep try/catch out of control flow

**Measured (net10 / x86-64-v4, integer parsing with 10% invalid input):** Exception-based control flow costs 132.5 ns per call plus 48 B (**one exception ≒ 1.3 μs**), against **2.89 ns / 0 B (0.02x = about 46x) for TryParse**. Code size is 8,348 B vs 1,705 B as well (the EH scaffolding). → [Results](benchmarks/results/TXT-03-TryPattern.md)

---

### 🔤 TXT-04: Matching byte-sequence tokens directly

**Goal:** Match known tokens in received bytes (HTTP methods, protocol keywords, etc.) on the byte sequence itself, without converting to string.

**Effect (measured, net10 / x86-64-v4, 64 matches of a 4-byte token):**

- **0.26x (3.8x faster) versus converting to string plus a switch, with allocation 2,048B → 0B** — the real win is avoiding the string conversion
- A chain of `SequenceEqual("GET "u8)` and comparison against uint constants are **the same speed** (84.1 vs 82.6ns, CIs overlap). SequenceEqual on net10 is already well optimized for constant lengths, so the only benefit of the uint form is smaller code (226B → 166B)

**AOT:** ✅ No issues

**Example:**

```csharp
// Default: SequenceEqual against a u8 literal (readable, safe, fast enough)
if (span.SequenceEqual("GET "u8)) { return HttpMethod.Get; }

// Many branches, or code size matters: compare as integers against constants
// Build constants from u8 literals rather than hand-written hex (the JIT folds static readonly at Tier1)
private static readonly uint GetToken = BinaryPrimitives.ReadUInt32LittleEndian("GET "u8);

var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
if (value == GetToken) { return HttpMethod.Get; }
```

**Use cases:** Method and keyword dispatch in protocol parsers, magic number checks.

**Caveats:** When tokens of lengths other than exactly 4 or 8 bytes are mixed in, test the length first. 5-7 bytes can be handled with a `ulong` read plus a mask, but write it with SequenceEqual and measure before optimizing.

---

### 🔤 TXT-05: Direct UTF-8 formatting with Utf8.TryWrite

**Goal:** Build UTF-8 output by writing straight into a `Span<byte>` with the UTF-8 interpolated string handler (.NET 8+), rather than the two-step string interpolation followed by encoding.

**Effect (measured, net10 / x86-64-v4, formatting `id={int}&name={string}&ts={long}`):**

- **0.45x (2.2x faster) versus string interpolation + `Encoding.UTF8.GetBytes`, with 104B → 0B**
- Faster than char-based `MemoryExtensions.TryWrite` + encoding (0.52x) because no intermediate char representation is needed

**AOT:** ✅ No issues (interpolated handlers are a compile-time transformation)

**Example:**

```csharp
using System.Text.Unicode;

if (Utf8.TryWrite(destination, $"id={id}&name={name}&ts={timestamp}", out var written))
{
    writer.Advance(written);
}
```

**Use cases:** Building HTTP and protocol responses, UTF-8 log output, formatting straight into BUF-02 (IBufferWriter).

**Caveats:** For fixed-format numbers and timestamps alone, TXT-01 (lookup tables) is faster still. Use this as the general-purpose option for variable formats and mixed content.

---

### 🔤 TXT-06: ASCII-specialized comparison

**Goal:** Replace the general Unicode-aware implementation with the ASCII-specialized one (.NET 8's `Ascii` class) for case-insensitive handling of tokens guaranteed to be ASCII (HTTP header names and the like).

**Effect (measured, net10 / x86-64-v4, case-insensitive comparison of 8 header name pairs):**

- `Ascii.EqualsIgnoreCase` (byte sequence against byte sequence) is **0.76x** versus `string.Equals(OrdinalIgnoreCase)`. It compares bytes as they are, so the string conversion itself becomes unnecessary too (the effective gap is larger still)
- A hand-written `| 0x20` normalized comparison is the fastest at 0.59x, but it carries the trap of falsely equating symbol pairs such as `@` and `` ` ``

**AOT:** ✅ No issues

**Example:**

```csharp
// Default: the Ascii class (.NET 8+; mixed byte/char overloads exist too)
if (Ascii.EqualsIgnoreCase(headerName, "content-type"u8)) { ... }

// | 0x20 normalization is acceptable only for a closed token set that differs solely in letter case
```

**Use cases:** Matching HTTP headers and protocol fields, normalization with `Ascii.ToLowerInPlace` and friends, classification with the `char.IsAsciiDigit` family.

**Caveats:** If the input can contain non-ASCII, test with `Ascii.IsValid` first or fall back to the general implementation. `| 0x20` collides on symbols (`@`↔`` ` ``, `[`↔`{`, etc.), so reserve it for comparison against a known set.

---

### 🔤 TXT-07: string.Create / TryFormat / ISpanFormattable

**Goal:** Make string generation a direct write into an already-allocated buffer, so no intermediate strings or arrays are created.

**Effect:**

- `string.Create(length, state, action)` fills the final string buffer directly, so exactly one string is produced (passing state keeps the lambda static → DSP-04)
- `TryFormat` / `ISpanFormattable` write into a `Span<char>` / `Span<byte>` without the intermediate string `ToString()` would create
- Implementing `ISpanFormattable` / `IUtf8SpanFormattable` on your own types lets interpolated handlers (TXT-02) and `Utf8.TryWrite` (TXT-05) format them with no intermediate representation

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Assemble the string with a single allocation (no closure)
var key = string.Create(prefix.Length + 1 + name.Length, (prefix, name), static (span, state) =>
{
    state.prefix.CopyTo(span);
    span[state.prefix.Length] = ':';
    state.name.CopyTo(span[(state.prefix.Length + 1)..]);
});

// ✅ Implement ISpanFormattable on your own types so no intermediate string is created
public readonly struct Measure : ISpanFormattable
{
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        => value.TryFormat(destination, out charsWritten, format, provider);
}
```

**Use cases:** Key generation, ID strings, log messages, value formatting in serializers.

**Related:** For fixed formats the table approach (TXT-01) is faster still. Combine with TXT-02 / TXT-05 for variable formats.

**Measured (net10 / x86-64-v4, assembling prefix:name:id):**

| Approach | Time | Ratio | Allocation |
|---|---:|---|---:|
| String interpolation (baseline) | 17.0 ns | 1.00 | 80 B |
| `string.Concat` + `ToString` | 19.6 ns | 1.15 | 176 B |
| `StringBuilder` (with capacity) | 15.6 ns | 0.92 | 280 B |
| ValueStringBuilder (TXT-02) | 12.5 ns | 0.74 | 80 B |
| **`string.Create`** | **9.4 ns** | **0.55** | **80 B** |

The 80 B is the result string itself (the floor that cannot be reduced further). `string.Create` is the fastest, allocating only the single result. → [Results](benchmarks/results/TXT-07-StringCreate.md)

**Caveats:** `string.Create` requires **knowing the length up front**. When you cannot, use TXT-02 (ValueStringBuilder).

---

### 🔤 TXT-08: SearchValues\<T\>

**Goal:** Delegate "find any one of several candidates" searches to an implementation optimized for the candidate set (SIMD, bitmap, lookup) (.NET 8+).

**Effect:**

- The more candidates, the bigger the win. Far faster than the naive `IndexOfAny(char[])` implementation
- It assumes the result of `SearchValues.Create` is **cached in a static readonly field** (creating it per call defeats the purpose)
- .NET 9 also offers `SearchValues<string>` for sets of strings

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Build the candidate set once in a static readonly field
private static readonly SearchValues<char> Delimiters = SearchValues.Create(",;:\t|");

var index = span.IndexOfAny(Delimiters);
```

**Use cases:** Delimiter detection in tokenizers, detecting characters that need escaping, validation (checking against an allowed character set).

**Measured (net10 / x86-64-v4, scanning 256 characters, versus `IndexOfAny(char[])`):**

| Candidates | Array overload | SearchValues | Ratio |
|---:|---:|---:|---|
| 3 | 5.66 ns | 5.46 ns | 0.96 |
| 8 | 13.9 ns | 4.61 ns | **0.33** |
| 32 | 23.1 ns | 4.54 ns | **0.20** |

SearchValues stays at about 4.5-5.5 ns **regardless of candidate count** (the array version degrades in proportion to it). Its code size is small too, 623 B vs about 3,960 B. → [Results](benchmarks/results/TXT-08-SearchValues.md)

**Caveats:** **For 2-3 candidates the dedicated overloads such as `IndexOfAny(char, char)` are faster** (measured; rejection list R-07). As the table above shows, SearchValues still beats the array overload even at 3 candidates, so use the dedicated overload whenever the count allows one and SearchValues otherwise — never the array overload.

**When only a boolean is needed, use `ContainsAny`:** for checks that do not need the position (validation, deciding whether escaping is required), write `ContainsAny(values)` rather than `IndexOfAny(values) >= 0`. It skips extracting the lane position out of the matching vector.

| Match position (256 chars) | `IndexOfAny(values) >= 0` | `ContainsAny(values)` | Ratio | Code size |
|---|---:|---:|---:|---:|
| Near the head (index 4) | 1.824 ns | **1.328 ns** | **0.73** | 566 → **399 B** |
| Near the tail (index 248) | 7.018 ns | 7.563 ns | 1.08 (within noise) | 547 → **382 B** |
| No match (full scan) | 6.438 ns | 5.826 ns | 0.90 (within noise) | 551 → **394 B** |

**The earlier the data matches, the bigger the win** (an early match gives 0.73x with non-overlapping confidence intervals), and no case gets worse. **Code size is about 30% smaller in every case**, so `ContainsAny` is a fine default even where the time difference stays within noise. → [Results](benchmarks/results/TXT-08-ContainsAny.md)

---

### 🔤 TXT-09: Applied idioms for fixed-length formatting

**Goal:** Keep formatting and trimming of fixed-length fields at minimum cost by leaning on the vectorized BCL APIs.

**Effect:**

- **Left-align numbers with `TryFormat` + `Fill`**: `TryFormat` already optimizes digit counting and two-digit table writes internally. Just `Fill` what is left to get a left-aligned fixed-length field
- **Vectorized trimming**: strip filler from fixed-length fields with `IndexOfAnyExcept` / `LastIndexOfAnyExcept` instead of a hand loop, and extract with a single `Slice`
- **Conversion-free UTF-16 copies**: .NET's internal representation is UTF-16, so UTF-16 fixed-length fields can be handled as a memcpy via `MemoryMarshal.Cast<byte, char>` without going through Encoding (the zero cost of Cast was measured in BIT-04). Padding is a single char-wise `Fill` as well

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Left-aligned fixed-length numbers: TryFormat + Fill (the fastest)
value.TryFormat(buffer, out var written);
buffer[written..].Fill(Filler);

// ✅ Trimming a fixed-length field (use the vectorized APIs)
var start = field.IndexOfAnyExcept(Filler);
var end = field.LastIndexOfAnyExcept(Filler);
var trimmed = start < 0 ? [] : field[start..(end + 1)];
```

**Measured (net10 / x86-64-v4, 8-digit number → 12-character field / 32-character trim):**

| Approach | Time | Ratio |
|---|---:|---|
| **`TryFormat` + `Fill`** | **3.33 ns** | 1.00 |
| Hand-written LSB-first write → Reverse | 10.7 ns | 3.21 (❌) |
| Hand-written right-align → forward shift | 12.2 ns | 3.67 (❌) |
| Trim: hand-written loop | 4.50 ns | 1.00 |
| **Trim: `IndexOfAnyExcept`** | **3.80 ns** | **0.85** |

**The hand-written digit-order tricks (right-align then shift, reverse-order writing) are counterproductive on net10** (rejected as R-16). They belong to the generation before `TryFormat` / `ISpanFormattable` existed; the BCL's digit formatting is optimized now, so they cannot beat it. → [Results](benchmarks/results/TXT-09-FixedFieldFormat.md)

**Use cases:** Fixed-length records (reports, EDI, legacy integration), fixed-width protocol fields, ID formatting.

**Caveats:** Conversion-free UTF-16 copies apply only when the endianness and character-set assumptions can be pinned. Use explicit conversion when compatibility with an external spec is required (the same caveat as SEQ-02).

---

### 🔤 TXT-10: Aggregating string matching into a switch

**Purpose:** Aggregate matching against a compile-time string set into a `switch` and let Roslyn do the length / character bucketing. Make it the default before reaching for a hand-written dispatch.

**Effect:**

- **The window is 5-64 entries.** At 16 keys it is **0.51x hit / 0.33x miss** against an `Equals` chain, and it beats `Dictionary` (1.38x) and `SampledNameTable` (1.09x) on hit
- **At 64 keys it is level with a hand-written sampling-hash switch** (1.06x hit, 1.00x miss, CIs overlap)
- **At 128 keys it collapses** (1.73x hit, **3.79x** miss) because Roslyn switches to an FNV-1a hash over every character plus a 191-node binary search tree
- **At <=4 keys there is no effect** - the compiler emits **the same instruction stream** as a hand-written `Equals` chain (60 instructions, 263 B). On span input the chain is actually faster (1.17x)
- Since C# 11 a `ReadOnlySpan<char>` constant pattern gets the same optimization (about 1.1x the string switch)

**AOT:** ✅ No issue (it emits only branches and jump tables - no runtime codegen, no reflection)

**Example:**

```csharp
// ✅ 5-64 entries: just write the switch (the compiler turns it into length -> character bucketing)
public static int GetIndex(ReadOnlySpan<char> name) => name switch
{
    "Id" => 0,
    "Name" => 1,
    "CreatedAt" => 2,
    _ => -1,
};

// ❌ Above 64 entries a plain switch becomes a full-string FNV scan plus a binary search
//    -> emit a sampling-hash switch (length + 3 characters) instead (GEN-02 / generated-code-patterns scenario 1)
```

**The compiler picks the strategy by keys-per-length-bucket**, not by key count alone:

| Key set | Generated strategy | Scans every character |
|---|---|---|
| 4 | Length + character tests (identical to the hand-written chain) | no |
| 16 | Length + character tests | no |
| 64 | Length jump table -> per-bucket character jump table | no |
| 128 | FNV-1a loop over every character + a 191-node binary search | **yes** |
| 16 x 58-62 chars | Length jump table + first character + AVX-512 compare | no |

**Measured (net10 / x86-64-v4, per probe):**

| Approach | 16 hit | 16 miss | 64 hit | 128 hit | 128 miss |
|---|---:|---:|---:|---:|---:|
| `Equals` chain (baseline at 16) | 3.466 ns | 6.080 ns | — | — | — |
| **Plain switch** | **1.776 (0.51)** | **1.993 (0.33)** | 2.792 (1.06) | 4.580 (1.73) | 10.112 (**3.79**) |
| Sampling-hash switch (baseline at 64 / 128) | — | — | **2.633 (1.00)** | **2.650 (1.00)** | **2.687 (1.00)** |
| `SampledNameTable` (COL-04) | 3.788 (1.09) | 4.908 (0.81) | 3.607 (1.37) | 3.722 (1.41) | 4.300 (1.61) |

**Code size grows with the key set** (1,185 B at 16 keys, 11,611 B at 128). `SampledNameTable` stays flat at 730-780 B, so where binary size or I-cache pressure matters the table can win even when it is 1.1-1.9x slower. → [Results](benchmarks/results/TXT-10-StringSwitchDispatch.md)

**Use cases:** Name-to-index resolution emitted by a Source Generator (DB columns, property names, JSON keys), enum name resolution, protocol header dispatch.

**Where it does not apply - matching that needs a conversion:** the gain above is against a **plain** `Equals` chain, one where the probe is compared exactly as it arrives. **It does not hold once the input has to be converted first.** On the DB column-name shape (`OrdinalIgnoreCase`), upper-casing the probe so a plain switch can be used lost in **all 12 conditions measured** (3 conversion forms x 2 column counts x 2 naming conventions): 2.0-3.0x at 8 columns and 1.35-2.91x at 24.

| Approach (per column, PascalCase) | 8 cols | 24 cols |
|---|---:|---:|
| `Equals(OrdinalIgnoreCase)` chain | **3.24 ns** | - |
| Sampling-hash switch | - | 3.55 ns |
| Plain switch, no case handling | 3.86 ns | **3.24 ns** |
| `Ascii.ToUpper` (SIMD) + span switch | 6.57 ns | 6.43 ns |
| `MemoryExtensions.ToUpperInvariant` + span switch | 7.26 ns | 6.91 ns |
| `string.ToUpperInvariant()` + string switch | 9.67 ns | 10.33 ns (+976 B) |

**Normalization adds 2.7-3.2 ns per column, and the SIMD form only shaves 6-11% off that.** The cost is structural - fold every character into a buffer, then read every character again in the switch - so no API choice avoids it, and it exceeds the switch's own dispatch gain. **When a conversion is required, convert as few characters as possible:** the hash form with upper-cased sampling plus an `OrdinalIgnoreCase` confirm touches 3 characters instead of all of them.

**The headline is not that `OrdinalIgnoreCase` is expensive** - it is the cheapest primitive in the table. What costs is the conversion introduced to avoid needing it. snake_case reproduces every row within a few percent. → [Measurement](benchmarks/results/LAB-ColumnMatch.md) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Lab/ColumnMatchBenchmark.cs)

**The plain switch is not universally the faster match - the crossover is the column count.** Measured like for like (every variant reached through the same per-column call), the chain wins at 8 columns and the switch wins at 24:

| | 8 cols | 24 cols |
|---|---|---|
| Plain ordinal switch vs the generated form | **1.17-1.19x slower** | **0.91x** (PascalCase; snake_case ties at 0.98x) |

A chain compares the probe against the literals in declaration order, so its cost grows with the column count (about 4.5 comparisons on average at 8, 12.5 at 24) while a switch stays flat. That is the same boundary this catalog already draws between a chain and a sampling-hash switch. Where case sensitivity is acceptable at 24+ columns the ordinal switch is the better generated shape - 0.91x with a third less code (2,212 vs 3,261 B).

**Caution:** The switch is **ordinal (case-sensitive)**. **Key length is not a criterion** (no reversal even at 58-62 characters). Expect **around 0.5x against a chain at 16 keys** - a real win, but not an order-of-magnitude one.

**Repository implementation:** No src implementation (the generated code shape itself is the pattern) / [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Lab/StringSwitchDispatchBenchmark.cs) / [Results](benchmarks/results/TXT-10-StringSwitchDispatch.md)

---

### 🔤 TXT-11: Type fast path for object → string conversion

**Purpose:** In a generic converter that turns boxed values into strings, put a type fast path in front of the `IFormattable` interface call so the concrete `ToString` is reached directly.

**Effect:** **Worth it on monomorphic call sites only, and only if the fast path stays small enough to inline.**

| Type fast path | Monomorphic | Mixed | Tier1 size | Inlined |
|---|---:|---:|---:|:---:|
| **Narrow (`int` / `string` + fallback)** | **0.91-0.96** | inconclusive | 216-315 B | ✅ |
| Wide (9 arms, every supported type) | **1.10-1.13 (slower)** | inconclusive | 660-799 B | ❌ |

- **Monomorphic agrees across all three runs**: the narrow form is faster every time with non-overlapping CIs, the wide form slower every time. The narrow form is inlined into the caller; **the wide form stays out of line and pays a call per conversion, making it worse than having no fast path at all**
- **Mixed input supports no claim.** The same narrow form measured 0.91x, 0.97x and 1.05x - it changes sign between runs - and the baseline itself drifts from 15.59 to 14.22 ns. The cause is the interface path's Tier1 code size swinging between 1,777 B and 15,792 B as Dynamic PGO settles differently. The generated code does differ, so this is recorded as **measurement noise**, not as "no difference"
- Allocation is identical in all three forms (the returned string dominates)

**AOT:** ✅ No issue. AOT has no profile-driven devirtualization, so the interface path loses its advantage there and **the fast path should be worth relatively more than under the JIT** (not yet measured)

**Example:**

```csharp
// ✅ Only the few types that dominate the call site. Keep it small
public static string Convert(object value) => value switch
{
    int v => v.ToString(CultureInfo.InvariantCulture),
    string v => v,
    _ => value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value.ToString()!,
};

// ❌ Enumerating every supported type - it defeats itself by exceeding the inlining threshold
```

**How to check:** run with `DOTNET_JitDisasm=<method>` and `DOTNET_TC_CallCountingDelayMs=0`, then compare the Tier1 `Total bytes of code` against the caller's size to see whether it was inlined (the DisassemblyDiagnoser may not emit type-switch bodies at all).

**Use cases:** ORM parameter conversion, value formatting for logs, generic value conversion in serializers.

**Caution:** The gain comes from **devirtualization via the type test**, not from shortest-overload selection (the `ToString()` vs `ToString(provider)` difference was separately found not to reproduce on net10). And the `switch` here lowers to **MethodTable comparisons on type patterns** - unrelated to the length / character bucketing of [TXT-10](#-txt-10-aggregating-string-matching-into-a-switch).

**Related:** [COL-05](#️-col-05-concrete-type-dispatch-for-ienumerable-parameters) applies the same type-test idea to `IEnumerable<T>` arguments, where the deciding factor is the input type distribution instead. For devirtualization from static type information see [DSP-01](#-dsp-01-devirtualization-via-sealed).

**Repository implementation:** [Benchmark](benchmarks/PerformancePatterns.Benchmarks/Lab/ObjectToStringBenchmark.cs) / [Results](benchmarks/results/TXT-11-ObjectToStringFastPath.md)

---

## 🔄 ASY: Asynchronous

### 🔄 ASY-01: Eliding the async state machine

**Goal:** In methods that just return an inner Task / ValueTask without touching it, drop `async`/`await` and return it directly (async elision).

**Effect (measured, net10 / x86-64-v4, synchronously completing path):**

- Returning the Task directly: **0.16x (6.4x faster) + 73B → 0B** — the async wrapper re-wraps even an already cached completed Task into a freshly allocated Task every call
- Same for ValueTask: 0.83ns for the direct return vs 4.23ns for the await wrapper (0.13x vs 0.67x; both allocate 0)

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ async/await on a plain forward (state machine + result re-wrapping)
public async Task<int> ReadAsync(byte[] buffer) => await inner.ReadAsync(buffer);

// ✅ Return it directly (async elision)
public Task<int> ReadAsync(byte[] buffer) => inner.ReadAsync(buffer);
```

**Applicability (important):** Restrict this to plain forwards — a single await whose result is returned immediately, crossing no `try`/`using`/`lock` scope. Eliding across such a scope changes exception and disposal semantics (a synchronous exception thrown before the await propagates straight to the caller, a `using` disposes before completion, and so on). An elided method also disappears from async stack traces.

**Use cases:** Forwarding methods in decorator and wrapper layers, delegating interface implementations, returning a completed Task on a cache hit.

---

### 🔄 ASY-02: Producer/consumer with System.Threading.Channels

**Goal:** Hand data between threads with `Channel<T>` instead of a hand-rolled lock + queue + signal.

**Effect (measured, net10 / x86-64-v4, pumping 10,000 items):**

- An unbounded channel costs **about 39ns per item** (write + async read + completion signal included) with near-zero allocation
- The `SingleReader`/`SingleWriter` options give 0.89x in this scenario (non-overlapping CIs) — a modest but real gain even with a single producer and consumer; declare them whenever the topology is fixed
- Bounded (capacity 128) is 1.63x — decide whether that is a fair price for backpressure (a guaranteed memory ceiling)

**AOT:** ✅ No issues

**Example:**

```csharp
var channel = Channel.CreateUnbounded<Work>(new UnboundedChannelOptions
{
    SingleReader = true, // Declare it when the topology is fixed (it does no harm)
    SingleWriter = false,
});

// Producer: await channel.Writer.WriteAsync(work);  when finished, channel.Writer.Complete();
// Consumer: await foreach (var work in channel.Reader.ReadAllAsync()) { ... }
```

**Use cases:** Background work queues, log and metric aggregation, connecting pipelined stages.

**Caveats:** Unbounded lets memory grow without limit when production outpaces consumption. If you need a ceiling, pick Bounded + `FullMode` (Wait/Drop family) and budget for the 2x cost.

---

### 🔄 ASY-03: System.IO.Pipelines

**Goal:** Connect stream reads and writes through a `Pipe` (PipeReader/PipeWriter) and let the infrastructure own buffer management, partial reads, and backpressure.

**Effect (measured, net10 / x86-64-v4, same-thread transfer of 16 x 4KB chunks):**

- Against a `MemoryStream` round trip, time is 2.2x (the cost of the synchronization machinery), but **allocation drops from 128.2KB to 1.8KB (1/70)** — the payoff of reusing pooled segments
- Its real domain is streaming network/file I/O; it is not meant for shuffling small in-memory payloads

**AOT:** ✅ No issues (NuGet package System.IO.Pipelines)

**Use cases:** Framing socket input (length prefixes, line splitting), Kestrel-style protocol handling, incremental parsing of large streams (pairs with SEQ-01).

**Caveats (a trap we actually hit):** The default `PauseWriterThreshold` is 64KB; once unconsumed data reaches it, `FlushAsync` waits for the reader and **never completes**. A sequential "write everything, then read" structure deadlocks at exactly 64KB — the writer and reader must always run concurrently (the first version of our own verification hit this deadlock).

---

### 🔄 ASY-04: Knowing the cost of IAsyncEnumerable and when to use it

**Goal:** Understand the per-item overhead of `await foreach` and avoid async streams for data that can be enumerated synchronously.

**Effect (measured, net10 / x86-64-v4, enumerating 1,024 synchronously completing items):**

- Against 0.48ns/item for a synchronous `foreach`, `await foreach` costs **7.3ns/item (15.2x)** — the price of the state machine and the ValueTask machinery behind `MoveNextAsync`
- In absolute terms it is only about 7ns, so it dilutes to nothing in streams whose per-item work is heavy (hundreds of ns and up)

**AOT:** ✅ No issues

**Design guidance:**

- Return `IEnumerable<T>` or a Span-based type when enumeration can be synchronous. Use `IAsyncEnumerable<T>` when producing the items is itself asynchronous (paging APIs, DB cursors, sockets)
- Accept cancellation through a `CancellationToken` parameter marked `[EnumeratorCancellation]`, and have callers pass it with `WithCancellation`
- For transfers inside a library, `ReadAllAsync` from ASY-02 (Channels) offers the same shape of consumption API

---

### 🔄 ASY-05: ValueTask / IValueTaskSource

**Goal:** Remove the `Task` object allocation from async APIs that usually complete synchronously.

**Effect:**

- `ValueTask<T>` allocates **nothing on the heap** when it completes synchronously (the result rides inside the struct). It pays off in APIs that return on a cache hit or when the buffer already holds the data
- On high-frequency paths that complete asynchronously over and over (socket reads and writes), implementing `IValueTaskSource` lets you **reuse the awaitable object itself** (the BCL's Socket and PipeReader do this)
- Combined with ASY-01 (async elision), the forwarding layer costs nothing either

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ A cache hit completes synchronously (no allocation); only a miss goes async
public ValueTask<Entry> GetAsync(string key, CancellationToken cancel)
{
    if (cache.TryGetValue(key, out var entry))
    {
        return new ValueTask<Entry>(entry);
    }

    return LoadAsync(key, cancel);   // Only this one is an async method
}
```

**Use cases:** Cached lookups, reads from a buffered stream, conditional I/O.

**Caveats (the ValueTask usage contract):**

- **Await it exactly once.** Awaiting twice, reading `.Result` more than once, or awaiting concurrently is undefined behavior. Convert with `AsTask()` if you need more than one await
- It is a return type, not something to stash in a field and reuse
- Hand-writing `IValueTaskSource` is hard to get right (build on `ManualResetValueTaskSourceCore<T>`). Only start **after you have measured a clear bottleneck**

**Measured (net10 / x86-64-v4, per synchronous completion):**

| Approach | Time | Allocated |
|---|---:|---:|
| `Task.FromResult` (value outside the BCL cache) | 2.87 ns | 72 B |
| **`new ValueTask<int>(value)`** | **0.93 ns** | **0 B** |
| async method returning Task (completes synchronously) | 6.45 ns | 72 B |
| **async method returning ValueTask (completes synchronously)** | **4.23 ns** | **0 B** |

Task keeps allocating 72 B on every synchronous completion; ValueTask allocates nothing. Pair it with async elision (ASY-01) and the forwarding layer cost disappears too. → [Results](benchmarks/results/ASY-05-ValueTask.md)

---

### 🔄 ASY-06: Single-loop scheduler

**Goal:** Manage every job's firing from one wait loop instead of creating a `Timer` per job.

**Effect:**

- Timer objects and their callback registrations drop from one per job to exactly one
- Computing the next due time and waiting once minimizes timer interrupts while idle
- Adding and removing jobs becomes "re-wake the wait" (swap in a new TaskCompletionSource, then complete the old one)

**AOT:** ✅ No issues

**Example (skeleton):**

```csharp
private TaskCompletionSource wakeup = new(TaskCreationOptions.RunContinuationsAsynchronously);

private async Task RunLoopAsync(CancellationToken cancel)
{
    while (!cancel.IsCancellationRequested)
    {
        var delay = CalculateNextDelay();                  // Clamp to an upper bound
        await Task.WhenAny(wakeup.Task, Task.Delay(delay, cancel)).ConfigureAwait(false);
        FireDueJobs();
    }
}

public void Notify()
{
    // ✅ Swap in the new TCS before completing the old one (so no wake-up is lost)
    var previous = Interlocked.Exchange(ref wakeup, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    previous.TrySetResult();
}
```

**Use cases:** Job schedulers, retry management, TTL expiry, batch flush control.

**Key design points:**

- **Cap the wait duration** (say, one hour). Long waits are vulnerable to system clock changes and drift
- Read the time for due checks from a monotonic clock, per SYS-01 (TickCount64 / GetTimestamp)
- Reuse the due-job list by calling `Clear()` each pass. Write continuations in the static + state form `ContinueWith(static (t, state) => ..., this, ...)` (DSP-04)
- If the schedule can be expressed as a bit set, BIT-03 (TrailingZeroCount) gets the next candidate in O(1) — the standard trick in cron implementations

**Measured (net10 / x86-64-v4, primitive level):** Creating and disposing a `Timer` per job costs 36.0 ns + 120 B (including registration in the global timer queue), while the single-loop wake-up (TCS swap + `TrySetResult`) costs **20.3 ns + 88 B (0.56x)**. This compares registration and notification primitives only; whole-scheduler behavior under load is out of scope. → [Results](benchmarks/results/ASY-06-SchedulerPrimitive.md)

**Caveats:** If the main goal is producer/consumer handoff, ASY-02 (Channels) fits better.

---

### 🔄 ASY-07: Streaming I/O

**Goal:** Stream data as it arrives or is produced instead of collecting it all in memory first.

**Effect:**

- Buffering the whole response disappears, so peak memory goes from the full payload size to the chunk size
- Large payloads avoid LOH allocations and GC pressure
- Processing can start at the first byte, which improves perceived latency (time to first byte)

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Return as soon as the headers arrive and read the body incrementally
using var response = await client
    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancel)
    .ConfigureAwait(false);

await using var stream = await response.Content.ReadAsStreamAsync(cancel).ConfigureAwait(false);
await ParseAsync(stream, cancel).ConfigureAwait(false);   // Never materialize the whole thing as byte[]

// ✅ On the send side, write straight to the stream inside HttpContent (no intermediate MemoryStream)
protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    => JsonSerializer.SerializeAsync(stream, value, jsonTypeInfo);
```

**Key design points:**

- **Return `TryComputeLength` / `Content-Length` when the length is known up front** (the receiver can size its buffer, and chunked overhead goes away). Fall back to chunked only when it is not
- When wrapping in a compression stream, pass `leaveOpen: true` so the underlying stream stays open while the compressor is reliably flushed
- Hand everything to `Stream.CopyToAsync` when you do not need progress reporting (the runtime already optimizes it). Write a manual loop over a rented ArrayPool buffer only when you do

**Related:** ASY-03 (Pipelines) for serious framing, SEQ-04 (ring buffer) for hand-managed fixed buffers.

**Measured (net10 / x86-64-v4, processing a 1 MB payload):** Buffering everything into a byte[] before processing costs 395.5 μs + **2,097,484 B (Gen0/1/2 collections = LOH pressure)**, while incremental processing over 16 KB ArrayPool chunks costs **224.5 μs + 64 B (0.58x, zero GC)**. Peak memory falls from the whole payload to one chunk, and cache locality makes it faster as well. → [Results](benchmarks/results/ASY-07-StreamBuffering.md)

**Caveats:** Incremental processing can leave a partially sent state on error, so design it together with your retry strategy.

---

## 🔒 CON: Concurrency and synchronization

### 🔒 CON-01: One-shot guards with Interlocked

**Goal:** Implement run-exactly-once control (guarding against repeated Dispose, idempotent initialization) with a single `Interlocked` instruction instead of a lock.

**Effect (measured, net10 / x86-64-v4, steady-state cost of the already-disposed path):**

| Approach | Time | Code size | Exactly-once guarantee |
|---|---|---|:---:|
| Plain bool | 0.18 ns | 26 B | ❌ (single-threaded only) |
| lock(System.Threading.Lock) | 8.85 ns | 2,612 B | ✅ |
| **Interlocked.Exchange / CompareExchange** | **3.90 / 3.98 ns** | **33 / 56 B** | ✅ |

Comparing thread-safe exclusion against thread-safe exclusion, `Interlocked` is **2.2-2.3x faster than lock with roughly 1/50 the code size**.

**AOT:** ✅ No issues

**Example:**

```csharp
private int disposed;

public void Dispose()
{
    if (Interlocked.CompareExchange(ref disposed, 1, 0) != 0)
    {
        return;     // Second and later calls return immediately
    }

    // Release work (runs exactly once)
}

// Idempotent initialization (fire-once, no need to wait for the result)
private static int initialized;

public static void EnsureInitialized()
{
    if (Interlocked.Exchange(ref initialized, 1) == 1)
    {
        return;
    }

    // Initialization work
}
```

**Use cases:** Dispose guards, global initialization, CAS-controlled flags.

**Caveats:**

- **In types that do not need thread safety (single-threaded ref structs and the like) a plain bool is the fastest option (0.40ns) and is good enough.** This pattern is about avoiding a lock when you do need thread-safe exclusion
- `Interlocked` has no bool overload, so express the flag as an `int` (0/1). If you wrap it to look like a bool, the standard shape is an AtomicBoolean storing true as -1 (all bits set)
- Note that the `Exchange` form returns without waiting for initialization to finish. Use `Lazy<T>`, or a lock with double-checking, when callers must wait

---

### 🔒 CON-02: Reference counting for pinned resources

**Goal:** Pin a shared resource (a cached page, a pooled buffer) for as long as a reader needs it and release it deterministically when the last user is done, without paying an interlocked operation per item read.

**Effect:**

- **Run-length batching is the large win.** When consecutive items belong to the same resource, retain once per run instead of once per item. 128 rows over 4 pages means 4 interlocked pairs instead of 128
- **Optimistic retain is the small win, and only under contention.** A CAS retry loop re-reads and retries on every lost race; a single `Interlocked.Increment` always makes progress in one operation. Uncontended, the two are identical - the retry loop never actually retries
- Death is stamped as a large negative bias rather than zero, so a late increment can never resurrect a dead entry

**AOT:** ✅ No issues

**Example:**

```csharp
// ✅ Optimistic: one fetch-add, no retry storm under contention
public bool TryRetain()
{
    var result = Interlocked.Increment(ref refCount);
    if (result >= 1)
    {
        return true;
    }

    Interlocked.Decrement(ref refCount);    // lost the race with death
    return false;
}

public void Release()
{
    // The bias must be large enough that a late increment cannot lift the count back above zero
    if ((Interlocked.Decrement(ref refCount) == 0) &&
        (Interlocked.CompareExchange(ref refCount, int.MinValue / 2, 0) == 0))
    {
        Dispose();
    }
}

// ✅ Retain once per run, not once per row
Page? last = null;
foreach (var row in rows)
{
    var page = row.Page;
    if (!ReferenceEquals(page, last))
    {
        if (page.TryRetain())
        {
            retained[retainedCount++] = page;
        }

        last = page;
    }
}
```

**Use cases:** Page / buffer cache pinning - a range scan pins the pages it is reading so they cannot be evicted, and the last release frees deterministically instead of waiting for the GC.

**Measured (net10 / x86-64-v4):** Run-length batching over 128 rows spread across 4 pages takes 0.483 ns/row against 7.68 ns for retain-per-row - **0.06x, about 16x**. The primitive swap is worth **0.57x under 4-thread contention** (29.30 → 16.60 ns) and **1.00x uncontended** (7.720 vs 7.728 ns, overlapping CIs). → [Results](benchmarks/results/CON-02-RefCount.md)

**Caveats:**

- **Order matters: batch first, then choose the primitive.** Batching is worth ~16x, the primitive swap is worth 1.75x and only when threads actually contend. Swapping the primitive on an uncontended path buys nothing
- The optimistic form's cost is correctness surface, not code size (115 → 120 B). Getting the death bias wrong resurrects freed objects, which fails far from the bug
- Batching is only valid when the run really is contiguous. Verify the grouping, or you release a resource a later item still holds

---

### 🔒 CON-03: False sharing and cache line padding

**Goal:** Break the cache invalidation ping-pong that happens when per-worker counters land on the same cache line.

**Effect:**

- Writing to adjacent slots concurrently costs **4.0-7.4x at 2 workers and 5.4-29.7x at 8**, depending on the machine
- **Padding is unconditional; the size is not.** 64 bytes is not enough on x86-64-v3 (128 bytes is 2.86x faster at 8 workers) and is fully sufficient on x86-64-v4 — pick the size from the target's prefetch granularity, and default to 128 bytes when it is unknown
- Making the writes `Interlocked` does not remove the penalty, it amplifies it

**AOT:** ✅ No issues

**Example:**

```csharp
// ❌ Per-worker counters share one or two cache lines
private readonly long[] counters = new long[workerCount];

// ✅ One slot per 128 bytes, wide enough to account for adjacent line prefetching
[StructLayout(LayoutKind.Explicit, Size = 128)]
internal struct PaddedCounter
{
    [FieldOffset(0)]
    public long Value;
}

private readonly PaddedCounter[] counters = new PaddedCounter[workerCount];

// Equivalent if you would rather stay with a plain array: stride the indices
private readonly long[] strided = new long[workerCount * 16];   // 16 * 8 = 128-byte stride
```

**Use cases:** Per-worker statistics counters, sharded hit counts, ring buffer head / tail, intermediate buffers for parallel aggregation.

**Measured (net10 / x86-64-v4 (Zen 5, 12 physical / 24 logical), 50,000 writes per worker):**

| Workers | Adjacent (baseline) | **64 bytes** | 128 bytes | Adjacent + Interlocked | 128 bytes + Interlocked |
|---:|---:|---:|---:|---:|---:|
| 2 | 151.12 μs | **33.78 μs (0.22)** | 46.01 μs (0.30) | 891.01 μs (5.90) | 557.32 μs (3.69) |
| 4 | 495.02 μs | **38.43 μs (0.08)** | 104.93 μs (0.21) | 2,413.44 μs (4.88) | 830.15 μs (1.68) |
| 8 | 756.31 μs | 138.88 μs (0.18) | 139.13 μs (0.18) | 4,354.64 μs (5.76) | 1,046.98 μs (1.38) |

**Among the largest improvements in this book, on every machine measured**: 4.0-4.5x at 2 workers and 5.4-12.9x at 4-8 workers here, 7.4x and 29.7x on x86-64-v3. Measured twice, because thread scheduling makes this benchmark's noise larger than the effects being compared on the padded rows. → [Measurement](benchmarks/results/CON-03-FalseSharing.md)

**Caveats:**

- **The padding is not the conditional part — the size is.** Every configuration measured on either machine is 4-13x better than adjacent slots, so never skip the pad because the right size is unclear
- **Pick the size from the target's prefetch granularity, not from the cache line size.** On x86-64-v3, 64 bytes is not enough: at 8 workers it measures 150.59 μs against 52.66 μs for 128 bytes (2.86x), because a prefetcher that pulls **cache line pairs** makes two adjacent 64 B lines behave as one contended granule — which is why the BCL itself pads to 128. On x86-64-v4 there is no pair effect at any worker count: 64 and 128 tie at 8 workers, and 64 bytes is 1.4-2.7x **faster** at 2 and 4
- **Default to 128 bytes when the target is unknown**, because the two failure modes are not symmetric: over-padding costs footprint and some low-contention throughput, while under-padding brings the full false-sharing penalty back at high contention
- **Measure at your real worker count.** The two sizes converge as contention rises, so a low-worker benchmark shows over-padding at its worst and a high-worker one shows under-padding at its worst
- **`Interlocked` does not hide the penalty.** At 8 workers adjacent interlocked is 5.76x the adjacent volatile baseline and 4.2x its padded counterpart. Any design that puts `Interlocked` on an array of counters has this pattern as a **precondition** (CON-01 targets a single variable and is unaffected)
- Time scales with the worker count, so **ratios are only meaningful inside one worker count**
- This trades memory for throughput. Paying 128 bytes per worker is only worth it for counters that really are written concurrently
- Cache line width is hardware dependent. These numbers are from Zen 3 (64-byte lines plus adjacent line prefetching)

---
## 🖥️ SYS: System and OS facilities

### 🖥️ SYS-01: Low-cost time and elapsed-time reads

**Goal:** Avoid the cost of reading wall-clock time (`DateTime.UtcNow`) for cache TTLs, timeout checks, and elapsed-time measurement.

**Effect (measured, net10 / x86-64-v4):**

| API | Time | Ratio | Characteristics |
|---|---|---|---|
| `DateTime.UtcNow` / `DateTimeOffset.UtcNow` | 21.5 / 21.7 ns | 1.00 | Wall-clock time. Affected by system clock changes |
| `Stopwatch.GetTimestamp` | 16.0 ns | 0.75 | High resolution and monotonic. Convert to TimeSpan with `Stopwatch.GetElapsedTime` |
| `Environment.TickCount64` | **1.08 ns** | **0.05 (20x)** | Monotonic, millisecond units (resolution ~10-16ms) |

**AOT:** ✅ No issues

**Design guidance:**

- TTL and timeout checks (millisecond precision is enough) → compare deltas of `Environment.TickCount64`
- High-precision elapsed time → `Stopwatch.GetTimestamp` + `Stopwatch.GetElapsedTime`
- `DateTime.UtcNow` only when you genuinely need a calendar date and time (using the wall clock for decisions misbehaves when the system clock changes, so the monotonic clock also wins on correctness)
- Abstract behind `TimeProvider` (.NET 8+) where testability matters, and call the API directly only on hot paths

**Use cases:** Cache expiry, rate limiters, retry and timeout management, lightweight metrics.

---

## 🗄️ DAT: Data access

### 🗄️ DAT-01: Optimizing column resolution in DB access

**Goal:** In `DbDataReader`-to-POCO mapping, move the resolution cost that otherwise repeats per row and per column to before reading starts.

**Effect:**

- **Resolve ordinals up front**: look up ordinals from column names once per reader instead of once per row. Hold them in a `readonly struct` passed by `in`, and row mapping becomes plain struct field reads (MEM-04)
- **Single-pass column resolution**: instead of calling `GetOrdinal(name)` once per column, walk `GetName(i)` in ascending order once and settle every ordinal. Missing columns stay at -1, so a partial SELECT does not throw (`GetOrdinal` does)
- **Typed reader methods**: call `GetInt32` / `GetString` and friends directly instead of `GetValue` plus an unbox. Read enums as their underlying type and cast
- **Cache by result-set shape**: key the mapper cache on the combination of column names and column types (the same POCO needs a different mapper when the SELECT list differs)

**AOT:** ✅ No issues (for codegen or hand-written mappers. For reflection/Emit-based mappers see AOTP-01/06 in [aot-compatibility.md](docs/aot-compatibility.md))

**Example:**

```csharp
// ✅ Resolve ordinals once and fold them into a readonly struct
private readonly struct Ordinals(int id, int name, int createdAt)
{
    public readonly int Id = id;
    public readonly int Name = name;
    public readonly int CreatedAt = createdAt;
}

// ✅ Resolve every column in a single GetName pass (missing ones stay -1)
static Ordinals ResolveOrdinals(DbDataReader reader)
{
    int id = -1, name = -1, createdAt = -1;
    for (var i = 0; i < reader.FieldCount; i++)
    {
        var column = reader.GetName(i);
        if (String.Equals(column, "Id", StringComparison.OrdinalIgnoreCase)) { id = i; }
        else if (String.Equals(column, "Name", StringComparison.OrdinalIgnoreCase)) { name = i; }
        else if (String.Equals(column, "CreatedAt", StringComparison.OrdinalIgnoreCase)) { createdAt = i; }
    }

    return new Ordinals(id, name, createdAt);
}

// ✅ Settle the ordinals on the first row, then enter the loop body (no per-row "is this the first?" check)
if (reader.Read())
{
    var ordinals = ResolveOrdinals(reader);
    do
    {
        list.Add(Map(reader, in ordinals));
    }
    while (reader.Read());
}
```

**Choosing a column-name matching strategy:** Switch between a chain of `String.Equals(OrdinalIgnoreCase)` (few columns) and a sampling-hash switch (moderate to many) based on the column count (COL-04 / BIT-01). Codegen knows the column count at generation time, so it can emit the right one. Where case-sensitive matching is acceptable, emit a **plain ordinal switch** instead - fastest up to 64 entries (TXT-10), and at 24 columns it measures 0.91-0.98x of the sampling-hash switch with a third less code. **Never normalise the probe to make that switch usable:** upper-casing first costs 2.7-3.2 ns per column and loses to the case-insensitive form in every condition measured (2.0-3.0x at 8 columns, 1.35-2.91x at 24) → [LAB-ColumnMatch](benchmarks/results/LAB-ColumnMatch.md).

**Choosing a CommandBehavior:**

| Flag | Effect | Caveats |
|---|---|---|
| `SequentialAccess` | Reads forward only, without buffering the whole row | **Columns must be read in ascending ordinal order.** Unusable when you read in property declaration order |
| `SingleResult` / `SingleRow` | Skips preparing result sets and rows you do not need | Only when you know it is a single row / single result |
| `SchemaOnly` | Fetches the schema without transferring rows | Useful for resolving column types up front |
| `CloseConnection` | Closes the connection when the reader is disposed | Only for connections you opened yourself |

**Measured (net10 / x86-64-v4, in-memory reader of 3 columns x 1000 rows, per row):**

| Approach | Time/row | Ratio | Allocated/row | Code size |
|---|---:|---|---:|---:|
| `GetOrdinal` x 3 per row (baseline) | 7.45 ns | 1.00 | 0 B | 2,219 B |
| **Cached-ordinal struct passed by `in`** | **1.00 ns** | **0.13** | 0 B | 533 B |
| Cached ordinals + `GetValue` + cast | 4.26 ns | 0.57 | **48 B (❌ boxing)** | 1,169 B |

Simply hoisting ordinal resolution out of the row loop gives about 8x. Leaning on `GetValue` instead of the typed methods (`GetInt32` and friends) piles up value-type boxes every row (int + bool = 48 B). Provider virtual dispatch and I/O are excluded (the measurement isolates the difference in resolution strategy alone). → [Results](benchmarks/results/DAT-01-OrdinalResolve.md)

**Caveats:** Emitting this from a Source Generator is easier to maintain than hand-writing all of it.

---

## 🏭 GEN: Code generation

### 🏭 GEN-01: Strategies for fast Emit-generated code

**Goal:** When you do generate IL at runtime (`DynamicMethod` / `TypeBuilder`), make the **emitted code itself** fast.

**Effect:**

- **Inline child factories**: when one generated delegate calls another, the calls chain. Record the child's construction steps and expand them directly into the parent's IL to remove the calls (cap how much you expand)
- **Bake in constants**: when a dependency is already fixed at resolution time (singletons and such), emit a holder field read instead of a factory call
- **Make a holder type's fields the delegate target**: generate a type with exactly the fields you need and bind it with `CreateDelegate(type, holder)`, and the IL becomes direct field access via `Ldarg_0; Ldfld` (no `object[]` indexing, no closure references)
- ~~Call a concrete delegate type's Invoke with `Call`~~: **confirmed to have no effect on net10** (the codegen is byte-identical — see the measurement below). The target field read doubles as the null check, so the JIT removes the `callvirt` check
- **Minimize IL size**: pick the short-form opcodes (`Ldc_I4_0..8`, `Ldarg_0..3`) whenever the value is in range

**AOT:** ❌ **Incompatible.** Reflection.Emit throws `PlatformNotSupportedException` under Native AOT (AOTP-01 in [aot-compatibility.md](docs/aot-compatibility.md))

**Applicability:** Implement it as a JIT-only fast path and switch to a static fallback under AOT — the dual-path structure of AOTS-08.

```csharp
public static Func<T> CreateFactory<T>() where T : new()
{
    // Decide with IsDynamicCodeCompiled (in an interpreted environment Emit is actually slower)
    if (RuntimeFeature.IsDynamicCodeCompiled)
    {
        return EmitFactoryBuilder.Build<T>();
    }

    return static () => new T();
}
```

**Design guidance:** **Make a Source Generator (AOTS-01) the first choice for new development.** Reserve Emit for maintaining existing assets and for dynamic scenarios where the consumer cannot generate code, always paired with the fallback above.

**Measured (net10 / x86-64-v4, invoking a factory with 2 dependencies):**

| Target strategy | Time | Ratio |
|---|---:|---|
| C# closure lambda (reference point) | 3.77 ns | 1.00 |
| **Holder field (direct `ldfld` read)** | **4.23 ns** | **1.12 (near parity)** |
| Closure array (`ldelem` + `castclass`) | 4.61 ns | 1.22 (❌) |
| Chained child factories (Callvirt) | 6.36 ns | 1.69 (❌) |
| Chained child factories (Call) | 6.46 ns | 1.71 (❌) |

A holder-field target comes close to a compiled closure (both end up in the same `ldfld` shape). The closure array pays 1.2x and the child-delegate chain 1.7x — measurement confirms the claim that **inlining and holder fields pay off**.

The `Call` vs `Callvirt` substitution, on the other hand, measured 6.36 vs 6.46 ns with overlapping CIs, so per the decision policy we **compared the codegen under JitDisasm → 68 instructions, 229 bytes, byte-identical**. In a delegate Invoke the target field read (`mov rcx, [delegate+0x08]`) doubles as a hardware null check, so the JIT drops the `callvirt` check. This is therefore **not measurement noise but no difference — the substitution has no effect on net10 delegate Invoke** (recorded as R-17 in the rejected index). → [Results](benchmarks/results/GEN-01-EmitStrategy.md)

**Caveats:** Generated code is easy to miss in ordinary tests, so always provide equivalence tests for the output.

---

### 🏭 GEN-02: Designing Source Generator output

**Goal:** Pin down **what code a Source Generator (AOTS-01) has to emit to actually deliver performance**. This is guidance on the shape of the output, not on how to implement the generator.

**Three design principles:**

1. **Move runtime resolution to build time** — bake dictionary lookups, reflection, hash computation, and string building into constants, switches, and literal `new` expressions
2. **Branch on count and shape** — the generator knows how many items and which types are involved. Do the N-dependent implementation switching that a runtime library cannot
3. **Compose only measured patterns** — the body of the generated code uses only the adopted patterns in this catalog. Never include a rejected one (R-01 through R-17)

**AOT:** ✅ No issues (this is the fundamental means of AOT support)

**Scenario → shape to emit (summary; code examples and evidence in the [generated code pattern collection](docs/generated-code-patterns.md)):**

| Scenario | Shape to emit | Supporting measurement |
|---|---|---|
| Name → index resolution | ≤4 entries: Equals chain / 5-64: plain switch / >64: sampling-hash switch (constants baked in) | TXT-10 / COL-04 / R-07 |
| Per-type artifacts (SQL, type names, keys) | Written literally as const / static readonly / `"..."u8` | TYP-06 (0.09 ns) |
| DB row mapper | Ordinal struct + `in` passing + typed getters (never emit `GetValue`) | DAT-01 (0.13x) |
| Factories / DI | Inline the dependency graph into literal `new` expressions (never emit chained child factories) | GEN-01 (chaining is 2.3x) |
| Formatting / serialization | Direct `TryFormat` calls + u8 literals + `string.Create` + lookup tables (TXT-01) | TXT-01 / 05 / 07, R-16 |
| enum specialization | Apply a name switch; make ToString return constants from a switch | Reduces to TXT-10 / COL-04 |
| Collection conversion | Fixed capacity + `SetCount` + a loop writing straight into the Span | COL-01 / COL-06 |
| Change notification / events | Bake EventArgs into static readonly fields; shape by subscriber count | DSP-03 / DSP-04 |

**❌ Code you must not emit (anti-generation):** typeof caching (R-01), unconditional Frozen conversion (R-08), readonly for performance (R-10), CopyBlock substitution (R-14), hand-written digit padding (R-16), Call substitution (R-17), manual ref walking (R-02), hand-rolled hash loops (BIT-04), and making a runtime Type-keyed dictionary the main path (TYP-01). The full list with reasons is in section 9 of the [generated code pattern collection](docs/generated-code-patterns.md).

**Relationship to GEN-01:** Literally generated code can emit, AOT-safely, the same shape as Emit's best form (holder field 6.55 ns ≒ closure 6.23 ns). You only need Emit alongside it (the AOTS-08 dual path) for dynamic scenarios that cannot be generated at build time.

**Caveats:** Apply equivalence tests and this document's verification process (including the measurement-noise policy) to generated code as well.

---

## 🤖 Optimizations no longer worth hand-writing on net10 (runtime automation)

A list of hand-written optimizations that were once effective (or were held to be) but **no longer need writing because the current runtime (.NET 10) does them automatically**. Codegen and AI-generated code must not emit these in the name of speed — the straightforward, readable form compiles to the same thing.

| Hand-written form | What the runtime does automatically | How it was confirmed | Record |
|---|---|---|---|
| Rewriting a range check as `(uint)(v - min) <= max - min` | The JIT fuses the two-comparison form into a single unsigned comparison | Identical Tier1 codegen | [R-18](benchmarks/results/LAB-RangeCheck.md) |
| Rewriting `%` by a constant power-of-two size into an `&` mask | The JIT folds a constant remainder into the AND form | Measures on par with the mask | [BIT-02](benchmarks/results/BIT-02-PowerOfTwoMask.md) |
| Putting `AggressiveInlining` on small helpers | The default policy (PGO) inlines them even with a loop inside | Identical Tier1 code at the call site (94 B) | [JIT-01](benchmarks/results/JIT-01-Inlining.md) |
| Replacing `new T[0]` with `Array.Empty<T>()` | Empty arrays are shared, so allocation is zero (`[]` has the smaller code size) | BDN measured zero allocation | [STK-07](benchmarks/results/STK-07-LazyAllocation.md) |
| Rewriting a single-Span loop with `GetReference` + `Unsafe.Add` | A plain for loop eliminates bounds checks entirely | The manual form is 1.07-1.13x slower | [R-02](docs/rejected-patterns.md) |
| Manual ref walking in a simple loop over **multiple Spans** | The indexed form auto-vectorizes (0.23 ns/item) | The manual form blocks vectorization and is 1.25x slower | [R-02](benchmarks/results/LAB-DualSpanWalk.md) |
| Rewriting array traversal with `GetArrayDataReference` + `Unsafe.Add` | The indexed form gets bounds checks removed and auto-vectorized | 1.30x slower sequentially, no difference even with random access | [R-02](benchmarks/results/LAB-ArrayDataReference.md) |
| Switching to `delegate*` function pointers for speed | PGO speculatively devirtualizes and inlines delegates | Function pointers go through calli, defeating speculation: 6.04x slower | [DSP-02](benchmarks/results/DSP-02-CallAbstraction.md) |
| static readonly caching of `typeof(X)` | Tier1 constant-folds it into an immediate frozen RuntimeType pointer | Byte-identical codegen (11 B) | [R-01](docs/rejected-patterns.md) |
| Choosing between loop constructs (for / while) | Normalized to the same instruction sequence | Identical codegen (28 B) | [R-04](docs/rejected-patterns.md) |
| Substituting `call` for a delegate Invoke (to skip the null check) | The target field read already serves as the null check | Byte-identical codegen (229 B) | [R-17](docs/rejected-patterns.md) |
| Hand-written digit formatting loops (right-align shifting, reverse writing) | `TryFormat` internally computes the digit count, uses a two-digit table, and is unrolled | 2.5-4.8x slower than TryFormat | [R-16](docs/rejected-patterns.md) |
| Hand-rolled hash loops (FNV-1a and similar) | `string.GetHashCode` / XxHash3 are already vectorized | From 64 characters up, the hand-rolled version loses | [BIT-04](benchmarks/results/BIT-04-XxHash3.md) |
| Avoiding boxes that do not escape | Escape analysis moves them to the stack (0.004 ns) | STK-05 measurement | [STK-05](#-stk-05-boxing-avoidance-and-hot-value-caching) |
| Touching the last element first to coax bounds-check elimination | Identical without the hint (no difference across any variant) | Measured on net10 / net8 | [R-15](docs/rejected-patterns.md) |

**How to read this:** The forms listed here still work if you write them, but **there is no longer a reason to**. For the boundaries where hand-writing is still required (range checks with compound conditions, masks over runtime sizes, walking multiple Spans at once, boxes that escape), see the "what to do instead" notes in each pattern and in the rejection records.

---

## ⚠️ Assumptions that change under AOT

Every measurement in this document was taken under **JIT (net10, Dynamic PGO enabled)**. Native AOT has neither a JIT nor PGO, so **any form that was fast because it relied on PGO's speculative optimizations loses its advantage under AOT**. Re-evaluate the following when AOT is your main target.

| Item | Measured under JIT (with PGO) | What happens under AOT | Guidance for AOT |
|---|---|---|---|
| Delegates vs function pointers (DSP-02) | Delegate 1.21x < function pointer 5.00x | **Measured: the gap does not narrow, it reverses.** Delegate 5.62x vs function pointer 4.82x - the delegate loses guarded devirtualization and regresses 4.7x in absolute terms (269 -> 1,271 ns) while the function pointer barely moves (1,111 -> 1,090 ns). Interface/abstract dispatch also roughly doubles, 1.06x -> 1.30x | Hold the concrete sealed type (unchanged on both). Function pointers stop being the cliff under AOT - the delegate becomes it |
| Calls through an interface (DSP-01) | No difference with or without sealed (PGO guesses the monomorphic case) | **Measured: sealing an interface-typed call site is still worth nothing** (0.99x JIT, 1.00x AOT). What the concrete type buys depends on the interface: 0.92x -> 0.96x in DSP-01 (implementations byte-identical, deduplicated by ILC) but 1.06x -> 1.30x in DSP-02 where they genuinely differ | Hold the **concrete** type on hot paths - `sealed` alone still buys nothing. Trust DSP-02 for the size of the interface-dispatch cost |
| Inlining small helpers (JIT-01) | The default policy inlines automatically (the attribute makes no difference) | **Measured: confirmed, and the largest AOT swing found.** The default policy declines the inline - `DefaultPolicy` (1,392 ns) and `NoInline` (1,395 ns) are identical - and `AggressiveInlining` is worth **0.69x**, beating even the JIT inlined default | **Spell out `AggressiveInlining`** on small hot helpers the heuristic may decline (anything with a loop). Free under JIT, decisive under AOT |
| `AggressiveOptimization` | Disables Dynamic PGO, so it **can actually be slower** | **Measured: confirmed, and the JIT penalty is severe.** On a PGO-sensitive shape (interface dispatch in a loop) the attribute costs **5.79x** under JIT and 1.26x even on straight-line arithmetic; under AOT it is inert at 0.95x / 1.02x | **Do not add it to new code**, but decide case by case before removing it from existing code (see the applied-measurement table in JIT-01) - without a speculatable dispatch in the hot path, removing it exposes the tier-0 promotion cost and can be up to 1.31x slower. Under AOT it genuinely does nothing. → [Results](benchmarks/results/LAB-AotAssumptions.md) |
| Runtime code generation (GEN-01) | Emit's best form matches compiled code | `PlatformNotSupportedException` (AOTP-01) | Replace it with a Source Generator (GEN-02) |
| Hash source for Type keys (TYP-07) | `TypeHandle.Value` 0.62x on hit vs the virtual `GetHashCode` | **The ranking reverses.** The virtual call becomes the fastest of the three (the closed type graph lets it be statically devirtualized); handle is 1.08x hit / 1.19x miss and identity 1.18x / 1.23x against it | **Use the plain `type.GetHashCode()`.** The handle trick is a pessimization here. Its 8-byte alignment does still hold, so the `>> 3` stays correct - it just stops paying |

**What AOT makes better instead (measured):** Type-initializer-based caches and static tables such as TYP-04 / TYP-06 do run fully optimized from the very first call - a cold-start probe reaches its steady state of **1.20 ns from batch 1** under AOT, while the JIT needs about 10,000 calls to get there and passes through a **3,418 ns/call tier1 recompilation spike** on the way. AOT's steady state is also **2.9x faster** (1.20 vs 3.45 ns). The stronger reason to pre-compute per-type artifacts under AOT is the alternative: recomputing one through reflection at the call site costs **~125 ns/call under AOT vs ~4.85 ns under JIT**, roughly 26x worse and never improving. → [Results](benchmarks/results/LAB-AotAssumptions.md). The R-01 half of this claim (caching the `typeof` operator itself) has **not** been verified under AOT.

See [aot-compatibility.md](docs/aot-compatibility.md) for the detailed incompatible patterns and their workarounds.

---

## ❌ Rejected techniques (index)

Techniques measured and judged to have no effect or to be counterproductive. **Do not apply these as "optimizations" in codegen or review.** For each technique's intent, measurements, and alternatives, see [docs/rejected-patterns.md](docs/rejected-patterns.md).

| ID | Technique | Reason for rejection (one line) |
|---|---|---|
| R-01 | static readonly caching of `typeof(X)` | The JIT constant-folds typeof, so it is exactly the same speed |
| R-02 | Manual ref walking (GetReference / GetArrayDataReference) | Not faster in any shape (1.46x for multi-Span, 1.30x for arrays) |
| R-03 | Manual ref walking after `CollectionsMarshal.AsSpan` | No difference, only more code |
| R-04 | Choice of loop construct (for / while / do-while / ascending or descending) | No difference |
| R-05 | Applying ArrayPool to arrays of class elements | Per-element allocation remains, so it ranges from no effect to counterproductive |
| R-06 | Hand-rolled sort implementations | The BCL's `Span.Sort` is about 9x faster |
| R-07 | `SearchValues` for 2-3 candidate characters | The dedicated `IndexOfAny(char, char)` overload is faster |
| R-08 | Adopting `FrozenDictionary` unconditionally | 15-20x construction cost, and lookups can lose depending on the key set |
| R-09 | Using `fixed` pointers where Span would do | The same speed or slower (pinning adds fixed cost) |
| R-10 | Expecting JIT optimizations from readonly fields | As long as the access inlines, the difference is unmeasurable |
| R-11 | Holding a delegate bound directly to a static method | Going through a thunk can make it the slowest call form of all |
| R-12 | ref field cursors for iteration (C# 11) | Cannot beat Span + for; 1.21x |
| R-13 | Pinned (POH) buffers for performance | fixed measures as free, while POH allocation costs 17.5x plus Gen2 |
| R-14 | Replacing `Span.CopyTo` with `Unsafe.CopyBlockUnaligned` | Same speed for variable lengths, and only a marginal difference for constant lengths |
| R-15 | Touching the last element first to coax bounds-check elimination | No difference across any variant on either net10 or net8 |
| R-16 | Hand-written digit-ordering tricks (right-align then shift, reverse writing) | 2.5-4.8x slower than TryFormat + Fill |
| R-17 | Substituting Call for a delegate Invoke (to avoid Callvirt) | JIT codegen confirmed byte-identical (net10) |
| R-18 | Hand-written unsigned-overflow range checks | The JIT already fuses the two comparisons; codegen is effectively identical |
| R-19 | "P/Invoke speed-up" as a pattern (LibraryImport / SuppressGCTransition) | LibraryImport is the standard declaration form; SuppressGCTransition shows no gain |
| R-20 | Ref-returning accessor via `[UnscopedRef]` (for performance) | 1.02-1.07x against a get/set pair, CIs overlapping on both machines; no axis improves (the code-size delta is alignment padding) |
| R-21 | Recovering an index from a ref with `Unsafe.ByteOffset` | 1.45-1.52x against carrying the index, and larger code (same conclusion as R-02) |

---

## 🚫 What a Source Generator must not emit

Forms that codegen (and AI-generated code) tends to emit because they look faster, but that **measurement and codegen inspection have already shown to be anywhere from useless to counterproductive**. This is the concrete prohibition list behind GEN-02's third design principle, "compose only measured patterns".

| Form you must not emit | Reason (measured) | Emit this instead | Record |
|---|---|---|---|
| static readonly caching of `typeof(X)` | Byte-identical codegen at Tier1; before promotion the cache is the slower side | Write `typeof(X)` literally | R-01 |
| Rewriting a single-Span loop with `GetReference` + `Unsafe.Add` | A plain for loop already has bounds checks removed; 1.07-1.13x slower | An indexed `for` | R-02 |
| Manual ref walking (single / multiple Spans, arrays) | The indexed form already gets bounds-check elimination + auto-vectorization; the manual form is 1.46x slower over multiple Spans | An indexed `for` | [R-02](docs/rejected-patterns.md) |
| Rewriting array traversal with `GetArrayDataReference` | 1.30x slower sequentially, no difference even with random access | An indexed `for` | [MEM-02](#-mem-02-struct-element-array--ref-access-data-oriented-layout) |
| Converting read-only dictionaries to `FrozenDictionary` unconditionally | 7.4-10.2x construction cost with no lookup gain (string keys) | `Dictionary` or a name switch (COL-04) | R-08 |
| `readonly` on instance fields for performance | Identical codegen (apart from offsets) | Apply it only to express design intent | R-10 |
| Replacing `Span.CopyTo` with `Unsafe.CopyBlockUnaligned` | Variable lengths reach the same Memmove (measurement noise) | `CopyTo` | R-14 |
| Hand-written digit-padding loops (right-align shifting, reverse writing) | 2.5-4.8x slower than `TryFormat` + `Fill` | `TryFormat` + `Fill` | R-16 |
| Substituting `call` for a delegate Invoke (to avoid `callvirt`) | Codegen byte-identical at 68 instructions / 229 B | Leave it as `callvirt` | R-17 |
| Switching to `delegate*` function pointers for speed | calli blocks PGO speculation and inlining; 6.04x slower | A concrete sealed type or a delegate | [DSP-02](#-dsp-02-choosing-a-call-abstraction) |
| Hand-rolled hash loops (FNV-1a and similar) | Slower than `string.GetHashCode` from 64 characters up | XxHash3 (BIT-04) or sampling (BIT-01) | [BIT-04](#-bit-04-general-purpose-hashing-with-xxhash3) |
| Generating a runtime `Type`-keyed dictionary as the main path | The runtime Type path is 1.93x slower than a plain Dictionary | Take a generic API and resolve statically (TYP-01) | [TYP-01](#️-typ-01-static-type-slots-typemap--typeslot) |
| The `(T)Enum.Parse(typeof(T), name)` form | The non-generic version boxes and always allocates | `Enum.TryParse<T>` or a name switch | [STK-05](#-stk-05-boxing-avoidance-and-hot-value-caching) |

For the shape to emit per scenario and its evidence see the [generated code pattern collection](docs/generated-code-patterns.md); for the rejection details see [rejected-patterns.md](docs/rejected-patterns.md).

---

## 🔍 Reverse index: choosing by goal

| Goal | Recommended pattern |
|---|---|
| Eliminating bounds checks inside a loop | Write the indexed form (manual ref walking is rejected as R-02) |
| Cutting stack frame initialization cost | MEM-01 |
| Cutting function call cost | JIT-01 |
| Removing virtual calls from comparison and search | JIT-02 |
| Reducing branches in range checks | No hand-writing needed — the JIT fuses them (R-18) |
| Fast hashing of a known key set (enum names and the like) | BIT-01 |
| Keeping temporary objects off the heap | STK-01 |
| Referencing data without copying | STK-02 |
| Removing foreach allocations | STK-03 / STK-04 |
| Eliminating allocation for small buffers | BUF-03 (stackalloc) |
| Avoiding GC for medium to large buffers | BUF-01 / BUF-04 |
| Writing directly into the output buffer | BUF-02 |
| Incremental reading and writing of binary and text | SEQ-01 |
| Splitting text/binary | SEQ-01 |
| Struct I/O against a Stream | SEQ-02 |
| Sequence processing without materializing everything | SEQ-03 |
| Fast reads from a type-based map | TYP-01 |
| Dictionary key comparison for value types | TYP-02 |
| Reflection-free access to non-public members | TYP-03 |
| Eliminating allocations in internal data structures | MEM-02 |
| Slicing inside a hot loop | MEM-03 |
| Per-type specialization of generic conversion | JIT-03 / TYP-04 |
| Encouraging inlining on hot paths | JIT-04 |
| Index computation for hash tables | BIT-02 |
| Choosing how to hold callbacks and factories | DSP-01 / DSP-02 |
| Fast raising of multi-subscriber events | DSP-03 |
| Avoiding boxing at object boundaries | STK-05 |
| Allocation strategy for temporary buffers | BUF-05 |
| Direct access to List/Dictionary internals | COL-01 |
| Faster lookups in immutable dictionaries | COL-02 |
| Dictionary lookup with a Span key | COL-03 |
| Choosing an implementation for name → value resolution | COL-04 / BIT-01 |
| Fixed-format formatting and radix conversion | TXT-01 |
| Building short-lived strings | TXT-02 |
| Handling parse and conversion failures | TXT-03 |
| Speeding up casts already known to be type-safe | TYP-05 |
| Bitmap traversal and bit counting | BIT-03 |
| Removing async from plain forwards | ASY-01 |
| Reading time for TTLs and timeouts | SYS-01 |
| Eliminating lambda captures | DSP-04 |
| Designing to allocate only on first use | STK-07 |
| Preventing repeated Dispose and initialization | CON-01 |
| Passing large structs as arguments | MEM-04 |
| Holding a fixed-length region inside a struct | STK-08 |
| Removing the array allocation for variadic arguments | STK-09 |
| Reusing reference-type instances | BUF-07 |
| Splitting records out of a streaming receive | SEQ-04 / ASY-03 |
| Optimizing allocation and copying in collection conversion | COL-06 |
| Zero-allocation string creation | TXT-07 |
| Character search over many candidates | TXT-08 (use the dedicated overload for 2-3 candidates) |
| Separating concurrent counters onto cache lines | CON-03 |
| Optimizing struct size and field order | MEM-05 |
| Handling Memory\<T\> inside a loop | BUF-08 |
| Existence-checked update of a dictionary entry | COL-07 |
| Lane permutation Vector\<T\> cannot express | VEC-02 |
| Field-granular reads of variable-length records | STK-10 |
| Formatting and trimming fixed-length fields | TXT-09 |
| Matching against a compile-time string set | TXT-10 (COL-04 / BIT-01 above 64 entries or when the set is runtime-only) |
| Converting a boxed value to string | TXT-11 (keep the fast path to a few types) |
| General-purpose hashing (long inputs, stable values) | BIT-04 |
| Reading an unknown-length stream without extra copies | SEQ-05 / ASY-03 |
| Pinning a shared page or buffer while it is read | CON-02 |
| Searching sorted variable-length keys | BIT-05 |
| Hashing a Type known only at runtime | TYP-07 |
| Invoking an interceptor chain per request | DSP-06 / DSP-05 |
| Precomputing per-type strings and SQL | TYP-06 |
| Cutting the composition cost of pipelines and callbacks | DSP-05 |
| Async APIs that usually complete synchronously | ASY-05 |
| Time management for many jobs | ASY-06 |
| Sending and receiving large payloads | ASY-07 |
| Column resolution in a DB reader | DAT-01 |
| Making Emit-generated code itself faster | GEN-01 (AOT incompatible) |
| What to have a Source Generator emit | GEN-02 ([generated code pattern collection](docs/generated-code-patterns.md)) |

---

## 🛠️ Unsafe / MemoryMarshal API cheat sheet

The low-level APIs are spread across many patterns, so this table cross-references them by purpose.

| API | Purpose | Related patterns |
|---|---|---|
| `Unsafe.Add(ref r, i)` | Offset access from a ref (no bounds check) | STK-10 (structured reads) / R-02 (rejected for whole-element walks) |
| `Unsafe.As<T>(object)` | Cast that skips the type check (reference types) | TYP-05 |
| `Unsafe.As<TFrom, TTo>(ref v)` | Reinterpreting a ref (generic specialization, bit reinterpretation) | JIT-03 / SEQ-02 |
| `Unsafe.ReadUnaligned / WriteUnaligned` | unmanaged reads and writes at positions with no alignment guarantee | SEQ-01 / SEQ-02 / BUF-02 |
| `Unsafe.SkipInit(out v)` | Skipping initialization of an out variable | MEM-01 / SEQ-02 |
| `Unsafe.SizeOf<T>()` | Size of an unmanaged type (a JIT constant) | SEQ-01 / SEQ-02 |
| `Unsafe.IsAddressLessThan` | Comparing the positions of two refs (end detection) | STK-10 (structured reads) / R-02 (rejected for whole-element walks) |
| `Unsafe.AreSame(ref a, ref b)` | Testing whether two refs point at the same location (alias check) | Quick reference only ([Measurement](benchmarks/results/LAB-RefIdentity.md)) |
| `Unsafe.ByteOffset(ref a, ref b)` | Byte distance between two refs. Recovering an index this way **does not pay off** ([R-21](docs/rejected-patterns.md)) | R-21 (rejected) |
| `MemoryExtensions.Overlaps(span, other)` | Testing whether two Spans intersect as ranges | Quick reference only (1.40x heavier than `AreSame`) |
| `Unsafe.BitCast<TFrom, TTo>` (.NET 8+) | Bit reinterpretation of same-size value types (**the form of As that rejects a size mismatch. Identical generated code**) | TYP-05 / JIT-03 / SEQ-02 / TYP-02 |
| `Unsafe.Unbox<T>(object)` | Getting a ref into an existing box (update without reboxing) | STK-05 |
| `MemoryMarshal.GetReference(span)` | Getting a ref to the start of a Span | STK-10 / VEC-02 (SIMD loads) / R-02 (manual walking rejected) |
| `MemoryMarshal.GetArrayDataReference(array)` | Getting a ref to the start of an array | R-02 (rejected; this book has no positive use) |
| `MemoryMarshal.Cast<TFrom, TTo>(span)` | Reinterpreting a Span's element type (zero cost; **the length changes when element sizes differ and the remainder is truncated. No alignment check**) | TYP-02 / BIT-04 / [traps](benchmarks/results/LAB-SpanReinterpret.md) |
| `MemoryMarshal.AsBytes(span)` | Viewing a Span as bytes | TYP-02 |
| `MemoryMarshal.TryGetArray(memory)` | Getting an array segment out of a Memory without copying | BUF-08 |
| `MemoryManager<T>` | Publishing an unmanaged region as Memory | BUF-08 |
| `MemoryMarshal.CreateSpan(ref r, len)` | Building a Span from a ref | SEQ-02 / STK-10 |
| `CollectionsMarshal.AsSpan(list)` | Getting a Span over a List's internal array | COL-01 |
| `CollectionsMarshal.GetValueRefOrAddDefault` | Getting a ref to a dictionary entry | COL-01 |
| `CollectionsMarshal.GetValueRefOrNullRef` | Getting a ref to an existing entry only (pairs with `Unsafe.IsNullRef`) | COL-07 |
| `Unsafe.IsNullRef(ref r)` | Testing whether a ref is null (receiving an optional ref) | COL-07 |
| `RuntimeHelpers.IsReferenceOrContainsReferences<T>()` | Per-type branching on whether references are present (a JIT constant) | JIT-05 |

**Shared caveat:** These APIs make you responsible for bounds checking and type safety. Keep them inside internal implementations that sit behind a public API's input validation, and pair them with `Debug.Assert` in Debug builds.

---

## 📖 Document structure

| Document | Contents |
|---|---|
| README.md (this document) | Taxonomy, index, and commentary for performance implementation patterns (core knowledge) |
| [docs/rejected-patterns.md](docs/rejected-patterns.md) | Techniques that are not adopted, in detail (why they are ineffective or counterproductive) |
| [docs/aot-compatibility.md](docs/aot-compatibility.md) | AOT / trimming compatibility index (incompatible patterns and workarounds) |
| [docs/benchmark-methodology.md](docs/benchmark-methodology.md) | Benchmarking guidelines (BenchmarkDotNet setup and measurement pitfalls) |
| [docs/generated-code-patterns.md](docs/generated-code-patterns.md) | Source Generator codegen pattern collection (what to generate for speed, plus an anti-generation list) |

## 🏗️ Repository structure

```
dotnet-performance/
├── README.md                          Pattern catalog (this document)
├── docs/                              Supporting docs (AOT / rejected techniques / measurement methodology)
├── src/PerformancePatterns/           Pattern implementations (folders by category, pattern ID in the XML docs)
├── tests/PerformancePatterns.Tests/   Correctness verification (xunit)
└── benchmarks/
    ├── PerformancePatterns.Benchmarks/  Effect verification with BenchmarkDotNet (Lab/ is for investigation)
    └── results/                         Recorded measurements (keyed by pattern ID, in English)
```

- Implementations, tests, and benchmarks map back to this document by pattern ID (for example SEQ-01)
- Benchmarks follow the conventions in [docs/benchmark-methodology.md](docs/benchmark-methodology.md) (verify before running, avoid interning, net10 alone by default)
- For the detailed AOT IDs (AOTP-xx / AOTS-xx), see [docs/aot-compatibility.md](docs/aot-compatibility.md)

## 🌱 Candidate patterns for future additions

Every current candidate is already included in the body. New candidates are added from the angles below, and their Example sections get filled in once verified by implementation and benchmark.

- New runtime and language features (with each .NET and C# release)
- Implementation idioms extracted from production library code
- Items judged conditional in the verification queue whose conditions have not yet been separated out

**Handling unmeasured patterns:** Patterns explicitly marked "not measured in this repository" get their numbers filled in once measured during the example phase. A claim with no numbers behind it is weak for a catalog, so always measure before deciding to adopt.
