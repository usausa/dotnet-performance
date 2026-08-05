# 📐 Benchmarking Guidelines

[日本語](benchmark-methodology.ja.md) | **English**

The BenchmarkDotNet setup used to verify pattern effectiveness, plus how to avoid the pitfalls that make measurements meaningless.
The "measured examples" in [README](../README.md) assume measurements taken according to these guidelines.

## 🧰 Base configuration

- **Keep MemoryDiagnoser enabled at all times** — always judge speed and allocation together
- **Enable DisassemblyDiagnoser (printSource, exportDiff)** — to inspect generated code and code size. Even when the speed difference is at the measurement-noise level, code size can settle which variant is better (the impact on inlining shows up in code size)
- To look at generated code as a one-off without going through a benchmark, set the environment variable `DOTNET_JitDisasm="MethodName"` and run: the JIT assembly goes to stdout (a Release build plus `DOTNET_TieredCompilation=0` lets you inspect the final code directly)
- **By default, measure on the latest runtime (net10.0) alone**. Reserve running multiple runtimes side by side for verifications that specifically ask whether the effect changes across generations (optimizations that disappear in a newer generation, such as bounds-check elimination idioms or uint-cast tricks)

```csharp
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
            maxDepth: 3, printSource: true, exportDiff: true)));
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P90);
    }
}

// On the class: net10.0 only by default. Add jobs for net8 and the like only on classes targeted for generation verification
[MediumRunJob(RuntimeMoniker.Net10_0)]
```

## ⚠️ Pitfalls that make measurements meaningless

### 1. The measurement target vanishing under optimization

If a result is pinned to the "empty loop floor", the JIT has eliminated that variant entirely and you are not comparing real costs. Prevent that by returning a value, applying `[MethodImpl(MethodImplOptions.NoInlining)]`, or using BenchmarkDotNet's Consumer. Conversely, the fact that it was eliminated is sometimes itself the conclusion — "that abstraction is zero-cost" — so be deliberate about which of the two you are measuring.

### 2. String interning short-circuiting the comparison

If you use string literals directly as keys, reference equality makes `string.Equals` short-circuit without comparing contents, so you are not measuring the comparison code at all. Production traffic brings external input (non-interned strings), so always build probe strings as copies and confirm they are non-interned before measuring.

```csharp
var probe = new string(literal.AsSpan());          // Create a non-interned copy
Debug.Assert(string.IsInterned(probe) is null || !ReferenceEquals(string.IsInterned(probe), probe));
```

### 3. Not verifying equivalence across variants

Before measuring, run a `Verify()` that confirms all variants return the same result (call it before `BenchmarkRunner.Run`). Measuring an implementation that is fast but wrong is pointless. Manual ref walking is especially prone to bugs (miscomputed end refs, forgetting to advance a ref in a dual walk, and so on). As a real example, a loop whose faulty end condition was "always true" had the whole condition removed by the JIT, producing an abnormally fast, bounds-check-free false result that was believed for a long time (after the fix, re-measurement dropped it from fastest to mid-pack).

### 4. Measuring only the best case

Measuring only ideal shapes such as declaration-order access leads you to pick an implementation that degrades in production. Parameterize the access shape (forward / reverse / partial access / mixed misses) and choose the implementation that is stable across shapes, not the one with the fastest average.

### 5. Exception paths mixed in or not separated

Measure success and failure cases separately via `[Params]`. A single exception throw costs on the order of several μs, which completely masks every other optimization difference (if the failure path throws, optimizing everything around it is meaningless).

### 6. Over-trusting microbenchmark results

We have measured cases where a 30x difference on an isolated primitive dilutes to roughly 1.1x once embedded in real processing (I/O, rendering, dominant computation). Make the final call with a benchmark shaped like the real workload, and use microbenchmarks to select which implementations are candidates.

### 7. Mixing TFM-dependent methods via #if (when running multiple runtimes)

If you mix benchmarks for APIs that exist only on newer runtimes into one class with `#if NET9_0_OR_GREATER` and the like, the child build for the older runtime cannot resolve the methods the host (latest TFM) discovered, and **every case on that runtime comes out NA**. Separate TFM-dependent comparisons into their own classes behind `#if`, and attach only the corresponding runtime's job to each class.

### 8. Setup work leaking into the measurement

Do preparation such as searching for colliding keys or generating data in `[GlobalSetup]` and keep it out of the measurement. `IterationSetup` degrades measurement accuracy, so design for GlobalSetup plus no need for state resets wherever possible.

## ⚖️ Decision criteria

- Evaluate on **three axes: speed, allocation, and code size**. An improvement on one axis alone is weak justification for adoption
- Make the Ratio baseline "the straightforward implementation you have today" so the improvement reads off directly
- Record optimizations whose effect vanished in a newer generation as patterns that are "no longer needed" (move them to [rejected-patterns.md](rejected-patterns.md))

### Handling measurements that fall within measurement noise

**"A nanosecond-scale difference" is not measurement noise.** Nanosecond-scale differences are exactly what this repository is about, and a difference with non-overlapping CIs is recorded as a real difference even at 0.2 ns. Call it "measurement noise" **only when the confidence intervals (error bars) overlap and the difference cannot be resolved statistically**. Even then, do not declare it "rejected" outright — **go down to the generated code and split the case in two**:

| Codegen check result | Record as |
|---|---|
| Differs (instruction sequence or code size differs) | Record it as **➖ measurement noise**. Do not reject it — a real difference exists below measurement resolution, so keep the numbers on record along with the room it has to pay off depending on code size, environment, and inlining context |
| Matches (identical instruction sequence) | **No difference** — move it to the rejected side. Record "the generated code matched" as the basis |

The check has two stages:

1. **First pass**: the Code Size column from DisassemblyDiagnoser. If the size differs between variants, the generated code differs
2. **Confirmation**: comparing instruction sequences with JitDisasm. DynamicMethod can be matched by name too

```
DOTNET_TieredCompilation=0 DOTNET_JitDisasm="*MethodName*" ./app.exe
```

Example: GEN-01's replacement of delegate Invoke with a `Call` measured as noise at 6.36 vs 6.46 ns, but the JitDisasm comparison showed **68 instructions / 229 bytes matching exactly**, which confirmed "no difference" (the load of the target field doubles as the null check, so the JIT removes the `callvirt` check).

---

## 🧪 Verification queue (record of adopt/reject decisions)

The following are candidates to be adopted or rejected once a sample has been built and benchmarks have been run. The decision flow:

1. For each candidate, build a verification benchmark (plus a minimal implementation if needed) and measure it on net8 / net9 / net10
2. **Effective** → document it in the main text as a pattern (with an implementation example and measurements)
3. **Ineffective** → record it in [docs/rejected-patterns.md](rejected-patterns.md) together with "which generation it stayed effective through"
4. **Conditional** → document it with the conditions for applying it spelled out
5. **Measurement within measurement noise** → go down to the generated code (disassembly) and split the case in two. **If the generated code differs, record it as "➖ measurement noise"** (do not reject it — a difference below measurement resolution really exists, so keep the numbers on record along with the room it has to pay off on a different axis or in a different environment). **If the generated code matches as well, it is "no difference" and gets rejected** (recorded with the code match as the basis). For the procedure, see the decision criteria above. Note that **a nanosecond-scale difference is not in itself measurement noise** — if the confidence intervals do not overlap, even 0.2 ns is treated as a real difference. It is "measurement noise" only when the confidence intervals overlap and the difference cannot be resolved statistically

### ➖ Record of measurement-noise / no-difference verdicts

Differences that measurement could not resolve, listed together with the result of the codegen check (applied cases of step 5 of the decision flow):

| Subject | Measurement | Codegen check | Verdict |
|---|---|---|---|
| GEN-01 `Call` / `Callvirt` swap for delegate Invoke | 6.36 vs 6.46 ns, overlapping CIs | JitDisasm comparison shows **68 instructions / 229 bytes matching exactly** | ❌ **No difference** (the target field load doubles as the null check, so the JIT removes the callvirt check) |
| BUF-03 Time on the growth path (4 KB) | 1,283 vs 1,427 ns, **non-overlapping CIs** | Code size 4,638 vs 997 B — different code | **Real difference now** (0.90x). Formerly noise on the previous baseline; adopted on the allocation axis either way (8,056 B → 0 B) |
| BUF-04 Wrapper vs raw Rent/Return time | 1.63 vs 1.65 μs, overlapping ranges | — | ➖ **Measurement noise** (time axis). The wrapper cost is below measurement resolution. Adopted on the safety and allocation axes |
| COL-06 `ToImmutable` vs `MoveToImmutable` time (256 elements) | 203 vs 171 ns, **non-overlapping CIs** | Code size 2,035 vs 891 B — different code | **Real difference now** (MoveToImmutable faster). At 16 elements too (14.3 vs 11.3 ns), and allocation is always halved |
| STK-08 InlineArray vs stackalloc | 2.92 vs 2.87 ns, overlapping CIs | Code size 112 vs 134 B — **different code** | ➖ **Measurement noise** (time axis). InlineArray's value is that it can sit in a struct field; its code is slightly smaller |
| R-18 Hand-written unsigned range check | 210.9 vs 211.7 ns, overlapping CIs | **Effectively identical** at Tier1 (only the encoding differs — `sub r8d,100` vs `add r8d,-100` — 60 B) | ❌ **No difference** (the net10 JIT automatically fuses the two-comparison form into a single unsigned comparison) |
| JIT-01 AggressiveInlining attribute (helper containing a loop) | 0.943 vs 0.959 μs, overlapping CIs | Code at the call site is an **exact match** (100 B) | ❌ **No difference** (the default policy already inlines it. Only NoInlining shows a real difference, at +25% — which does prove the value of inlining itself) |
| STK-07 `new int[0]` vs `Array.Empty` | 0.137 vs 0.140 ns, overlapping CIs | **Identical code** (both a 12 B shared-reference load) | ❌ **No difference** (on net10 both allocate nothing and compile identically; `[]` remains the stylistic default) |
| DSP-01 sealed or not, through an interface reference | 220.7 vs 221.9 ns, overlapping CIs | Code size matches at 84 B (first pass) | ➖ **Measurement noise**. The concrete sealed type buys ~2% time and a 27 vs 84 B code-size win (its payoff is code size/AOT, not wall-clock) |
| COL-02 Frozen lookup (string keys, 16 / 256 entries) | 1.00 / 0.98x, overlapping CIs | — | ➖ **Measurement noise**. With no lookup gain, the 8-11x construction cost never amortizes, which meets the rejection condition |
| R-02 Switching range-guaranteed random access to refs | 245.2 vs 246.3 ns, overlapping CIs | Code size 55 vs 72 B | ➖ **Measurement noise**. The gain from bounds-check elimination is effectively zero (and the ref form costs 1.05x on a sequential walk by defeating auto-vectorization) |
| R-02 manual ref for sampling access (3 positions in a Span) | Time below resolution | **Different** (the indexed form keeps one bounds check = an RNGCHKFAIL path, 128 vs 115 B, 56 vs 49 instructions) | ➖ **Measurement noise**. The manual form is kept on hot paths whose range is guaranteed by construction (SampledNameTable.CalculateHash) |
| R-01 static readonly cache for typeof | Exactly equal | At Tier1 both **collapse to the same immediate load** (11 B. Before promotion, the cached side still carries an init check at 48 B) | ❌ **No difference** (on a cold path the cached side is actually worse) |
| R-04 Loop syntax: for / while | Exactly equal | **Identical instruction sequence** (28 B) | ❌ **No difference** ("normalization" holds for these two forms) |
| R-04 do-while / descending for | Exactly equal | **Different code** (do-while keeps a bounds check inside the loop, 63 B; the descending form is cloned, 85 B) | ➖ **Measurement noise**. Default to for / while |
| R-10 Instance readonly field | 0.006-0.016 ns, below the measurable range | The load is **identical apart from the offset** (4 B) | ❌ **No difference** (instance readonly contributes nothing to JIT optimization) |
| R-14 Replacing variable-length copies with CopyBlockUnaligned | 0.92-1.01x at 512 B+, overlapping CIs | The call shape differs, but both **reach the same Memmove** | ➖ **Measurement noise** at larger sizes. At 16 B there is a real difference (0.81x, call-shape overhead), but it is rejected on safety grounds |

| Batch | Candidate | Summary / question under test | Related | Status |
|:---:|---|---|---|:---:|
| ① | RuntimeHelpers.IsReferenceOrContainsReferences\<T\> branch | Skip clear/copy work for a T that holds no references. Does the JIT fold it to a constant and remove the branch entirely? | JIT-03 | ✅ Documented ([JIT-05](../README.md#️-jit-05-skipping-work-with-isreferenceorcontainsreferences)) |
| ① | Unsafe.CopyBlockUnaligned | Pin down the conditions under which it beats Span.CopyTo / Array.Copy (only when a constant length expands into a mov sequence?) | MEM-03 / SEQ-02 | ❌ Moved to the rejected list |
| ① | Bounds-check elimination by touching the last element first | Pre-touching with `_ = array[length - 1]`, and reverse unrolling. Re-confirm that it worked on .NET 8 and that the difference is gone on .NET 10 (rejection expected) | MEM-01 | ❌ Moved to the rejected list |
| ① | GC.AllocateUninitializedArray\<T\> | Skipping zero-initialization for large arrays. Pin down the size threshold at which it pays off | BUF-01 / BUF-05 | ✅ Documented conditionally ([BUF-06](../README.md#-buf-06-skipping-zero-init-with-gcallocateuninitializedarray)) |
| ① | Constant-size stackalloc | Cost difference between a constant allocation plus slicing and a variable size (the localloc instruction) | BUF-03 / BUF-05 | ✅ Documented ([STK-06](../README.md#-stk-06-constant-size-stackalloc)) |
| ② | CollectionsMarshal.SetCount (.NET 8+) | An Add loop (N capacity checks) vs SetCount plus direct Span writes. With a warning about the exposed uninitialized region | COL-01 | ✅ Documented (COL-01 extension, 0.22-0.26x) |
| ② | Concrete-type branching on an IEnumerable\<T\> argument | The LINQ-internal idiom of escaping to a Span path via `is T[]` / `is List<T>` / TryGetNonEnumeratedCount | COL-04 / STK-02 | ✅ Documented conditionally ([COL-05](../README.md#️-col-05-concrete-type-dispatch-for-ienumerable-parameters). 1.8x for List; no gain for arrays thanks to GDV) |
| ② | Implementation examples for COL-01, re-measured on our own environment | AsSpan / GetValueRefOrAddDefault (building implementation examples for an already-documented pattern) | COL-01 | ✅ Verified (AsSpan 0.52 / ref form 0.66) |
| ③ | Constant comparison of a byte sequence read as an int | Deciding short ASCII tokens (HTTP methods and the like) with a single uint/ulong constant comparison vs `SequenceEqual("..."u8)` | BIT-01 / TXT-01 | ✅ Documented ([TXT-04](../README.md#-txt-04-matching-byte-sequence-tokens-directly). Avoiding the string conversion is the real win; uint and SequenceEqual are equally fast) |
| ③ | Utf8.TryWrite (.NET 8+) | Formatting straight into a Span\<byte\> via the UTF-8 interpolation handler. Compared against the TXT-01 table approach | TXT-01 / BUF-02 | ✅ Documented ([TXT-05](../README.md#-txt-05-direct-utf-8-formatting-with-utf8trywrite), 0.54x / 0B) |
| ③ | ASCII-specialized processing | Fast paths that assume ASCII, via the Ascii class (.NET 8) / char.IsAsciiXxx / uppercasing with `& 0x5F` | BIT-01 / TXT-01 | ✅ Documented ([TXT-06](../README.md#-txt-06-ascii-specialized-comparison), 0.62x. With a warning that hand-written normalization collides with punctuation) |
| ③ | Implementation example for BUF-02 (wired straight to I/O) | Accumulating in a MemoryStream vs ArrayBufferWriter vs a hand-rolled PooledBufferWriter (demonstrating an already-documented pattern) | BUF-02 | ✅ Implemented (PooledBufferWriter. Allocation 2,976B → 32B) |
| ④ | Eliminating the async state machine | Returning the Task directly for a plain forward vs async/await. With a warning that the throw site and the using scope change | TXT-03 / ValueTask expansion candidate | ✅ Documented ([ASY-01](../README.md#-asy-01-eliding-the-async-state-machine), 0.16x / 73B → 0B) |
| ④ | Environment.TickCount64 / Stopwatch.GetTimestamp | Reading the time or elapsed time while avoiding DateTime.UtcNow (a dozen-plus ns). For cache TTLs and timeouts | — | ✅ Documented ([SYS-01](../README.md#️-sys-01-low-cost-time-and-elapsed-time-reads), TickCount64 is 22x) |
| ④ | Pinned buffers (GC.AllocateArray(pinned: true)) | Avoiding pinning cost with I/O buffers resident in the POH | BUF-01 / BUF-02 | ❌ Moved to the rejected list for performance purposes (fixed measures as free. The POH is strictly a countermeasure for long-lived fragmentation) |
| ④ | Putting BitOperations to work | Removing scan/compute loops with TrailingZeroCount / PopCount / Log2 | BIT-02 | ✅ Documented ([BIT-03](../README.md#-bit-03-bit-scanning-and-counting-with-bitoperations), scanning 7.6x / PopCount 67x) |
| ⑤ | SIMD implementation examples (Vector128/256) | Explicit SIMD for sum, search, and conversion. Comparing scalar, `Vector<T>`, and intrinsics | JIT-02 / BIT | ✅ Documented ([VEC-01](../README.md#-vec-01-explicit-simd-vectort--vector256), Vector256 8.9x. With guidance to prefer BCL APIs that already do this) |
| ⑤ | ref struct design built on ref fields (C# 11) | Cost comparison for holding the cursor as a ref T rather than a Span plus index | STK-01 | ❌ Iteration use moved to the rejected list (1.21x against for, so no gain) |
| ⑤ | Speeding up P/Invoke | Effects and constraints of \[LibraryImport\] plus passing Spans plus \[SuppressGCTransition\] (skipping the GC transition for short native calls) | BUF-05 | ❌ Moved to the rejected list (R-19. LibraryImport is the standard declaration, not an optimization; SuppressGCTransition shows no measurable win) |
| ⑤ | System.Threading.Channels | Producer-consumer queues. Effect of the Bounded/Unbounded and SingleReader/SingleWriter options | DSP-03 | ✅ Documented ([ASY-02](../README.md#-asy-02-producerconsumer-with-systemthreadingchannels), ~45ns/element. Bounded is 2x) |
| ⑤ | System.IO.Pipelines | I/O pipelines via PipeReader/PipeWriter. Compared against processing a Stream directly | BUF-02 | ✅ Documented conditionally ([ASY-03](../README.md#-asy-03-systemiopipelines), 1.63x on small data / 1/80 the allocation. Watch out for the 64KB deadlock) |
| ⑤ | The cost of IAsyncEnumerable | Per-element overhead of await foreach (vs IEnumerable / Channel), and the conventions around \[EnumeratorCancellation\] | SEQ-03 | ✅ Documented ([ASY-04](../README.md#-asy-04-knowing-the-cost-of-iasyncenumerable-and-when-to-use-it), being aware of the 11.6x per-element cost) |

---
