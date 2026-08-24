# LAB-SpanReinterpret: MemoryMarshal.Cast and its traps (study queue 7-8)

- Verdict: confirms the existing recommendation; the traps are the deliverable. Re-measured on x86-64-v4 (Zen 5)
- `MemoryMarshal.Cast` is the fastest of the three on both machines: **217.5 ns**, ahead of a manual
  `Unsafe.ReadUnaligned` walk (242.2 ns, **1.11x**) and a `BinaryPrimitives` loop (375.3 ns, **1.73x**).
  On x86-64-v3 the same comparison was 243.7 / 428.3 (1.76x) / 463.2 (1.90x)
- **Correction to the earlier record: none of these loops is auto-vectorized.** The x86-64-v3 entry credited
  "bounds-check elimination and auto-vectorization"; the disassembly shows zero vector instructions in all three
  forms. A sum reduction over a dependent chain is not vectorizable here, and the real difference is the
  instruction count per element

| Form | Loop body | Per element |
|---|---|---|
| `MemoryMarshalCast` | `movsxd r10,[rcx+r8]` / `add rdx,r10` / `add r8,4` / `dec eax` / `jne` | **5 instructions**, ~1.0 cycle |
| `UnsafeReadUnaligned` | the same plus a `movsxd r10,edx` index sign-extension every iteration | **6 instructions**, ~1.2 cycles |
| `BinaryPrimitivesRead` | adds a per-element re-check of the remaining length (`lea`/`add`/`cmp`/`jbe`) | **12 instructions**, ~1.8 cycles |

- The length for the Cast form comes from the reinterpreted span itself (`shr eax,2`), so the bounds check is
  hoisted out of the loop entirely. That is the whole mechanism

**Why the margin narrowed on the newer core:** the Cast loop is already at its floor - one `add rdx,r10`
dependency per element is 1 cycle, and no wider machine can beat that. The other two carry extra *execution*
work, which a wider core absorbs. So the gap shrinks (1.76x -> 1.11x) while the ranking stays put, because the
ranking follows instruction count per element rather than any ISA feature.

## The traps (all asserted in Verify)

- **Trap 1 - silent truncation:** casting 10 bytes to `int` yields length 2. The trailing 2 bytes disappear with
  no exception
- **Trap 2 - the widening direction:** casting `int[3]` to `byte` yields length 12. This is the direction that
  carries the int overflow guard
- **Trap 3 - no alignment check:** `Cast` performs none. On Arm, `Cast<byte, double>` over an unaligned buffer
  can raise `DataMisalignedException`

## What to weigh

1. **Make `Cast` the default for reinterpreting a span.** It is the fastest form on both machines, it needs no
   `fixed` pinning, and its advantage is structural (fewer instructions per element), not a JIT accident
2. **Expect the margin to keep shrinking on newer cores, not to invert.** 1.76x -> 1.11x between two
   generations. If the manual walk is already written and correct, the case for rewriting it is weaker than the
   older number suggested - but there is no case for moving *to* the manual walk
3. **Reach for `BinaryPrimitives` when you need endianness, and know what it costs**: a length re-validation per
   element, 12 instructions against 5. Slice once and read through a Cast span where the byte order already
   matches the platform
4. **Casting transfers two guarantees to the caller** - that the trailing bytes are genuinely not needed, and
   that the buffer is aligned for the target type. Both are silent when violated, which is why they are asserted
   in Verify rather than left to review

## x86-64-v4

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| MemoryMarshalCast    | 217.5 ns | 0.65 ns | 0.94 ns | 216.1 ns | 220.0 ns | 218.6 ns |  1.00 |    0.01 |      57 B |         - |          NA |
| UnsafeReadUnaligned  | 242.2 ns | 1.41 ns | 2.06 ns | 239.7 ns | 247.7 ns | 244.9 ns |  1.11 |    0.01 |      54 B |         - |          NA |
| BinaryPrimitivesRead | 375.3 ns | 3.01 ns | 4.41 ns | 367.8 ns | 384.8 ns | 382.2 ns |  1.73 |    0.02 |     103 B |         - |          NA |
