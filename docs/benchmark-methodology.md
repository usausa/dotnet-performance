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

## 🅰️ Comparing against NativeAOT

Every measurement in this repository is taken under JIT. When a pattern's value depends on JIT-only machinery — speculative devirtualization, Dynamic PGO, tiered promotion — the JIT number does not carry over, and the only way to know is to measure. TYP-07 is the worked example: the ranking of its three variants **reverses** under NativeAOT.

Two things block a naive attempt, and both cost a full run to discover:

1. **`DisassemblyDiagnoser` is not supported on NativeAOT.** BDN rejects the job at validation and every AOT row comes out `NA` — with exit code 0, so it looks like a successful run. Since the base configuration above always carries the diagnoser, an AOT comparison needs a separate config without it (and therefore no Code Size column)
2. **`vswhere.exe` must be on `PATH`** for the ILCompiler link step (`C:\Program Files (x86)\Microsoft Visual Studio\Installer`). Without it the link fails inside `Microsoft.NETCore.Native.targets` and, again, every AOT row is `NA`

So an AOT comparison is run from a small standalone harness: copy the benchmark bodies, give them a diagnoser-free config, and supply both runtimes on the command line so the job settings are identical.

```csharp
// Diagnoser-free config for AOT comparison. Do not put a job attribute on the class -
// pass both runtimes on the command line so JIT and AOT get the same MediumRun settings.
public class AotComparisonConfig : ManualConfig
{
    public AotComparisonConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P90);
    }
}
```

```
dotnet run -c Release -- --filter "*" --runtimes net10.0 nativeaot10.0 --job medium
```

**Reading the result:** BDN computes `Ratio` against the baseline method **on the first runtime**, so every AOT row is scaled to the JIT baseline. To judge the AOT side, re-derive the ratios against the AOT run's own baseline row.

**Checking an assumption rather than a speed:** correctness tests can hide a broken assumption. TYP-07 shifts the type handle right by 3 because it is pointer-aligned; if that stopped holding, lookups would still be correct — the same shift applies at insert and at lookup — and only the bucket distribution would collapse. Assumptions like that need a direct probe, published with `PublishAot=true` and run as the native binary. Note that `dotnet run` on a project with `PublishAot=true` is **still a JIT run**: the property flips the feature switches, so `RuntimeFeature.IsDynamicCodeCompiled` already reads `false` and an AOT self-check will lie. Run `bin/Release/<tfm>/<rid>/publish/<name>.exe` directly.

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

**Equal results are not the same as equal shape.** `Verify()` cannot catch a baseline that is called differently from its variants. In LAB-ColumnMatch the baseline was a direct call that the JIT inlined while every variant went through a `Func<string,int>` per element - worth **1.9-2.2 ns per element**, more than the difference under test, and it inverted the conclusion (the alternative read as 3.0-3.6x slower when the real figure was 1.17-1.19x). Before reading any ratio, check that every variant pays the same call shape: same delegate or no delegate, same inlining outcome. The fix is to add the baseline *through the same harness* as a sixth variant, not to remove the harness.

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

### 9. Measuring a polymorphic call site only once per process

Dynamic PGO settles differently in each process, so on a **polymorphic** shape (several concrete types passing through one call site) the Tier1 code size swings by an order of magnitude and **even the sign of the ratio flips**. In TXT-11 the same code measured 0.91x, 0.97x and 1.05x across runs, and the baseline's Tier1 code size was observed anywhere from 1,777 B to 15,792 B. Either **confirm the sign across several processes** or restrict the comparison to a monomorphic shape, which stays stable under the same conditions.

### 10. Identical instruction streams can still differ 2x by placement (same code size ≠ same performance)

The decision rule "overlapping confidence intervals plus identical generated code means **no difference**" is correct, but **the reverse inference — "the code is identical, so the performance must be identical" — does not hold.** The VEC-02 verification is the counterexample: `Vector128.Shuffle` and `Ssse3.Shuffle` produce **byte-identical disassembly** (190 B, the same `vpshufb`) and still measured 121.53 vs 64.35 ns on x86-64-v3, a reproducible **1.76x**. The cause was where the hot loop landed: one fits inside a 64-byte instruction fetch window, the other straddles the boundary.

**Re-measuring the same code on a second machine settles it.** On x86-64-v4 the same two forms land 1-4% apart and the second run cannot resolve them at all — so the 121.53 ns was the anomaly, not the 64.35 ns. A placement effect does not survive a change of placement; an API effect does.

**Small spreads deserve the same suspicion.** `Unsafe.BitCast` vs `Unsafe.As` (7-7) compiles to identical code and the three concrete forms landed within 1-2% of each other — with **the ranking reversing between two processes on the same machine**, and a between-process drift of ~7% that dwarfs the within-run spread. **A few percent measured in a single process cannot be attributed to the code under test until a second process reproduces its sign.**

**How to separate the two:** when an unexpected difference appears and the disassembly matches, **add a byte-identical duplicate method and measure it**.

| Observation | Conclusion |
|---|---|
| The duplicate matches its original's time | Placement. Not an API or coding difference |
| The duplicate differs from its original | Placement as well (and unstable). Question the measurement setup |

**Swapping the declaration order does not separate them.** The JIT's code heap placement does not depend on declaration order, so reordering leaves the addresses unchanged (confirmed in VEC-02). Loop start addresses are readable from the DisassemblyDiagnoser output (`printInstructionAddresses`).

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

## 🗂️ How pattern IDs are classified

Families come in two kinds.

- **Use-case based** - TXT / COL / SEQ / ASY / DAT / GEN / TYP / SYS (grouped by what is being worked on)
- **Mechanism based** - MEM / STK / BUF / JIT / DSP / BIT / VEC / CON (grouped by the technique used)

**The rule:** if a use-case family applies, **prefer it**; otherwise place the entry by mechanism. When an entry spans **several** use-case families, keep it in the family of its representative use case rather than inventing a new destination.

**Where the lines fall** (settled by actual decisions):

| Family | What belongs here | The confusing neighbour |
|---|---|---|
| TYP | Handling **types themselves as data** (Type-keyed maps, per-type caches, casts, accessors) | Boundary with JIT, below |
| JIT | **Writing code so the JIT emits specialized output** (attributes, generic constraints, branches that get constant-folded) | Even when a type parameter is the subject, it is JIT if it steers codegen rather than handling type data |
| DSP | **How the call itself is assembled** (sealed, delegate vs interface vs function pointer, handler lists, pipelines) | The same *outcome* (devirtualization) does not merge families if the technique sits at a different layer |
| BIT | **Folding keys into a bit representation / reducing to integer arithmetic** (lightweight hashes, masks, digests) | Even when the use case is lookup, it is BIT if the folding is the subject |
| COL | Collection search, conversion, and internal access | If the key is a `Type`, it is TYP (precedent: TYP-01 / TYP-07) |
| TXT | Producing and matching strings and text | Even when the mechanism is type dispatch, it is TXT if the subject is string production (precedent: TXT-11) |

**Decisions on record:** seven entries were reviewed on 2026-08-19 and **none were moved**.

| Entry | The question | Decision |
|---|---|---|
| BIT-05 order-preserving digest | Use case is search (COL), mechanism is bit manipulation | Stays in BIT (same line as BIT-01) |
| TYP-07 hash source for Type keys | Use case is lookup (COL) | Stays in TYP (paired with TYP-01; Type-keyed lookup is TYP) |
| JIT-02 IEquatable constraint | The outcome is devirtualization (DSP) | Stays in JIT (a declaration that steers codegen) |
| JIT-03 / JIT-05 | Generic specialization (TYP) | Stay in JIT (the constant-folding trio) |
| COL-03 GetAlternateLookup | Has no result file of its own | Stays in COL (one comparative benchmark referenced by several IDs is natural) |
| BIT-01 vs COL-04 | Two IDs over one artifact | Both stay (how to build the hash vs which implementation to pick - different subjects) |
| TXT-03 Try pattern | Not a text topic | Stays in TXT (spans several use-case families, so there is no destination) |

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
| BUF-03 Time on the growth path (4 KB) | 1,283 vs 1,427 ns, **non-overlapping CIs** | Code size 4,638 vs 997 B — different code | **Real difference** (0.90x), and adopted on the allocation axis as well (8,056 B → 0 B) |
| BUF-04 Wrapper vs raw Rent/Return time | 1.63 vs 1.65 μs, overlapping ranges | — | ➖ **Measurement noise** (time axis). The wrapper cost is below measurement resolution. Adopted on the safety and allocation axes |
| COL-06 `ToImmutable` vs `MoveToImmutable` time (256 elements) | 203 vs 171 ns, **non-overlapping CIs** | Code size 2,035 vs 891 B — different code | **Real difference** (MoveToImmutable faster). At 16 elements too (14.3 vs 11.3 ns), and allocation is always halved |
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
| R-04 foreach / for (array, Span) | 212.4 vs 212.6 ns for the array pair, the four Span forms within 0.5 ns | **Identical instruction sequence** (array 32 B / Span 54 B) | ❌ **No difference**. Pick for readability |
| R-04 for walking an array through a field | 1.13x on x86-64-v4, **CIs disjoint** (2.20x on x86-64-v3) | **Different code** (bounds check kept, reference reloaded, 67 B) | ✅ **Real difference**, but its size is core-dependent. Hoist the reference into a local |
| STK-03 foreach over a monomorphic `IEnumerable<int>` (.NET 10) | ⏳provisional: on par with the array | Enumerator stack-allocated (0 B, 206 B of code) | ✅ **Premise changed**. Polymorphic goes back to 10x / 36 B |
| DSP-04 loop-scoped capture (.NET 10) | ⏳❗unexpected (the official post says elided), pending the final run | **Not elided** (still 88 B per iteration) | ❌ **Guidance unchanged**. Only static + TState reaches 0 B |
| R-09 Int32 decoding: Cast vs pointer | ⏳❗unexpected (source: pointer 0.19), pending the final run: Cast 0.26 / pointer 0.52 | Cast 54 B / pointer 97 B | ✅ **Cast is 2x faster**. The outside pointer advantage did not reproduce |
| R-15 index from another sequence's Length | ⏳provisional: Span 2.05x | Eliminated for string/array (22 B), **kept for Span** (48 B, RNGCHKFAIL) | ✅ **Asymmetry confirmed**. Take string/array on hot paths |
| R-22 Ankerl layout vs Dictionary | ⏳❗unexpected (source: 3.8x faster), pending the final run: 1.26-1.66x slower (1.40x with the same hash) | — | ❌ **Rejected**. An outlier baseline is behind the 3.8x |
| STK-08 InlineList (fits / spills) | ⏳❗unexpected (the spill loss is not in the source), pending the final run: 0.35x / loses 1.62x to a sized List | 0 B / 240 B | ⚠ **Conditional**. Only with a known upper bound |
| R-04 indexed for over `List<T>` | 1.29x on x86-64-v4, **CIs disjoint** (within noise on x86-64-v3) | **Different code** (the indexer form keeps **two** bounds checks per step, Count then the array, 72 vs 71 B) | ✅ **Real difference**, size core-dependent. Prefer foreach, or `CollectionsMarshal.AsSpan` (0.85x) |
| R-10 Instance readonly field | 0.006-0.016 ns, below the measurable range | The load is **identical apart from the offset** (4 B) | ❌ **No difference** (instance readonly contributes nothing to JIT optimization) |
| R-14 Replacing copies with CopyBlockUnaligned | Variable length 0.92-1.01x at 512 B+; constant 8 B 0.89x / 16 B 0.94x - all with overlapping CIs. At constant 64 B it is **1.07x slower** (non-overlapping) | The call shape differs (52-64 B vs 96-102 B), but both **reach the same Memmove** | ➖ **Measurement noise** where CIs overlap, and the sign reverses by 64 B. Code size is the only surviving advantage, which does not outweigh the safety loss |
| 7-1 `scoped` on span / ref parameters | 60.50 vs 60.83 ns / 1.511 vs 1.516 ns, CIs overlap (x86-64-v3: 69.22 vs 70.00 / 2.276 vs 2.296) | Caller and callee **instruction streams identical, and byte for byte the same figures on both machines** (89 / 35 B, 50 / 38 B) | ❌ **No difference** (a pure compile-time contract, ISA independent; documented in STK-01 as a safety tool) |
| 7-1 `[UnscopedRef]` ref-returning accessor | 0.3881 vs 0.3943 ns, CIs overlap (x86-64-v3: 0.5013 vs 0.5349) | **Different code**, same instruction count. Code size **flips sign between machines** (85 → 88 B on v3, 85 → 81 B on v4) and the flip is alignment padding: 71 vs 72 B of real code | ❌ **Rejected** (no axis improves on either machine → R-20) |
| 7-7 `Unsafe.BitCast` vs `Unsafe.As` | Within 1-2% of each other, and **the ranking reverses between two runs on the same machine** (`Unsafe.As` slowest at 222.9 ns in run 1, fastest at 230.5 ns in run 2). Between-process drift (~7%) exceeds the within-run spread | **Instruction streams identical** (57 B x3 concrete, 21 B x2 generic, both machines) | ❌ **No difference** — but **adopted on the safety axis** (identical code proves the switch is free) |
| 7-3 `GetValueRefOrNullRef` read path, **8 B value** | 0.99-1.04x, CIs overlap on both machines (a 1.04x with a 6 ns overlap did not survive re-measurement) | Identical instruction counts (200 vs 200); only basic-block placement differs | ❌ **No difference** for a single-field read |
| 7-3 `GetValueRefOrNullRef` read path, **32 B value / 2 fields read** | 0.93x and 0.94x on x86-64-v4, **CIs disjoint in both runs**; 1.00x on x86-64-v3 | 202 vs 201 instructions: the ref form folds each field read into `add rsi,[r13]` where `TryGetValue` emits `mov` + `add` | ✅ **Real** — the saving is one instruction per field read, so it resolves only when the loop reads several fields and the core is wide enough to expose it → COL-07 |
| 7-9 `Vector128.Shuffle` vs `Ssse3.Shuffle` | 121.53 vs 64.35 ns on x86-64-v3, **CIs disjoint**. On x86-64-v4 the same comparison is 62.51 vs 61.06 (run 1) and 64.69 vs 62.21 ns (run 2, **CIs overlap**) | **Instruction streams identical** (59 instructions / 190 B, both machines) | ⚠️ **Placement confirmed**, not an API difference. A second machine collapsed the 1.76x to 1-4%, so the 121.53 ns was the anomaly (see pitfall 10) |

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
| ⑥ | net11 generation watch | Re-measure after net11 GA: (1) enum boxing through Equals disappears via JIT specialization (add a generation note to STK-05's implicit-boxing list), (2) LINQ Min/Max vectorization (reinforces VEC-01's prefer-BCL-APIs guidance) | STK-05 / VEC-01 | ⏳ Waiting for net11 GA |
| ⑦ | `scoped` / `[UnscopedRef]` (C# 11) | Does it show in codegen? Does a ref-returning accessor beat a get/set pair? | STK-01 | ❌ No difference / rejected (R-20). `scoped` documented in STK-01 |
| ⑦ | ref field cursor for structured reads | Does it beat the indexed form in the field-granular shape R-12 named? | STK-01 / R-12 | ✅ Adopted ([STK-10](../README.md), 0.81x on x86-64-v4 / 0.75x on v3, identical codegen) |
| ⑦ | `GetValueRefOrNullRef` + `IsNullRef` | Can an existence-checked update collapse into one probe? | COL-01 | ✅ Adopted ([COL-07](../README.md), update 0.44-0.59x. Read path: no difference for a single-field read, 0.93x for a 32 B value read through two fields on x86-64-v4) |
| ⑦ | Struct layout (size and field order) | Does 32 → 24 bytes show in traversal and argument passing? | MEM-02 / MEM-04 | ✅ Conditionally adopted ([MEM-05](../README.md)). The condition is a cache-capacity boundary, not the byte count: scattered 0.67-0.71x on x86-64-v3 (512 KB vs 384 KB across a 512 KB L2) and no difference on x86-64-v4 (1 MB L2). By-value passing reverses between the machines |
| ⑦ | False sharing / cache line padding | How large is the penalty? Is 64 bytes enough? | CON-01 | ✅ Adopted ([CON-03](../README.md), 4-29.7x). Padding is unconditional, the size is not: 128 bytes required on x86-64-v3, 64 bytes sufficient on v4 — pick by prefetch granularity |
| ⑦ | `Memory<T>` `.Span` cost and array interop | Hoisting, backing-store dependence, `TryGetArray` | BUF-04 | ✅ Adopted ([BUF-08](../README.md), 2.96-3.11x per element; the cost is per `.Span` resolution, so 1 per 16-byte chunk is only 1.05x) |
| ⑦ | `Unsafe.BitCast` (.NET 8+) | Does it cost anything over `Unsafe.As<TFrom,TTo>`? | JIT-03 / SEQ-02 | ✅ Adopted (identical codegen → made the safe default in TYP-05 / JIT-03 / SEQ-02) |
| ⑦ | `MemoryMarshal.Cast` cost and traps | Against manual reinterpretation; truncation and alignment | BIT-04 / R-09 | ✅ Adopted as notes (quick reference, BIT-04, R-09 conditions) |
| ⑦ | Fixed-width intrinsics (shuffle) | Effect of a lane permutation `Vector<T>` cannot express | VEC-01 | ✅ Adopted ([VEC-02](../README.md), 0.29x on x86-64-v4). The raw-ISA advantage is disproved as placement — confirmed again on a second machine, where the 1.76x gap collapses to 1-4% |
| ⑦ | `MemoryManager<T>` / `NativeMemory` | Cost of exposing an unmanaged region as `Memory<T>` | BUF-04 / R-13 | ✅ Adopted (lives in BUF-08) |
| ⑦ | `AreSame` / `ByteOffset` / `Overlaps` | Index recovery from a ref; cost of alias checking | R-02 | ❌ Index recovery rejected (R-21). Alias checking moved to the quick reference |
| ⑦ | `Unsafe.Unbox<T>` | Can an existing box be updated without reallocating? | STK-05 | ✅ Adopted (STK-05 extension, 0.17-0.18x and zero allocation; the ratio is machine independent because what is removed is an allocation) |
| ⑦ | `MemoryMarshal.TryGetArray` | Copy-free bridge to `byte[]`-based APIs | BUF-04 | ✅ Adopted (lives in BUF-08, 4,120 → 0 B allocated) |
| ⑧ | Normalising the probe to enable an ordinal switch (column-name matching) | Does upper-casing the probe first beat `Equals(OrdinalIgnoreCase)` / the sampling-hash switch? | TXT-10 / GEN-02 | ⚠️ **Split verdict.** The conversion is **rejected** - it loses in 12 of 12 conditions (2.0-3.0x at 8 columns, 1.35-2.91x at 24) and costs 2.7-3.2 ns per column. For the match itself the crossover is the column count: the chain wins at 8 (the switch is 1.17-1.19x) and the **un-converted** ordinal switch wins at 24 (0.91x, code 2,212 vs 3,261 B). Measured like for like only after adding a harness-matched baseline - see pitfall 3 → [LAB-ColumnMatch](../benchmarks/results/LAB-ColumnMatch.md) |

---
