# ❌ Techniques Not Adopted (Rejected Pattern Details)

[日本語](rejected-patterns.ja.md) | **English**

A record of techniques that measurement showed to be ineffective, counterproductive, or not worth the risk.
"Why it is not adopted / why it does not improve anything" is documented at the same granularity as the patterns in [README](../README.md).
Measurement environment: .NET 10 / x86-64-v4 (Ryzen AI 9 HX 370; generation-dependent items are noted individually).

---

### R-01: static readonly caching of typeof(X)

🎯 **Intent:** Avoid the evaluation cost of `typeof(X)` by pre-caching it into a `static readonly Type` field.

📉 **Measured — why it is rejected:** Identical in both speed and code size. **Generated code check (JitDisasm, Tier1): both emit the same immediate load of a frozen RuntimeType pointer (`mov rax, <ptr>; ret`, 11 bytes) — a perfect match** — confirmed no difference. If anything, before tiered-compilation promotion the static readonly version still carries a static-initialization check plus a helper call (48 bytes), so on cold paths the cached version is actually worse.

✅ **Do this instead:** Just write `typeof(X)` (favor readability). For specializing type *comparison*, see JIT-03 (typeof(T) branching).

---

### R-02: Manual ref walking (GetReference / GetArrayDataReference + Unsafe.Add)

🎯 **Intent:** eliminate in-loop bounds checks by walking memory manually with `MemoryMarshal.GetReference` / `MemoryMarshal.GetArrayDataReference` plus `Unsafe.Add`.

📉 **Measured — why it is rejected:** on net10 it is not faster in **any** shape.

- Single Span: the plain `for` loop already gets full bounds-check elimination, so the manual ref form only pays the setup cost (1.07-1.13x)
- **Walking several Spans at once** (the shape this technique was supposed to win): the JIT auto-vectorizes the indexed loop (0.36 ns/element), and manual ref walking blocks that — **1.46x slower**
- **Sequential array walk** (`GetArrayDataReference`): same mechanism, **1.30x slower**
- **Random access with a structurally guaranteed range** (array + masked index): **no difference** from the indexed form — the bounds check is effectively free
- **Sampling access** (three computed positions in a Span): the time difference is below resolution, but **the indexed form cannot eliminate one bounds check on `value[length >> 1]`** (the Tier1 code keeps an RNGCHKFAIL path: 128 B vs 115 B, 56 vs 49 instructions) — under the noise policy this is ➖ measurement noise (codegen differs)

Manual walking also has a high defect rate (several real bugs were found during verification: wrong end-ref computation, forgetting to advance one cursor).

✅ **Do this instead:** write the indexed form, `for (var i = 0; i < span.Length; i++)`. Only two legitimate uses of manual refs remain: (1) **structural** reasons — a head ref where a Span cannot be formed, or a type that stores a ref; (2) **hot-path sampling access whose range is guaranteed by construction** (it removes the bounds check the indexed form keeps; in-repo example: SampledNameTable.CalculateHash).

🔗 **Measurement record:** [LAB-DualSpanWalk.md](../benchmarks/results/LAB-DualSpanWalk.md) / [LAB-ArrayDataReference.md](../benchmarks/results/LAB-ArrayDataReference.md)

---

### R-03: Manual ref walking after CollectionsMarshal.AsSpan

🎯 **Intent:** After converting a List to a Span, go further and speed it up with a ref cursor walk.

📉 **Measured — why it is rejected:** The improvement plateaus at the AsSpan conversion (about 2x); the ref walk beyond that leaves the time unresolvable (measurement noise) while only growing code size. Rejected on the grounds of a **regression on the code-size axis** (not rejected merely because of measurement noise).

✅ **Do this instead:** Stop at `CollectionsMarshal.AsSpan(list)` + a standard for / foreach (COL-01).

---

### R-04: Choosing the loop construct (for / while / do-while / foreach / ascending vs descending)

🎯 **Intent:** Make code faster by picking a particular loop construct or iteration direction.

📉 **Measured — why it is rejected:** All forms converge on identical performance (an exact match at size 256 and above). **A generated code check (JitDisasm) revealed the breakdown:**

- for / while: **instruction sequences match exactly (28 bytes) — no difference**. "Normalized to the same thing" holds only for these two forms
- do-while (guard + do form): bounds checks **remain** inside the loop (63 bytes) — the generated code differs, but the time is unresolvable (➖ measurement noise)
- descending for: loop cloning kicks in (85 bytes, no checks on the hot path) — again the generated code differs but the time is unresolvable (➖ measurement noise)

✅ **Do this instead:** Choose for readability (default to for / while). do-while and descending loops produce genuinely different code, so use an ascending for wherever you are relying on bounds-check elimination. What matters is not the syntax but the data access shape (MEM-01 / COL-01 / MEM-02).

#### Choosing between `foreach` and `for`

The comparison above covers `for` / `while` / `do-while` / iteration direction and did not include `foreach`, so three more shapes were measured separately. The benchmark is `Lab/LoopFormBenchmark.cs`. → [Results](../benchmarks/results/R-04-LoopForm.md)

**Conclusion: `foreach` and `for` normally compile to the same instruction sequence. The only reason to deliberately reach for `for` is needing the index — and conversely, "a `for` that walks an array through a field" and "an indexed `for` over `List<T>`" are the shapes to avoid.**

**1. Array / Span / ReadOnlySpan**

| Form | Time | Code size | Generated code |
|---|---:|---:|---|
| `ArrayForeach` (baseline) | 212.4 ns | 32 B | Pointer walk, no bounds check |
| `ArrayLocalFor` | 212.6 ns | 32 B | **Instruction sequence identical to `ArrayForeach`** |
| **`ArrayFieldFor`** | **239.1 ns (1.13x)** | **67 B** | **Different code** (see below) |
| `SpanForeach` / `SpanFor` | 219.6 / 219.4 ns | 54 B | **All four forms share one instruction sequence** |
| `ReadOnlySpanForeach` / `ReadOnlySpanFor` | 219.8 / 219.9 ns | 54 B | Same as above |

What only `ArrayFieldFor` (loop condition reading `this.values.Length`) gives up — JitDisasm:

- Reloads the array reference every iteration (`mov r8,rcx`)
- **A bounds check stays inside the loop** (`cmp edx,[r8+8]` / `jae`) plus a throw block
- Re-reads `.Length` from memory for the loop condition (`cmp [rcx+8],edx`)
- Needs a stack frame (`sub rsp,28`)
- Indexed addressing `[r8+rdx*4+10]` (the foreach form uses a `add rcx,4` pointer walk)

The cause is that **the JIT cannot prove the loop body never writes to the field**. `foreach` takes the array reference once at loop entry, so it never hits this. **A `for` that hoists the reference into a local produces exactly the same code as `foreach`.**

**2. `List<T>`**

| Form | Time | Ratio | Code size |
|---|---:|---:|---:|
| `ListForeach` (baseline) | 254.8 ns | 1.00 | 71 B |
| `ListFor` | 328.8 ns | **1.29** | 72 B |
| `ListAsSpanForeach` | 216.3 ns | **0.85** | 72 B |
| `ListAsSpanFor` | 216.2 ns | **0.85** | 72 B |

The `_version` comparison in `List<T>.Enumerator` is not the thing to worry about — the enumerator is the **faster** of the two. **Both forms reload `_items` every iteration and keep a bounds check, but `ListFor` carries two** (one for Count, one for the array), and that second check is where the 1.29x comes from. This matches COL-01, which measures the same shape at 1.07x with an `int` accumulator: the indexed `for` over `List<T>` is the slowest of the four either way, and what actually matters is `CollectionsMarshal.AsSpan` (0.85x, foreach and for identical there).

**3. Large struct elements (64 bytes)**

| Form | Time | Code size |
|---|---:|---:|
| `ForeachCopy` (baseline) | 295.7 ns | 51 B |
| `ForeachRef` | 296.3 ns | 51 B |
| `ForIndexer` | 296.7 ns | 51 B |
| `ForRef` | 294.5 ns | 51 B |

**All four share one instruction sequence.** `foreach (var x in span)` emits no 64-byte copy — the body only reads `entry.Id`, so the JIT reads that field directly (`add rax,[rdx+r8]`, advancing by the element size with `add r8,40`). **"Receiving a large struct by foreach copies it" does not hold when the body only reads fields.** It does materialize if the element is **passed to a method that is not inlined**, and that shape belongs to MEM-02 / MEM-04.

✅ **Which to use:**

| Situation | Choice |
|---|---|
| Array / Span / ReadOnlySpan | **Either** (same instruction sequence). Pick for readability |
| Walking an array with `for` | Hoist the reference so the loop condition **does not re-read a field**; otherwise 1.13x |
| `List<T>` | `foreach` (the indexed `for` is 1.29x). Better still, move to `CollectionsMarshal.AsSpan` (COL-01) |
| Large struct elements | **Either** if you only read. Use `ref` when passing to a method |
| **The index is needed** | **`for`** (R-21: recovering it afterwards is 1.52x slower) |

**About how these verdicts were reached:** every "no difference" verdict rests on **instruction-sequence identity**, not on the times — the two array forms land at 212.4 / 212.6 ns and the four struct forms within 2 ns of each other, which is what identical code should look like. Only `ArrayFieldFor` (1.13x) and `ListFor` (1.29x) are decided by timing, both on non-overlapping confidence intervals.

**These two penalties are core-dependent in size, not in existence.** The previous run of this benchmark, on a Ryzen 9 5900X (x86-64-v3), put `ArrayFieldFor` at 2.20x and `ListFor` within noise; the current numbers come from a Ryzen AI 9 HX 370 (x86-64-v4), which absorbs the extra reload far better and the second bounds check far worse. **The code sizes and the instruction sequences quoted above are identical on both machines** (32 / 67 / 54 B and 71 / 72 B) — that is the part to rely on.

**Multi-dimensional arrays (`int[,]`) are out of scope.** No shipping library uses `[,]` at all (the only hit is a console program in the Work repositories), and measuring it would require suppressing CA1814, which is not worth it.

**Wrapper collections (`ReadOnlyCollection<T>`) stay on the indexer:** the .NET 10 performance post states that array interface implementations are now devirtualized, so that `foreach` over a `ReadOnlyCollection<int>` wrapping an `int[]` beats the indexer. **That did not reproduce on either x86-64-v4 or x86-64-v3**: foreach is 1.21x slower than the indexer (558.6 vs 460.0 ns; 1.26x on x86-64-v3), the 32 B enumerator allocation is still there, and the code is 664 B against 134 B. The generated code shows the devirtualization itself does reach through the wrapper's `IList<T>` field (both forms get a guarded devirtualization on `int[]`, and foreach inlines `SZGenericArrayEnumerator<int>`'s MoveNext/Current), but **the enumerator object is never stack-allocated** — `CORINFO_HELP_NEWSFAST` remains — so every element pays a reload of `_index` from the heap plus three checks. The indexer form is two guards plus one bounds check with no allocation. Keep iterating wrappers by index. → [LAB-ReadOnlyCollectionLoop.md](../benchmarks/results/LAB-ReadOnlyCollectionLoop.md)

---

### R-05: Applying ArrayPool to arrays of class elements

🎯 **Intent:** Pool the backing entry array to eliminate its allocation cost.

📉 **Measured — why it is rejected:** Even with the array itself pooled, each element object is still allocated individually, so the result ranges from no effect to counterproductive (you only add the pool management cost).

✅ **Do this instead:** Make the elements structs first, then pool the array (MEM-02 + BUF-01). struct + pooling measures at about 6x and 0B.

---

### R-06: Hand-rolled sort implementations

🎯 **Intent:** Beat the BCL with a purpose-built sort (merge sort, etc.).

📉 **Measured — why it is rejected:** The BCL's `Span.Sort` (introsort) is about 9x faster than a hand-written merge sort, at 1/5 the code size. The BCL side keeps receiving pdqsort-family optimization, leaving essentially no room to win on general-purpose comparison sorts.

✅ **Do this instead:** Use the BCL sort. Pass the comparer as a struct under a generic constraint (JIT-02).

---

### R-07: SearchValues for 2–3 candidate characters

🎯 **Intent:** Always turn search candidate characters into `SearchValues<T>` for SIMD search.

📉 **Measured — why it is rejected:** With 2–3 candidates the dedicated overloads such as `IndexOfAny(char, char)` are faster (0.885ns vs 1.494ns). SearchValues is an optimization for large candidate sets.

✅ **Do this instead:** Use the dedicated overload for a few candidates; for many (roughly 4–5 or more), cache a `SearchValues` instance in a static readonly field.

---

### R-08: Unconditional adoption of FrozenDictionary

🎯 **Intent:** Replace every read-only dictionary with `FrozenDictionary` to speed up lookups.

📉 **Measured — why it is rejected:** Construction costs 5–20x that of Dictionary, and the lookup never wins back the difference. For string keys it is 1.07x at 16 entries, level at 256, and **1.19x slower at 1024** (non-overlapping CIs) — scale makes the lookup side worse, not better. Lookups also invert depending on the key set (1.15–1.31x slower for 64 enum names), and in small-scale name resolution it was never once the fastest.

✅ **Do this instead:** Adopt only when it is "built once at startup and read from then on" *and* you have confirmed a lookup win on real data (COL-02). For Type keys, the dedicated implementation (TYP-01) is about 3x faster.

---

### R-09: Using fixed pointers where Span would do

🎯 **Intent:** Beat Span with `fixed` + raw pointers.

📉 **Measured — why it is rejected:** Either the same speed as reinterpretation via `MemoryMarshal.Cast` / `Unsafe.As`, or slower by the fixed overhead. The gain does not justify the cost of introducing an unsafe context (auditing, safety).

✅ **Do this instead:** Write Span / ref based code. For reinterpretation use `MemoryMarshal.Cast` (measured zero-cost; in the BIT-04 re-measurement Cast beats fixed with non-overlapping CIs at 8 and 512 characters — precisely because no pinning is needed); for unmanaged reads and writes see SEQ-02 (struct I/O over Stream). **Note though that `Cast` changes the length when element sizes differ and silently truncates the remainder, and performs no alignment check** (`DataMisalignedException` on Arm). Giving up `fixed` means the caller now guarantees those two things → [LAB-SpanReinterpret.md](../benchmarks/results/LAB-SpanReinterpret.md)

📌 **Dropping the pinning on a static table (confirmed while applying this):** A shape like `fixed (ushort* pTable = StaticTable)` **pins a read-only static array on every call**, and that pinning is pure overhead. In the generated code the pinned version carries **two pinned GC slots** (one for the Span as well), does an indirect static-field load plus an `add` for the array header, and zeroes both slots on return — every call.

There are two ways to remove it, and the **element width decides which**.

| Approach | When | Measured (`FormatInt32`) |
|---|---|---|
| **`MemoryMarshal.GetArrayDataReference`** + `Unsafe.Add` | Keep the read width (default choice) | 4.861 → **4.386 ns (0.90x, non-overlapping CIs)** |
| A u8 literal byte table | Only when the table was **already read byte by byte** | Replacing a `ushort` read gives 7.751 → 8.636 ns at `Int32.MaxValue` (**1.11x regression**) |

A u8 literal does have the advantage that the table address becomes a link-time constant hoisted out of the loop, but **one `ushort` read turns into two `byte` reads plus an index doubling**, and for inputs with many digits the inner loop cost outweighs it. → see "do not change the access width" under TXT-01

📌 **Checking an outside report that "pointers are faster":** an NDepend article (2026) reports "Span 0.26x / pointer 0.19x" for bulk little-endian Int32 decoding and concludes in favor of pointers. The four shapes were measured side by side over the same 1024 values.

| Form | Time | Ratio | Code size |
|---|---:|---:|---:|
| Shift/or of four bytes per element (baseline) | 718.2 ns | 1.00 | 200 B |
| `BinaryPrimitives.ReadInt32LittleEndian` per element | 364.0 ns | 0.51 | 85 B |
| **`MemoryMarshal.Cast<byte, int>` + indexed loop** | **215.2 ns** | **0.30** | **54 B** |
| `fixed` + `int*` | 249.0 ns | 0.35 | 97 B |

**`Cast` beats the pointer (the pointer is 1.16x slower, non-overlapping CIs; 2.0x on x86-64-v3).** The article's "Span 0.26x" corresponds to the `Cast` form (0.26-0.30x), and its "pointer 0.19x" did not reproduce on either machine (0.35x / 0.52x). In the generated code the `Cast` inner loop is 5 instructions (a byte offset stepped forward, remaining count `dec`), while the pointer loop is 6 — the `int` index needs a `movsxd` on the dependent chain every element — plus the stack frame for `fixed`. **Reinterpret once with `Cast` and index, and you beat the pointer** — which is this entry's conclusion. → [LAB-Int32Parse.md](../benchmarks/results/LAB-Int32Parse.md)

📌 **On the C# 16 unsafe redesign:** per the same article, C# 16 moves `unsafe` to individual members, makes pointer **types** themselves safe with only dereferences requiring an unsafe context, documents safety contracts in `/// <safety>`, and turns calls from a safe context into errors. That weakens this entry's "cost of introducing an unsafe context (auditing, safety)" rationale, but **the rejection rests on measurement (same speed or slower), which a language change does not overturn**. Revisit the wording once C# 16 is final.

🔗 **Measurement record:** [BIT-04-XxHash3.md](../benchmarks/results/BIT-04-XxHash3.md) (includes the Cast vs fixed comparison)

---

### R-10: Expecting JIT optimizations from readonly fields

🎯 **Intent:** Mark fields readonly to draw out JIT constant folding and devirtualization.

📉 **Measured — why it is rejected:** Where the call is inlined, the difference with or without readonly is unmeasurable (all variants 0.006–0.016ns). **Generated code check (JitDisasm): reading a readonly field and a normal field both compile to `mov eax, [rcx+offset]; ret` (4 bytes), identical apart from the offset — confirmed no difference** (instance readonly contributes nothing to JIT optimization).

✅ **Do this instead:** Apply readonly as a statement of design intent (immutability). For performance, use forms with demonstrated effect: sealed (DSP-01), or static readonly fields the JIT turns into constants (the token constants in TXT-04, for example).

---

### R-11: Holding delegates bound directly to static methods

🎯 **Intent:** Bind a static method straight to a delegate and hold it as a callback.

📉 **Measured — why it is rejected:** A delegate bound directly to a static method goes through a thunk that shuffles the this argument, which can make it the slowest call form of all (it alone remained about 8x slower in a situation where every other form was fully inlined).

✅ **Do this instead:** Hold an interface / sealed class implementation, or the compiler-cached lambda form (`static x => Foo(x)`) (DSP-02).

---

### R-12: ref field cursors for iteration (C# 11)

🎯 **Intent:** Speed up walking by holding the cursor in ref fields (ref T + end ref) instead of a Span plus index.

📉 **Measured — why it is rejected:** For full traversal it cannot beat a plain Span for loop (249ns/1024 elements), landing at 1.21x. Reading elements one at a time through the cursor type (repeated SpanReader.Read()) is 2.06x.

✅ **Do this instead:** Write whole-collection processing as a Span for loop. Use cursor types only for field-granularity structured reads.

🔗 **Measurement record:** [LAB-RefFieldCursor.md](../benchmarks/results/LAB-RefFieldCursor.md)

---

### R-13: Pinned (POH) buffers for performance

🎯 **Intent:** Use a resident `GC.AllocateArray(pinned: true)` buffer to avoid the cost of pinning with fixed on every call.

📉 **Measured — why it is rejected:** The rejection rests on allocation cost: POH allocation is 19.3x a normal allocation (961 vs 49.9 ns) and induces Gen1/Gen2 collections, so never allocate POH per operation. On current hardware the pre-pinned POH pointer IS measurably faster than fixed (0.015 vs 0.118 ns, code 33 vs 56 B — a real difference), but at ~0.1 ns per pin it only surfaces in pin-per-iteration hot loops, which does not justify the pattern for general use.

✅ **Do this instead:** Just use `fixed`. Reserve POH for avoiding GC relocation and fragmentation of long-lived I/O buffers, allocated once at startup (BUF-06 caveat) — in that shape the ~0.1 ns/pin saving comes along for free anyway.

🔗 **Measurement record:** [LAB-PinnedArray.md](../benchmarks/results/LAB-PinnedArray.md)

---

### R-14: Replacing Span.CopyTo with Unsafe.CopyBlockUnaligned

🎯 **Intent:** Speed up copies by replacing them with `Unsafe.CopyBlockUnaligned`.

📉 **Measured — why it is rejected:** At variable lengths it lands at 0.81–1.01x with mostly overlapping CIs (➖ measurement noise — the generated code at the call site differs, but both reach the same Memmove). At the constant lengths the JIT can unroll, the 8–16 B edge is under 0.05 ns with overlapping CIs, and **at 64 B CopyBlockUnaligned is 1.07x slower** (non-overlapping CIs) — the advantage does not survive as the size grows. The only thing that holds is code size (52–64 B vs 96–102 B), which does not justify giving up safety (no bounds checks, no type information). `Array.Copy` is the slowest and bloats code size (1.7KB).

✅ **Do this instead:** Default to `Span.CopyTo` (combined with the explicit slicing of MEM-03).

🔗 **Measurement record:** [LAB-CopyBlockUnaligned.md](../benchmarks/results/LAB-CopyBlockUnaligned.md)

---

### R-15: Touching the last element up front to steer bounds-check elimination

🎯 **Intent:** Get the JIT to eliminate bounds checks in loops driven by an external length, via a pre-access such as `_ = array[length - 1];` or an unsigned guard.

📉 **Measured — why it is rejected:** On .NET 10 there is no difference across any variant. Even on .NET 8 there is no difference for a 1024-element summation loop (the effect reported in older generations was a tiny difference limited to extremely small loops). Using `array.Length` directly in the condition also gives the smallest code (34B vs 94–140B).

✅ **Do this instead:** Rewrite the loop condition to use `array.Length` / `span.Length` directly.

🔗 **Measurement record:** [LAB-BoundsCheckHint.md](../benchmarks/results/LAB-BoundsCheckHint.md)

📌 **Where bounds checks do and do not disappear on .NET 10 (confirmed in generated code):** of the shapes collected by an outside "patterns where the check disappears" article (Zenn, 2026-04) and the .NET 10 JIT changes, the two that affect implementation decisions were checked.

| Shape | string / array | `ReadOnlySpan<char>` |
|---|---|---|
| `prefix.Length < path.Length ? path[prefix.Length] : -1` (**index taken from another sequence's Length**) | **Eliminated** (22 B, no RNGCHKFAIL) | **Kept** (48 B, `cmp/jae` + `CORINFO_HELP_RNGCHKFAIL` + a stack frame) |

**With the very same guard, only the Span keeps its bounds check.** The guard and the check are the same two-register compare, yet the JIT does not merge them for Span. The time cost is core-dependent — 1.09x on x86-64-v4 (2.05x on x86-64-v3) — but **the instruction sequences are identical on both machines**. On a hot path that indexes by another sequence's length, take a string / array instead of a Span, or derive the index from the sequence's own `Length`.

`switch (span.Length) { 4 => span[0] + span[1] + span[2] + span[3], _ => -1 }` is **check-free** from .NET 10 on, the same as the `if (span.Length == 4)` guard (32 B vs 31 B, equivalent instruction sequences). Formatting and parsing code that branches on length may use switch.

The same article's "`(uint)` cast" and "touch the last element first" shapes were measured in this entry and R-18: the check disappears in the assembly but **the time does not move**. Treat "gone from the asm" and "faster" as separate claims. → [LAB-BoundsCheckPattern.md](../benchmarks/results/LAB-BoundsCheckPattern.md)

---

### R-16: Hand-rolled digit-ordering formatting tricks (right-aligned generation → forward shift, reverse-order writing)

🎯 **Intent:** In fixed-width numeric formatting, avoid computing the digit count up front (a `Log10` equivalent) or reversing after generation, by writing right-aligned from the end of the buffer and shifting forward, or by writing forward starting from the least significant digit.

📉 **Measured — why it is rejected:** On net10, `TryFormat` + `Fill` is fastest at 5.32 ns. Hand-written LSB-first writing + Reverse is 2.51x slower, and right-aligned + forward shift is 4.79x slower. `TryFormat` is already optimized internally down to digit counting, a two-digit table, and unrolling, so a hand-written `% 10` / `/ 10` loop cannot beat it. This technique was effective in generations before `TryFormat` / `ISpanFormattable` were in place.

✅ **Do this instead:** Write with `value.TryFormat(buffer, out var written)` and `Fill(filler)` the remainder. If you need right alignment, `TryFormat` into scratch space and `CopyTo` it to the tail.

🔗 **Measurement record:** [TXT-09-FixedFieldFormat.md](../benchmarks/results/TXT-09-FixedFieldFormat.md)

---

### R-17: Emitting delegate Invoke with Call instead of Callvirt

🎯 **Intent:** Emit `Invoke` on a sealed concrete delegate type with `call` rather than `callvirt`, to save the null check and virtual dispatch cost (a technique long treated as standard practice for Emit-generated code).

📉 **Measured — why it is rejected:** Measurements were 14.2 vs 14.6 ns with overlapping CIs. Following the decision policy, **comparing the generated code with JitDisasm showed 68 instructions / 229 bytes matching exactly** — for delegate Invoke, the load of the target field (`mov rcx, [delegate+0x08]`) doubles as a hardware null check, so the JIT already removes the `callvirt` null check. The cost you set out to save never existed in the first place. Confirmed no difference (net10).

✅ **Do this instead:** Emit `callvirt` as-is (the same as Roslyn). What does pay off in Emit-generated code is eliminating child delegate chains (2.3x) and targeting a Holder field (1.5x versus a closure array) — see GEN-01.

🔗 **Measurement record:** [GEN-01-EmitStrategy.md](../benchmarks/results/GEN-01-EmitStrategy.md)

### R-18: Hand-written unsigned-overflow range checks

🎯 **Intent:** rewrite `min <= value && value <= max` as the single comparison `(uint)(value - min) <= (uint)(max - min)` to cut a branch.

📉 **Measured — why it is rejected:** 548.5 vs 553.7 ns with overlapping CIs. **Comparing the Tier1 codegen shows the two forms are effectively identical** — the only difference is the encoding (`sub r8d,100` vs `add r8d,-100`), 45 bytes either way. The net10 JIT already fuses the two-comparison form into a single unsigned comparison, so rewriting it by hand buys nothing.

✅ **Do this instead:** write the readable `(value >= min) && (value <= max)`. The manual form can still matter for compound conditions the JIT cannot prove — measure before adopting it there.

🔗 **Measurement record:** [LAB-RangeCheck.md](../benchmarks/results/LAB-RangeCheck.md)

### R-19: "Faster P/Invoke" as a pattern (LibraryImport / SuppressGCTransition)

🎯 **Intent:** treat `[LibraryImport]` and `[SuppressGCTransition]` as speed optimizations for native calls.

📉 **Measured — why it is rejected:** `[LibraryImport]` is the standard way to declare P/Invoke since .NET 7, not an optimization — for a blittable signature it generates the same call as `DllImport` (1.13 vs 1.14 ns), so there is nothing to compare; adopt it as the default for its source-generated, AOT/trimming-safe marshalling. `[SuppressGCTransition]` measured **1.26x (slower)** — the plain transition already costs only ~0.06 ns over an equivalent managed call, leaving nothing for the attribute to skip. With no measurable speed win and strict correctness constraints (sub-microsecond, non-blocking, no callbacks, no exceptions; violations cause process-wide GC delays), it does not qualify as a general speed pattern.

✅ **Do this instead:** declare P/Invoke with `[LibraryImport]` as a matter of course (AOT/trimming support, not speed). Apply `[SuppressGCTransition]` only to calls that satisfy its constraints AND show a measured win in the target environment; it also halves call-site code size (70 vs 163 B), which can matter for inlining.

---

### R-20: Ref-returning accessor via `[UnscopedRef]` (for performance)

🎯 **Goal:** Return an internal struct slot as `[UnscopedRef] public ref long GetSlot(int index)` so that a getter + setter pair collapses into a single access.

📉 **Measured / why it is rejected:** on x86-64-v4 the get/set pair measures 0.3881 ns against 0.3943 ns for the ref-returning form (**1.02x**, overlapping confidence intervals — and the ref form's own interval is seven times wider). x86-64-v3 measured 1.07x, also overlapping. The disassembly genuinely differs — the ref form folds the loop body into a single `add [r8],r10` read-modify-write — but it pays an extra `lea` for the address, so **the instruction count is identical for both forms** (9 on x86-64-v4, recorded as 7 on x86-64-v3). No axis improves on either machine.

⚠️ **The code-size figure is not evidence here.** It flips sign between machines — 85 → 88 B on x86-64-v3, 85 → **81 B** on x86-64-v4 — and the flip is **alignment padding, not code**: the get/set form carries 14 B of nops before its loop head and the ref form 9 B, leaving 71 vs 72 B of real code. The JIT's loop-head padding moves by more than 10 B between machines, so a delta of that size says nothing until the nops are subtracted or the instructions counted.

✅ **Do this instead:** write the plain getter / setter (when the storage is an `[InlineArray]`, indexed access inlines and two accesses cost the same). **`[UnscopedRef]` itself is not unnecessary** — it is required for any struct member that returns `ref this.field`, and without it the member does not compile (CS8170). What is rejected is only the motive "add it because it is faster" (see the STK-01 caveats).

🔗 **Measurement:** [LAB-ScopedRef.md](../benchmarks/results/LAB-ScopedRef.md)

---

### R-21: Recovering an index from a ref with `Unsafe.ByteOffset`

🎯 **Goal:** Iterate with `foreach (ref var item in span)` and, when an index is needed, derive it from `Unsafe.ByteOffset(ref first, ref item) / sizeof(T)` instead of carrying one.

📉 **Measured / why it is rejected:** a plain indexed `for` carrying the index measures 333.3 ns against 507.7 ns for the recovery form (**1.52x**, non-overlapping confidence intervals), and code size grows from 64 to 82 B (22 → 27 instructions). x86-64-v3 measured 1.45x with byte-identical code sizes, so **the margin grows on the newer core instead of shrinking** — the recovery adds a subtract and a shift **per element on the dependent chain**, which is work a wider core cannot hide (contrast STK-10, whose win narrows on the same machines because the work it removes is independent). Same reason as R-02: the indexed form is the shape the JIT handles best, and rewriting around refs makes the index expensive.

✅ **Do this instead:** if you need an index, use an indexed `for` and carry it. Reserve `Unsafe.ByteOffset` for cases where the distance itself is the answer (computing an offset inside a buffer, for example).

🔗 **Measurement:** [LAB-RefIdentity.md](../benchmarks/results/LAB-RefIdentity.md)

---

### R-22: A hand-written general-purpose hash table (ankerl::unordered_dense layout)

🎯 **Intent:** Replace `Dictionary<TKey, TValue>` with a hand-written table using the ankerl::unordered_dense layout — Robin Hood probing over a compact metadata array whose entries carry probe distance plus an 8-bit fingerprint, with keys and values packed densely in separate arrays — to speed up lookups. An outside article (2024) reports 4.88 us for 1024 lookups against 18.61 us for `Dictionary` (about 3.8x).

📉 **Measured — why it is rejected:** with the same 1024 string keys and the same hash function side by side, it is **slower**. Because this is the opposite of the source's "3.8x faster", it was measured on both x86-64-v4 and x86-64-v3: the sign agrees on both machines, and the gap widens on the newer core.

| Operation | `Dictionary<string, int>` | Ankerl layout | Ratio |
|---|---:|---:|---:|
| Lookup (all hits) | 3.92-3.93 us | 5.82 us | **1.48x slower** (x86-64-v3: 1.26-1.66x) |
| Lookup (all misses) | 3.38 us | 5.33 us | 1.58x slower (x86-64-v3: 1.14-1.30x) |
| Build | 6.59 us | 7.98 us | 1.21x slower (x86-64-v3: equal) |

The gap is not the hash function. `Dictionary` uses a non-randomized string hash while the Ankerl table can only call `string.GetHashCode` (randomized), so a row was added giving `Dictionary` the same randomized hash via `StringComparer.Ordinal` — Ankerl is still **1.48x slower** (5.82 vs 3.92 us, non-overlapping CIs; 1.40x on x86-64-v3 as well). **The table layout by itself does not beat .NET 10's `Dictionary`.**

The article's 3.8x comes from its `Dictionary` baseline (18.6 us per 1024 lookups); `Dictionary` does the same work in 3.9 us here (5.7-7.8 us even on x86-64-v3). **The baseline being compared against was the outlier.**

✅ **Do this instead:** Keep `Dictionary<TKey, TValue>` for general keys. For name resolution over a known key set use COL-04 (sampling hash, 0.60-0.62x of `Dictionary`); for `Type` keys use TYP-01. Hand-roll a table only when it has been measured to beat `Dictionary` on a specific key distribution; a generic layout difference will not do it.

🔗 **Measurement:** [LAB-HashTableDesign.md](../benchmarks/results/LAB-HashTableDesign.md)

---

### R-23: A small list backed by InlineArray with heap spill (SmallVec)

🎯 **Intent:** The small-vector shape — the first N elements live in an `[InlineArray]` inside the struct and move to a heap array once exceeded — to skip `List<T>`'s heap allocation while the count is small and keep working when it is not. An outside article (Qiita) reports 166% of `List<T>`'s speed while the elements fit.

📉 **Measured — why it is rejected:** It wins while it fits, as the source says, and **loses to a capacity-sized `List<T>` the moment it spills**. The asymmetry has the same sign on x86-64-v4 and x86-64-v3, with identical allocation counts.

| Elements | `List<int>` | `List<int>(capacity)` | InlineList (8 inline) |
|---:|---:|---:|---:|
| 4 (fits) | 8.8 ns / 72 B | 8.1 ns / 72 B | **4.3 ns / 0 B (0.49x)** |
| 32 (spills) | 58.5 ns / 368 B | **25.0 ns / 184 B (0.43x)** | 44.1 ns / 240 B (**1.76x behind the sized List**; x86-64-v3: 1.62x) |

Once it spills, copying the inline elements out plus two resizes costs more than one correctly sized allocation, and it allocates more (240 vs 184 B). It only wins when the upper bound is known and the inline capacity can be set to cover it — but **with a known bound the spill path is never needed, and STK-08's fixed-length InlineArray already covers that case**. With an unknown bound it loses to `List<T>(capacity)` / BUF-05. Nothing is left for it between STK-08 and BUF-05. On top of that, a by-value copy makes the inline and array forms disagree on writes, so it has to be a `ref struct` that forbids copies (the source's own caveat).

✅ **Do this instead:** STK-08 (a fixed-length `[InlineArray]` buffer) when the bound is known; `List<T>(capacity)` or the BUF-05 tiered strategy (stackalloc / ArrayPool) when it is not.

🔗 **Measurement:** [LAB-InlineList.md](../benchmarks/results/LAB-InlineList.md)

---

📝 Note that "extrapolating microbenchmark results directly" (a 30x difference in isolation dilutes to 1.1x in real processing) is measurement methodology rather than a technique, so it is documented as a pitfall in [benchmark-methodology.md](benchmark-methodology.md).
