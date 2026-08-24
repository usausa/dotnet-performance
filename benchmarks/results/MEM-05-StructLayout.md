# MEM-05: Struct layout (field order, padding, LayoutKind)

- Verdict: **conditional. Both axes of this pattern are microarchitecture dependent, and they can point in
  opposite directions for the same type** - the sections below give the mechanism for each and what to weigh
  when choosing a size
- Measured on x86-64-v4 (Zen 5), twice. The x86-64-v3 (Zen 3) run that first produced this entry reached a
  different answer on both axes, and the difference is explainable in both cases
- Sizes confirmed by Verify: Sequential/padded declaration order 32 B, Sequential/wide-first 24 B,
  LayoutKind.Auto 24 B. C# emits Sequential by default, so the padded declaration order is what you get unless
  you reorder the fields wide-first or mark the type [StructLayout(LayoutKind.Auto)]. Both reach 24 B
- Generated code is identical for all three layouts (65 B sequential / 93 B scattered) on both machines. Any
  difference is data side, not code side, so the methodology's "identical code means no difference" rule does
  not apply here

## Axis 1 - array traversal: the payoff is a cache-capacity boundary, not the byte count

| | x86-64-v3 (Zen 3, 512 KB L2/core) | x86-64-v4 (Zen 5, 1 MB L2/core) |
|---|---|---|
| Padded (32 B) | 26,213 ns | 14,061 / 14,088 ns |
| Packed (24 B) | 18,516 ns (**0.71x**) | 13,875 / 14,014 ns (0.99x) |
| Auto (24 B) | 17,662 ns (**0.67x**) | 14,031 / 14,013 ns (1.00x) |
| CIs | disjoint | **overlap in both runs** |

**Why it moves:** the array is 16,384 elements - **512 KB padded, 384 KB packed**. On Zen 3 that boundary falls
exactly on the 512 KB per-core L2, so only the packed form stays resident, and the 0.67-0.71x is that residency
difference rather than the 8 bytes themselves. On Zen 5 the L2 is 1 MB per core, **both forms fit**, and the
difference disappears (measured twice, 0.987x and 0.995x, CIs overlapping both times). Sequential traversal is
a tie on both machines (0.98-1.00x) because the prefetcher absorbs the footprint difference either way.

**What to weigh:** compute `element count x size` against the **target machine's per-core L2 (and L3 for shared
workloads)**. The shrink pays when it moves the working set from one side of a capacity boundary to the other,
and pays nothing when both sizes land on the same side. A working set far above or far below the boundary gets
no benefit on any machine - which also means a benchmark sized to straddle the boundary will overstate the
pattern for workloads that do not.

## Axis 2 - by-value argument passing: the copy shape is fixed by size, the winner by the microarchitecture

| | x86-64-v3 | x86-64-v4 run 1 | x86-64-v4 run 2 |
|---|---|---|---|
| Padded (32 B) | 2.034 ns | 1.292 ns | 1.301 ns |
| Packed (24 B) | 1.854 ns (**0.91x**) | 1.535 ns (**1.19x**) | 1.533 ns (**1.18x**) |

CIs are disjoint in all three runs - the machines disagree on the sign, not on the resolution.

```asm
ByValuePadded (32 B)   vmovdqu ymm0,[src] / vmovdqu [dst],ymm0                  ; 2 instructions
ByValuePacked (24 B)   vmovdqu xmm0,[src] / vmovdqu [dst],xmm0
                       mov rcx,[src+16]   / mov [dst+16],rcx                    ; 4 instructions
```

**Why it moves:** the JIT picks the copy shape from the size alone - **24 bytes is not a multiple of the widest
vector move, so the copy splits** into a 16 B vector move plus an 8 B scalar pair, while 32 B copies in one
`vmovdqu ymm` pair. That choice is identical on both machines (102 vs 118 B of code). What differs is execution:
Zen 5 retires the single wide pair faster, Zen 3 measured the split form faster by the same order of magnitude.
The absolute size of the effect is ~0.24 ns per call on either machine.

**What to weigh:**

1. **Aim for a size that is a multiple of the widest vector move (16 / 32 B), not for the smallest size**, when
   the type is passed by value on a hot path. Shaving a struct to 24 B buys a 4-instruction copy in place of a
   2-instruction one on every call, in exchange for 8 bytes
2. **Check which axis your type actually lives on.** Array element traversed in bulk -> axis 1, and smaller can
   win. Hot by-value parameter -> axis 2, and aligned-larger can win. Here the same type is better at 24 B as an
   array element on Zen 3 and worse at 24 B as an argument on Zen 5
3. **When both axes apply, size for the one your workload does more of**, and treat the other as the cost. The
   two effects are of very different magnitudes: axis 1 was worth 1.4x on the machine where it applied, axis 2
   is worth ~0.2 ns per call, so a high call rate is needed before axis 2 outweighs a residency win
4. If the target microarchitecture is unknown, the aligned size is the more durable choice on axis 2 - a single
   wide move stays a single move as vector widths grow, and its worst measured cost here was 10% of a
   sub-nanosecond call
5. Field ordering and `LayoutKind.Auto` are free in themselves and remove the padding, so they stay the default
   way to reach whichever size you chose. What is conditional is the target size, not the practice

## x86-64-v4 run 1 (full)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean          | Error       | StdDev      | Min           | Max           | P90           | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |--------------:|------------:|------------:|--------------:|--------------:|--------------:|------:|--------:|----------:|----------:|------------:|
| SequentialPadded | 13,185.147 ns |  42.1165 ns |  61.7338 ns | 13,069.684 ns | 13,312.810 ns | 13,253.303 ns | 1.000 |    0.01 |      65 B |         - |          NA |
| SequentialPacked | 13,179.888 ns |  61.4065 ns |  86.0833 ns | 13,049.985 ns | 13,353.877 ns | 13,287.347 ns | 1.000 |    0.01 |      65 B |         - |          NA |
| SequentialAuto   | 13,115.552 ns |  29.2703 ns |  41.0329 ns | 13,033.139 ns | 13,214.775 ns | 13,154.005 ns | 0.995 |    0.01 |      65 B |         - |          NA |
| ScatteredPadded  | 14,061.099 ns |  93.4316 ns | 130.9780 ns | 13,844.534 ns | 14,355.681 ns | 14,223.186 ns | 1.066 |    0.01 |      93 B |         - |          NA |
| ScatteredPacked  | 13,874.847 ns |  95.1978 ns | 130.3079 ns | 13,732.797 ns | 14,167.426 ns | 14,026.061 ns | 1.052 |    0.01 |      93 B |         - |          NA |
| ScatteredAuto    | 14,030.893 ns | 130.0888 ns | 194.7108 ns | 13,757.767 ns | 14,539.690 ns | 14,220.731 ns | 1.064 |    0.02 |      93 B |         - |          NA |
| ByValuePadded    |      1.292 ns |   0.0140 ns |   0.0210 ns |      1.234 ns |      1.322 ns |      1.315 ns | 0.000 |    0.00 |     102 B |         - |          NA |
| ByValuePacked    |      1.535 ns |   0.0083 ns |   0.0121 ns |      1.510 ns |      1.554 ns |      1.550 ns | 0.000 |    0.00 |     118 B |         - |          NA |
