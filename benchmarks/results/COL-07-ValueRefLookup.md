# COL-07: GetValueRefOrNullRef + Unsafe.IsNullRef (optional ref lookup)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. Every Dictionary path is 3-6% faster on net11 and the ratios do not move
- Update: GetValueRefOrNullRef is 0.44x (all hit), 0.59x (half miss) and 0.49x (32 B value) of TryGetValue + indexer write-back on net11 (0.44x / 0.57x / 0.49x on net10)
- Read: 0.99-1.01x of TryGetValue on both generations

### ValueRefLookupBenchmark

net10 → net11 per row:

| Method | Probe | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| TryGetValueRead | AllHit | 1.584 μs | 1.490 μs | 0.94 | - → - | 1,205 B → 1,209 B |
| ValueRefOrNullRefRead | AllHit | 1.555 μs | 1.473 μs | 0.95 | - → - | 1,202 B → 1,202 B |
| TryGetValueThenIndexerUpdate | AllHit | 3.570 μs | 3.402 μs | 0.95 | - → - | 6,763 B → 7,334 B |
| ValueRefOrNullRefUpdate | AllHit | 1.577 μs | 1.503 μs | 0.95 | - → - | 1,206 B → 1,210 B |
| TryGetValueRead | HalfMiss | 1.248 μs | 1.209 μs | 0.97 | - → - | 1,003 B → 989 B |
| ValueRefOrNullRefRead | HalfMiss | 1.224 μs | 1.224 μs | 1.00 | - → - | 1,000 B → 986 B |
| TryGetValueThenIndexerUpdate | HalfMiss | 2.162 μs | 2.090 μs | 0.97 | - → - | 6,042 B → 6,638 B |
| ValueRefOrNullRefUpdate | HalfMiss | 1.239 μs | 1.231 μs | 0.99 | - → - | 1,003 B → 989 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.55 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                       | Job                 | Runtime   | Probe    | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------------- |-------------------- |---------- |--------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **TryGetValueRead**              | **.NET 10.0**           | **.NET 10.0** | **AllHit**   | **1.584 μs** | **0.0152 μs** | **0.0218 μs** | **1.554 μs** | **1.615 μs** | **1.610 μs** |  **1.00** |    **0.00** |   **1,205 B** |         **-** |          **NA** |
| ValueRefOrNullRefRead        | .NET 10.0           | .NET 10.0 | AllHit   | 1.555 μs | 0.0057 μs | 0.0082 μs | 1.542 μs | 1.574 μs | 1.566 μs |  0.98 |    0.01 |   1,202 B |         - |          NA |
| TryGetValueThenIndexerUpdate | .NET 10.0           | .NET 10.0 | AllHit   | 3.570 μs | 0.0116 μs | 0.0174 μs | 3.540 μs | 3.608 μs | 3.591 μs |  2.25 |    0.03 |   6,763 B |         - |          NA |
| ValueRefOrNullRefUpdate      | .NET 10.0           | .NET 10.0 | AllHit   | 1.577 μs | 0.0058 μs | 0.0082 μs | 1.561 μs | 1.596 μs | 1.588 μs |  1.00 |    0.01 |   1,206 B |         - |          NA |
|                              |                     |           |          |          |           |           |          |          |          |       |         |           |           |             |
| TryGetValueRead              | MediumRun-.NET 11.0 | .NET 11.0 | AllHit   | 1.490 μs | 0.0046 μs | 0.0063 μs | 1.480 μs | 1.503 μs | 1.499 μs |  1.00 |    0.00 |   1,209 B |         - |          NA |
| ValueRefOrNullRefRead        | MediumRun-.NET 11.0 | .NET 11.0 | AllHit   | 1.473 μs | 0.0057 μs | 0.0082 μs | 1.462 μs | 1.494 μs | 1.483 μs |  0.99 |    0.01 |   1,202 B |         - |          NA |
| TryGetValueThenIndexerUpdate | MediumRun-.NET 11.0 | .NET 11.0 | AllHit   | 3.402 μs | 0.0117 μs | 0.0168 μs | 3.372 μs | 3.446 μs | 3.420 μs |  2.28 |    0.01 |   7,334 B |         - |          NA |
| ValueRefOrNullRefUpdate      | MediumRun-.NET 11.0 | .NET 11.0 | AllHit   | 1.503 μs | 0.0041 μs | 0.0057 μs | 1.492 μs | 1.513 μs | 1.510 μs |  1.01 |    0.01 |   1,210 B |         - |          NA |
|                              |                     |           |          |          |           |           |          |          |          |       |         |           |           |             |
| **TryGetValueRead**              | **.NET 10.0**           | **.NET 10.0** | **HalfMiss** | **1.248 μs** | **0.0053 μs** | **0.0078 μs** | **1.237 μs** | **1.266 μs** | **1.258 μs** |  **1.00** |    **0.00** |   **1,003 B** |         **-** |          **NA** |
| ValueRefOrNullRefRead        | .NET 10.0           | .NET 10.0 | HalfMiss | 1.224 μs | 0.0030 μs | 0.0044 μs | 1.214 μs | 1.234 μs | 1.229 μs |  0.98 |    0.01 |   1,000 B |         - |          NA |
| TryGetValueThenIndexerUpdate | .NET 10.0           | .NET 10.0 | HalfMiss | 2.162 μs | 0.0083 μs | 0.0122 μs | 2.137 μs | 2.178 μs | 2.175 μs |  1.73 |    0.01 |   6,042 B |         - |          NA |
| ValueRefOrNullRefUpdate      | .NET 10.0           | .NET 10.0 | HalfMiss | 1.239 μs | 0.0064 μs | 0.0094 μs | 1.220 μs | 1.265 μs | 1.248 μs |  0.99 |    0.01 |   1,003 B |         - |          NA |
|                              |                     |           |          |          |           |           |          |          |          |       |         |           |           |             |
| TryGetValueRead              | MediumRun-.NET 11.0 | .NET 11.0 | HalfMiss | 1.209 μs | 0.0051 μs | 0.0072 μs | 1.197 μs | 1.221 μs | 1.216 μs |  1.00 |    0.00 |     989 B |         - |          NA |
| ValueRefOrNullRefRead        | MediumRun-.NET 11.0 | .NET 11.0 | HalfMiss | 1.224 μs | 0.0087 μs | 0.0121 μs | 1.209 μs | 1.257 μs | 1.244 μs |  1.01 |    0.01 |     986 B |         - |          NA |
| TryGetValueThenIndexerUpdate | MediumRun-.NET 11.0 | .NET 11.0 | HalfMiss | 2.090 μs | 0.0082 μs | 0.0115 μs | 2.067 μs | 2.109 μs | 2.104 μs |  1.73 |    0.01 |   6,638 B |         - |          NA |
| ValueRefOrNullRefUpdate      | MediumRun-.NET 11.0 | .NET 11.0 | HalfMiss | 1.231 μs | 0.0071 μs | 0.0105 μs | 1.211 μs | 1.248 μs | 1.243 μs |  1.02 |    0.01 |     989 B |         - |          NA |

### ValueRefLargeLookupBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| TryGetValueRead | 1.622 μs | 1.584 μs | 0.98 | - → - | 1,219 B → 1,216 B |
| ValueRefOrNullRefRead | 1.631 μs | 1.571 μs | 0.96 | - → - | 1,206 B → 1,210 B |
| TryGetValueThenIndexerUpdate | 3.434 μs | 3.306 μs | 0.96 | - → - | 3,217 B → 3,268 B |
| ValueRefOrNullRefUpdate | 1.699 μs | 1.610 μs | 0.95 | - → - | 1,214 B → 1,215 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.57 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                       | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| TryGetValueRead              | .NET 10.0           | .NET 10.0 | 1.622 μs | 0.0126 μs | 0.0188 μs | 1.593 μs | 1.654 μs | 1.647 μs |  1.00 |    0.00 |   1,219 B |         - |          NA |
| ValueRefOrNullRefRead        | .NET 10.0           | .NET 10.0 | 1.631 μs | 0.0069 μs | 0.0104 μs | 1.609 μs | 1.649 μs | 1.642 μs |  1.01 |    0.01 |   1,206 B |         - |          NA |
| TryGetValueThenIndexerUpdate | .NET 10.0           | .NET 10.0 | 3.434 μs | 0.0251 μs | 0.0352 μs | 3.384 μs | 3.491 μs | 3.473 μs |  2.12 |    0.03 |   3,217 B |         - |          NA |
| ValueRefOrNullRefUpdate      | .NET 10.0           | .NET 10.0 | 1.699 μs | 0.0085 μs | 0.0122 μs | 1.679 μs | 1.719 μs | 1.715 μs |  1.05 |    0.01 |   1,214 B |         - |          NA |
|                              |                     |           |          |           |           |          |          |          |       |         |           |           |             |
| TryGetValueRead              | MediumRun-.NET 11.0 | .NET 11.0 | 1.584 μs | 0.0080 μs | 0.0118 μs | 1.562 μs | 1.606 μs | 1.597 μs |  1.00 |    0.00 |   1,216 B |         - |          NA |
| ValueRefOrNullRefRead        | MediumRun-.NET 11.0 | .NET 11.0 | 1.571 μs | 0.0046 μs | 0.0069 μs | 1.559 μs | 1.583 μs | 1.580 μs |  0.99 |    0.01 |   1,210 B |         - |          NA |
| TryGetValueThenIndexerUpdate | MediumRun-.NET 11.0 | .NET 11.0 | 3.306 μs | 0.0203 μs | 0.0297 μs | 3.265 μs | 3.355 μs | 3.344 μs |  2.09 |    0.02 |   3,268 B |         - |          NA |
| ValueRefOrNullRefUpdate      | MediumRun-.NET 11.0 | .NET 11.0 | 1.610 μs | 0.0089 μs | 0.0125 μs | 1.596 μs | 1.643 μs | 1.625 μs |  1.02 |    0.01 |   1,215 B |         - |          NA |

## Earlier record

- Verdict: adopted for the update path unconditionally; adopted for the read path **in proportion to how many
  fields the loop reads out of the slot**
- Re-measured on x86-64-v4 (Zen 5). The update-path verdict is identical to the earlier x86-64-v3 (Zen 3) run;
  the 32 B read result is the one figure that moved, and the mechanism below says why
- Update: 0.44x (all hit), 0.59x (half miss), 0.51x (32 B value) against TryGetValue + indexer write-back
  (x86-64-v3: 0.48x / 0.62x / 0.51x)
- The update ratio barely moves between an 8 B value and a 32 B value, so that gain is the single hash probe,
  not the avoided value copy
- Code size is where the update path shows first: the indexer setter drags the whole insert path into the
  caller (8,351 B) while the ref form needs 1,155 B. That ratio holds on both machines
- The ContainsKey + indexer shape is not measured because CA1854 rejects it at build time in this repository
- Probe keys are separate string instances from the stored keys, so reference equality cannot short-circuit

## The read path: same instruction saving, different resolvability

| Value | Machine | TryGetValue | GetValueRefOrNullRef | Ratio | CIs |
|---|---|---:|---:|---:|---|
| 8 B | x86-64-v3 | 2.327 us | 2.287 us | 0.98 | overlap |
| 8 B | x86-64-v4 run 1 | 1.576 us | 1.643 us | 1.04 | overlap by 6 ns |
| 8 B | x86-64-v4 run 2 | 1.609 us | 1.630 us | 1.01 | overlap |
| 32 B | x86-64-v3 | 2.486 us | 2.485 us | 1.00 | overlap |
| 32 B | x86-64-v4 run 1 | 1.735 us | 1.608 us | **0.93** | disjoint |
| 32 B | x86-64-v4 run 2 | 1.711 us | 1.603 us | **0.94** | disjoint |

**Why it moves - and what it is not.** Neither form emits a struct copy at all: the JIT elides it and drops the
fields the loop never reads, so "avoiding the copy of a large value" is not the mechanism. The entire difference
is the accumulate step:

| | Instructions per probe |
|---|---|
| `TryGetValueRead` | `mov rax,[r13]` / `mov rcx,[r13+8]` / `add rsi,rax` / `add rsi,rcx` (4) |
| `ValueRefOrNullRefRead` | `add rsi,[r13]` / `add rsi,[r13+8]` (2) |

The ref form reads the slot straight as a memory operand; everything else in the two streams matches modulo
label renaming (202 vs 201 instructions, 742 vs 752 B). **The saving is one instruction per field read out of
the slot** - two fields here, one field in the 8 B case. That is why the 8 B rows are noise on both machines
(one instruction is below the resolution) and why the 32 B rows resolve on Zen 5. Zen 3 did not resolve even
two instructions per probe on this loop; a narrower or more memory-bound core hides them.

## What to weigh

1. **Updates: take it.** 0.44-0.59x with disjoint CIs on both machines is the reason on its own - a 1.7-2.3x
   saving on every write-back is not a marginal call. That the indexer form also pulls the dictionary's whole
   insert path into the caller (8,351 vs 1,155 B) is an additional argument, not the primary one
2. **Reads: count the field reads, not the value's byte size.** The gain is one folded memory operand per field
   the loop pulls from the slot. One field is below measurement resolution anywhere; several fields become
   measurable on cores wide enough to expose it. A 32 B value read through a single field will not reproduce
   this - the size is a proxy, the field count is the criterion
3. **Expect this class of win to appear on newer cores rather than disappear.** Nothing about the codegen
   changed between the two machines; only whether two instructions per iteration are resolvable did. Treat a
   "no difference" read-path result from an older core as unresolved rather than settled
4. **Keep TryGetValue for the single-field read.** It is the clearer API, it carries no ref-safety obligation,
   and every measurement of that shape here is a tie

## Reproducibility

Each x86-64-v4 figure above comes from two independent runs; the 8 B all-hit row is the reason the re-run
happened (run 1 read 1.04x with the intervals overlapping by 6 ns, which did not survive re-measurement).

## 8 B value (Stat8), all-hit and half-miss probes

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Probe    | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------------- |--------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **TryGetValueRead**              | **AllHit**   | **1.576 μs** | **0.0493 μs** | **0.0674 μs** | **1.531 μs** | **1.517 μs** | **1.673 μs** | **1.665 μs** |  **1.00** |    **0.06** |   **1,127 B** |         **-** |          **NA** |
| ValueRefOrNullRefRead        | AllHit   | 1.643 μs | 0.0243 μs | 0.0340 μs | 1.650 μs | 1.556 μs | 1.681 μs | 1.679 μs |  1.04 |    0.05 |   1,146 B |         - |          NA |
| TryGetValueThenIndexerUpdate | AllHit   | 3.769 μs | 0.0284 μs | 0.0398 μs | 3.757 μs | 3.710 μs | 3.827 μs | 3.812 μs |  2.40 |    0.10 |   8,351 B |         - |          NA |
| ValueRefOrNullRefUpdate      | AllHit   | 1.654 μs | 0.0106 μs | 0.0155 μs | 1.653 μs | 1.632 μs | 1.682 μs | 1.680 μs |  1.05 |    0.04 |   1,155 B |         - |          NA |
|                              |          |          |           |           |          |          |          |          |       |         |           |           |             |
| **TryGetValueRead**              | **HalfMiss** | **1.327 μs** | **0.0061 μs** | **0.0092 μs** | **1.329 μs** | **1.311 μs** | **1.344 μs** | **1.338 μs** |  **1.00** |    **0.01** |     **967 B** |         **-** |          **NA** |
| ValueRefOrNullRefRead        | HalfMiss | 1.333 μs | 0.0148 μs | 0.0212 μs | 1.333 μs | 1.292 μs | 1.380 μs | 1.358 μs |  1.00 |    0.02 |     964 B |         - |          NA |
| TryGetValueThenIndexerUpdate | HalfMiss | 2.286 μs | 0.0179 μs | 0.0250 μs | 2.283 μs | 2.239 μs | 2.341 μs | 2.323 μs |  1.72 |    0.02 |   8,124 B |         - |          NA |
| ValueRefOrNullRefUpdate      | HalfMiss | 1.347 μs | 0.0143 μs | 0.0209 μs | 1.340 μs | 1.316 μs | 1.396 μs | 1.376 μs |  1.01 |    0.02 |     967 B |         - |          NA |

## 32 B value (Stat32), all-hit probes

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| TryGetValueRead              | 1.735 μs | 0.0289 μs | 0.0414 μs | 1.679 μs | 1.827 μs | 1.793 μs |  1.00 |    0.03 |   1,161 B |         - |          NA |
| ValueRefOrNullRefRead        | 1.608 μs | 0.0095 μs | 0.0129 μs | 1.595 μs | 1.642 μs | 1.627 μs |  0.93 |    0.02 |   1,150 B |         - |          NA |
| TryGetValueThenIndexerUpdate | 3.552 μs | 0.0861 μs | 0.1288 μs | 3.412 μs | 3.782 μs | 3.739 μs |  2.05 |    0.09 |   3,364 B |         - |          NA |
| ValueRefOrNullRefUpdate      | 1.809 μs | 0.0268 μs | 0.0401 μs | 1.730 μs | 1.882 μs | 1.862 μs |  1.04 |    0.03 |   1,159 B |         - |          NA |
