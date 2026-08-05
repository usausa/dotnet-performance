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

### R-04: Choosing the loop construct (for / while / do-while / ascending vs descending)

🎯 **Intent:** Make code faster by picking a particular loop construct or iteration direction.

📉 **Measured — why it is rejected:** All forms converge on identical performance (an exact match at size 256 and above). **A generated code check (JitDisasm) revealed the breakdown:**

- for / while: **instruction sequences match exactly (28 bytes) — no difference**. "Normalized to the same thing" holds only for these two forms
- do-while (guard + do form): bounds checks **remain** inside the loop (63 bytes) — the generated code differs, but the time is unresolvable (➖ measurement noise)
- descending for: loop cloning kicks in (85 bytes, no checks on the hot path) — again the generated code differs but the time is unresolvable (➖ measurement noise)

✅ **Do this instead:** Choose for readability (default to for / while). do-while and descending loops produce genuinely different code, so use an ascending for wherever you are relying on bounds-check elimination. What matters is not the syntax but the data access shape (MEM-01 / COL-01 / MEM-02).

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

📉 **Measured — why it is rejected:** Construction costs 15–20x that of Dictionary. Lookups can invert too, depending on the key set (measured 1.15–1.31x slower for 64 enum names). In some small-scale name resolution measurements it was never once the fastest.

✅ **Do this instead:** Adopt only when it is "built once at startup and read from then on" *and* you have confirmed a lookup win on real data (COL-02). For Type keys, the dedicated implementation (TYP-01) is about 3x faster.

---

### R-09: Using fixed pointers where Span would do

🎯 **Intent:** Beat Span with `fixed` + raw pointers.

📉 **Measured — why it is rejected:** Either the same speed as reinterpretation via `MemoryMarshal.Cast` / `Unsafe.As`, or slower by the fixed overhead. The gain does not justify the cost of introducing an unsafe context (auditing, safety).

✅ **Do this instead:** Write Span / ref based code. For reinterpretation use `MemoryMarshal.Cast` (measured zero-cost; in the BIT-04 re-measurement Cast beats fixed with non-overlapping CIs at 8 and 512 characters — precisely because no pinning is needed); for unmanaged reads and writes see SEQ-02 (struct I/O over Stream).

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

📉 **Measured — why it is rejected:** Pinning with fixed each time is essentially free in practice (0.74ns), and using the POH pointer directly (0.85ns) is no faster. POH allocation itself costs 17.5x a normal allocation and induces Gen2 GCs.

✅ **Do this instead:** Just use `fixed`. Reserve POH for avoiding GC relocation and fragmentation of long-lived I/O buffers, allocated once at startup (BUF-06 caveat).

🔗 **Measurement record:** [LAB-PinnedArray.md](../benchmarks/results/LAB-PinnedArray.md)

---

### R-14: Replacing Span.CopyTo with Unsafe.CopyBlockUnaligned

🎯 **Intent:** Speed up copies by replacing them with `Unsafe.CopyBlockUnaligned`.

📉 **Measured — why it is rejected:** At variable lengths it lands at 0.98–1.03x with overlapping CIs (➖ measurement noise — the generated code at the call site differs, but both reach the same Memmove, so no difference shows up). At the constant length of 16B, which the JIT can unroll, there is a **real difference** of 0.83x and 45B less code, but it does not justify giving up safety (no bounds checks, no type information). `Array.Copy` is the slowest and bloats code size (1.7KB).

✅ **Do this instead:** Default to `Span.CopyTo` (combined with the explicit slicing of MEM-03).

🔗 **Measurement record:** [LAB-CopyBlockUnaligned.md](../benchmarks/results/LAB-CopyBlockUnaligned.md)

---

### R-15: Touching the last element up front to steer bounds-check elimination

🎯 **Intent:** Get the JIT to eliminate bounds checks in loops driven by an external length, via a pre-access such as `_ = array[length - 1];` or an unsigned guard.

📉 **Measured — why it is rejected:** On .NET 10 there is no difference across any variant. Even on .NET 8 there is no difference for a 1024-element summation loop (the effect reported in older generations was a tiny difference limited to extremely small loops). Using `array.Length` directly in the condition also gives the smallest code (34B vs 94–140B).

✅ **Do this instead:** Rewrite the loop condition to use `array.Length` / `span.Length` directly.

🔗 **Measurement record:** [LAB-BoundsCheckHint.md](../benchmarks/results/LAB-BoundsCheckHint.md)

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

🎯 **Intent:** treat `[LibraryImport]` and `[SuppressGCTransition]` as speed optimizations for native calls (formerly documented as SYS-02).

📉 **Measured — why it is rejected:** `[LibraryImport]` is the standard way to declare P/Invoke since .NET 7, not an optimization — for a blittable signature it generates the same call as `DllImport` (1.13 vs 1.14 ns), so there is nothing to compare; adopt it as the default for its source-generated, AOT/trimming-safe marshalling. `[SuppressGCTransition]` measured **1.26x (slower)** — the plain transition already costs only ~0.06 ns over an equivalent managed call, leaving nothing for the attribute to skip. With no measurable speed win and strict correctness constraints (sub-microsecond, non-blocking, no callbacks, no exceptions; violations cause process-wide GC delays), it does not qualify as a general speed pattern.

✅ **Do this instead:** declare P/Invoke with `[LibraryImport]` as a matter of course (AOT/trimming support, not speed). Apply `[SuppressGCTransition]` only to calls that satisfy its constraints AND show a measured win in the target environment; it also halves call-site code size (70 vs 163 B), which can matter for inlining. (The benchmark and its result record were retired together with the pattern; the figures above are the final measurement.)

---

📝 Note that "extrapolating microbenchmark results directly" (a 30x difference in isolation dilutes to 1.1x in real processing) is measurement methodology rather than a technique, so it is documented as a pitfall in [benchmark-methodology.md](benchmark-methodology.md).
