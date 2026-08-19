# TXT-10: Aggregating known-string matching into a switch

- Verdict: **adopted with a size window** - the default for 5-64 keys, useless at <=4, and not usable above 64
- 16 keys: **0.51x hit / 0.33x miss** vs an Equals chain; also beats Dictionary(Ordinal) (1.38x) and SampledNameTable (1.09x) on hit
- 64 keys: level with a hand-written sampling-hash switch (1.06x hit, 1.00x miss - CIs overlap)
- 128 keys: **collapses to 1.73x hit / 3.79x miss** - Roslyn switches to an FNV-1a hash over every character plus a 191-node binary search tree
- <=4 keys: the compiler emits **the same instruction stream as a hand-written Equals chain** (60 instructions, 263 B). On span input the chain is actually faster (1.17x hit / 1.12x miss), so the existing "<=4 keys: chain" guidance stands
- Long keys do **not** cause the predicted reversal: at 16 keys of 58-62 characters Roslyn never hashes, so the plain switch stays within 1.10-1.12x
- Code size scales with the key set (1,185 B at 16 -> 11,611 B at 128) while SampledNameTable stays flat at ~730-780 B
- **The win is against a *plain* if-chain.** Where the input must be converted before it can be matched (case folding for DB column names and the like), normalization costs more than the dispatch saves - 4.5-7.8x slower at 8 columns. See "Where the switch does not fit"

## What Roslyn actually generates

The strategy is chosen by **how many keys land in each length bucket**, not by the key count alone. Only the 128-key set crosses into hashing, and that is the only shape that reads every character.

| Key set | Generated strategy | Scans every character |
|---|---|---|
| 4 | Length + character tests - **identical to the hand-written Equals chain** | no |
| 16 | Length + character tests | no |
| 64 | Length jump table -> per-bucket character jump table | no |
| 128 | **FNV-1a loop over every character** (`mov edx,811C9DC5`, `xor` / `imul 1000193`) + a 191-node binary search over the hashes | **yes** |
| 16 x 58-62 chars | Length jump table (5 buckets) + first character + reference check + AVX-512 `vmovups zmm0` compare | no |

The hand-written sampling-hash switch it is measured against is a flat ~14 instructions with no loop (`shl edx,16`, first `<<8`, middle `<<4`, last xor), so its cost is independent of both key count and key length.

## How the probes are built

Keys are ordinary PascalCase identifier names (column / property / enum-member shaped); the smaller sets are prefixes of the same 128-name master list. The sampling hash collides naturally on them (64 keys -> 63 distinct hashes, 128 -> 125, longest chain 2); nothing is tuned to favour a strategy.

A miss probe keeps the length and replaces the character at index `length/3` - a position the sampling hash never reads, since it samples first / middle / last. So **every miss lands in the same bucket and has to be rejected by the full compare**: the pessimistic case for the sampling forms and the neutral case for the compiler's own dispatch. The 128-key conclusion survives that handicap.

Numbers below are **per probe** (`OperationsPerInvoke` = key count).

## 4 keys (lower bound)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Probe | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **EqualsChain**     | **Hit**   | **1.0790 ns** | **0.0179 ns** | **0.0263 ns** | **1.0862 ns** | **1.0228 ns** | **1.1208 ns** | **1.1074 ns** |  **1.00** |    **0.03** |     **255 B** |         **-** |          **NA** |
| SpanEqualsChain | Hit   | 0.9358 ns | 0.0200 ns | 0.0280 ns | 0.9439 ns | 0.8820 ns | 0.9709 ns | 0.9661 ns |  0.87 |    0.03 |     251 B |         - |          NA |
| StringSwitch    | Hit   | 1.0480 ns | 0.0141 ns | 0.0206 ns | 1.0439 ns | 1.0109 ns | 1.0836 ns | 1.0782 ns |  0.97 |    0.03 |     263 B |         - |          NA |
| SpanSwitch      | Hit   | 1.0920 ns | 0.0551 ns | 0.0825 ns | 1.0685 ns | 1.0043 ns | 1.2855 ns | 1.2469 ns |  1.01 |    0.08 |     252 B |         - |          NA |
|                 |       |           |           |           |           |           |           |           |       |         |           |           |             |
| **EqualsChain**     | **Miss**  | **1.5144 ns** | **0.0427 ns** | **0.0599 ns** | **1.5201 ns** | **1.4463 ns** | **1.6447 ns** | **1.5868 ns** |  **1.00** |    **0.05** |     **259 B** |         **-** |          **NA** |
| SpanEqualsChain | Miss  | 1.6789 ns | 0.1070 ns | 0.1569 ns | 1.8038 ns | 1.4847 ns | 1.8518 ns | 1.8417 ns |  1.11 |    0.11 |     259 B |         - |          NA |
| StringSwitch    | Miss  | 1.6569 ns | 0.0201 ns | 0.0300 ns | 1.6523 ns | 1.6145 ns | 1.7058 ns | 1.6992 ns |  1.10 |    0.05 |     259 B |         - |          NA |
| SpanSwitch      | Miss  | 1.8760 ns | 0.0478 ns | 0.0670 ns | 1.8607 ns | 1.7901 ns | 2.0959 ns | 1.9697 ns |  1.24 |    0.06 |     259 B |         - |          NA |

`EqualsChain` and `StringSwitch` compile to a byte-identical instruction stream here, and the sign of the difference flips between runs (an earlier run measured 1.009 vs 1.038 the other way). The span switch is the only form that is genuinely slower.

## 16 keys (the main win)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Probe | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **EqualsChain**      | **Hit**   | **3.466 ns** | **0.0332 ns** | **0.0476 ns** | **3.459 ns** | **3.379 ns** | **3.583 ns** | **3.537 ns** |  **1.00** |    **0.02** |   **1,146 B** |         **-** |          **NA** |
| SpanEqualsChain  | Hit   | 2.808 ns | 0.0677 ns | 0.0949 ns | 2.843 ns | 2.697 ns | 2.954 ns | 2.915 ns |  0.81 |    0.03 |   1,169 B |         - |          NA |
| StringSwitch     | Hit   | 1.776 ns | 0.0448 ns | 0.0657 ns | 1.746 ns | 1.702 ns | 1.928 ns | 1.905 ns |  0.51 |    0.02 |   1,170 B |         - |          NA |
| SpanSwitch       | Hit   | 2.008 ns | 0.0512 ns | 0.0751 ns | 2.033 ns | 1.915 ns | 2.115 ns | 2.097 ns |  0.58 |    0.02 |   1,185 B |         - |          NA |
| DictionaryLookup | Hit   | 4.776 ns | 0.0396 ns | 0.0569 ns | 4.783 ns | 4.692 ns | 4.879 ns | 4.838 ns |  1.38 |    0.02 |   1,142 B |         - |          NA |
| SampledTable     | Hit   | 3.788 ns | 0.0223 ns | 0.0298 ns | 3.777 ns | 3.741 ns | 3.845 ns | 3.827 ns |  1.09 |    0.02 |     766 B |         - |          NA |
|                  |       |          |           |           |          |          |          |          |       |         |           |           |             |
| **EqualsChain**      | **Miss**  | **6.080 ns** | **0.0865 ns** | **0.1240 ns** | **6.091 ns** | **5.854 ns** | **6.307 ns** | **6.224 ns** |  **1.00** |    **0.03** |   **1,146 B** |         **-** |          **NA** |
| SpanEqualsChain  | Miss  | 5.177 ns | 0.0457 ns | 0.0656 ns | 5.182 ns | 5.047 ns | 5.323 ns | 5.245 ns |  0.85 |    0.02 |   1,178 B |         - |          NA |
| StringSwitch     | Miss  | 1.993 ns | 0.0138 ns | 0.0202 ns | 1.999 ns | 1.953 ns | 2.027 ns | 2.017 ns |  0.33 |    0.01 |   1,166 B |         - |          NA |
| SpanSwitch       | Miss  | 2.016 ns | 0.0717 ns | 0.1005 ns | 1.956 ns | 1.897 ns | 2.146 ns | 2.138 ns |  0.33 |    0.02 |   1,179 B |         - |          NA |
| DictionaryLookup | Miss  | 3.152 ns | 0.0216 ns | 0.0316 ns | 3.158 ns | 3.091 ns | 3.201 ns | 3.188 ns |  0.52 |    0.01 |     509 B |         - |          NA |
| SampledTable     | Miss  | 4.908 ns | 0.0854 ns | 0.1252 ns | 4.869 ns | 4.740 ns | 5.316 ns | 5.065 ns |  0.81 |    0.03 |     753 B |         - |          NA |

## 64 keys

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Probe | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **Hit**   | **2.633 ns** | **0.0301 ns** | **0.0450 ns** | **2.633 ns** | **2.551 ns** | **2.723 ns** | **2.683 ns** |  **1.00** |    **0.02** |   **5,825 B** |         **-** |          **NA** |
| SpanSwitch    | Hit   | 2.792 ns | 0.0224 ns | 0.0328 ns | 2.791 ns | 2.736 ns | 2.862 ns | 2.833 ns |  1.06 |    0.02 |   4,897 B |         - |          NA |
| SampledTable  | Hit   | 3.607 ns | 0.0646 ns | 0.0926 ns | 3.550 ns | 3.499 ns | 3.761 ns | 3.721 ns |  1.37 |    0.04 |     755 B |         - |          NA |
|               |       |          |           |           |          |          |          |          |       |         |           |           |             |
| **SampledSwitch** | **Miss**  | **2.419 ns** | **0.0462 ns** | **0.0632 ns** | **2.402 ns** | **2.341 ns** | **2.585 ns** | **2.522 ns** |  **1.00** |    **0.04** |   **6,001 B** |         **-** |          **NA** |
| SpanSwitch    | Miss  | 2.408 ns | 0.0467 ns | 0.0685 ns | 2.387 ns | 2.291 ns | 2.510 ns | 2.492 ns |  1.00 |    0.04 |   5,019 B |         - |          NA |
| SampledTable  | Miss  | 4.074 ns | 0.0142 ns | 0.0209 ns | 4.075 ns | 4.039 ns | 4.120 ns | 4.101 ns |  1.69 |    0.04 |     733 B |         - |          NA |

Miss is level (2.419 +/- 0.046 vs 2.408 +/- 0.047, CIs overlap) but code size differs (6,001 vs 5,019 B), so it is recorded as a measurement tie rather than "no difference".

## 128 keys (the cliff)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Probe | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **Hit**   |  **2.650 ns** | **0.0360 ns** | **0.0538 ns** |  **2.639 ns** |  **2.551 ns** |  **2.755 ns** |  **2.716 ns** |  **1.00** |    **0.03** |  **11,506 B** |         **-** |          **NA** |
| SpanSwitch    | Hit   |  4.580 ns | 0.3549 ns | 0.5090 ns |  4.514 ns |  3.807 ns |  5.543 ns |  5.217 ns |  1.73 |    0.19 |  11,611 B |         - |          NA |
| SampledTable  | Hit   |  3.722 ns | 0.0103 ns | 0.0154 ns |  3.722 ns |  3.692 ns |  3.754 ns |  3.741 ns |  1.41 |    0.03 |     776 B |         - |          NA |
|               |       |           |           |           |           |           |           |           |       |         |           |           |             |
| **SampledSwitch** | **Miss**  |  **2.687 ns** | **0.1545 ns** | **0.2215 ns** |  **2.542 ns** |  **2.414 ns** |  **3.091 ns** |  **2.962 ns** |  **1.01** |    **0.11** |  **11,517 B** |         **-** |          **NA** |
| SpanSwitch    | Miss  | 10.112 ns | 0.0234 ns | 0.0350 ns | 10.111 ns | 10.054 ns | 10.185 ns | 10.157 ns |  3.79 |    0.30 |  11,839 B |         - |          NA |
| SampledTable  | Miss  |  4.300 ns | 0.0138 ns | 0.0207 ns |  4.295 ns |  4.270 ns |  4.338 ns |  4.331 ns |  1.61 |    0.13 |     756 B |         - |          NA |

Miss costs far more than hit because the binary search has to descend to full depth before it can conclude "not present", while a hit can exit at an earlier arm.

## 16 keys x 58-62 characters (long keys)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Probe | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **Hit**   | **1.856 ns** | **0.0098 ns** | **0.0146 ns** | **1.827 ns** | **1.878 ns** | **1.872 ns** |  **1.00** |    **0.01** |   **2,018 B** |         **-** |          **NA** |
| SpanSwitch    | Hit   | 2.051 ns | 0.0113 ns | 0.0165 ns | 2.027 ns | 2.090 ns | 2.073 ns |  1.10 |    0.01 |   1,651 B |         - |          NA |
| SampledTable  | Hit   | 3.525 ns | 0.0340 ns | 0.0488 ns | 3.477 ns | 3.655 ns | 3.602 ns |  1.90 |    0.03 |     730 B |         - |          NA |
|               |       |          |           |           |          |          |          |       |         |           |           |             |
| **SampledSwitch** | **Miss**  | **1.869 ns** | **0.0046 ns** | **0.0069 ns** | **1.857 ns** | **1.884 ns** | **1.877 ns** |  **1.00** |    **0.01** |   **2,039 B** |         **-** |          **NA** |
| SpanSwitch    | Miss  | 2.085 ns | 0.0320 ns | 0.0479 ns | 2.022 ns | 2.227 ns | 2.149 ns |  1.12 |    0.03 |   1,640 B |         - |          NA |
| SampledTable  | Miss  | 3.077 ns | 0.0165 ns | 0.0247 ns | 3.041 ns | 3.138 ns | 3.107 ns |  1.65 |    0.01 |     718 B |         - |          NA |

No reversal. The plain switch is only 1.10-1.12x behind, barely worse than at 64 short keys - because Roslyn does not hash this shape at all.

## Code size

| Key set | Plain switch | Sampling-hash switch | SampledNameTable |
|---|---:|---:|---:|
| 16 | 1,185 B | - | 766 B |
| 16 x 58-62 chars | 1,651 B | 2,018 B | 730 B |
| 64 | 4,897 B | 5,825 B | 755 B |
| 128 | 11,611 B | 11,506 B | **776 B** |

Both switch forms grow with the key set; the runtime table does not. Where binary size or I-cache pressure matters, SampledNameTable ([COL-04](COL-04-SampledNameTable.md)) can be the right answer even at sizes where the switch is 1.1-1.9x faster.

## Where the switch does not fit: matching that needs a conversion

The result above is a comparison against a **plain** `Equals` chain - one where the probe can be compared exactly as it arrives. As soon as the input has to be converted first, the conversion dominates and the switch stops being the right answer.

Measured on the DB column-name shape: resolve a reader's columns to ordinals, matching `OrdinalIgnoreCase` per SQL identifier rules. The current generated form is a guarded `Equals(OrdinalIgnoreCase)` chain at <=16 groups and a sampling-hash switch above that. The alternative is to normalize the probe first so a plain ordinal switch can be used.

| Approach | 8 cols Pascal | 8 cols snake | 24 cols Pascal | 24 cols snake |
|---|---:|---:|---:|---:|
| **Current (chain / hash)** | **15.4 ns** | **15.6 ns** | **129.2 ns** | **129.1 ns** |
| `ToUpperInvariant()` string + string switch | 120.6 (7.82) | 117.0 (7.50) | 387.2 (3.00) | 388.6 (3.01) |
| stackalloc normalize + span switch | 78.4 (5.09) | 75.2 (4.82) | 243.6 (1.89) | 242.6 (1.88) |
| `Ascii.ToUpper` (SIMD) + span switch | 72.3 (4.69) | 70.0 (4.49) | 227.6 (1.76) | 226.6 (1.76) |
| Plain switch, no case handling (reference) | 34.3 (2.23) | 35.4 (2.27) | 102.5 (0.79) | 108.1 (0.84) |

Per column:

| Approach | 8 cols | 24 cols |
|---|---:|---:|
| Current | 1.93 ns | 5.38 ns |
| Plain switch, no case handling | 4.29 ns | 4.27 ns |
| `Ascii.ToUpper` + span switch | 9.04 ns | 9.48 ns |
| stackalloc normalize + span switch | 9.80 ns | 10.15 ns |

- **Normalization adds about 4.8-5.2 ns per column, and the SIMD `Ascii.ToUpper` version is no cheaper.** The cost is structural - fold every character into a buffer, then read every character again in the switch - so no API choice avoids it. It exceeds the switch's own dispatch gain
- The plain switch's dispatch is a size-independent **4.3 ns per column**. That beats the current hash form at 24 columns (5.38) but loses to the chain at 8 (1.93), where arriving in declaration order costs roughly one `Equals` per column
- The 0.79x at 24 columns is **not** the switch being fast - it is case-insensitive matching being dropped, which changes the semantics
- Naming convention makes no difference (PascalCase vs snake_case within a few percent), because the generated literals already carry the applied convention
- Extrapolating the chain's 1.93 ns/column to 24 columns by N²/2 gives about 120 ns against the hash form's 129 ns, so **the existing switchover at 16 columns is consistent with measurement**

Where case folding, culture handling, or trimming has to happen first, use the sampling-hash form instead: only the 3 sampled characters are converted, and the confirming comparison stays `OrdinalIgnoreCase`. The switch is the default only when keys can be compared **as they arrive**.

## Guidance

- Key set fixed at compile time, comparable **as-is**, and **<= 64 entries: write the switch first**, before any hand-written dispatch
- **> 64 entries**: sampling-hash switch (see [generated-code-patterns](../../docs/generated-code-patterns.md) scenario 1)
- **<= 4 entries**: no reason to change - the chain and the switch are the same code, and on span input the chain wins
- Key length is not a criterion
- The switch is **ordinal**. Case-insensitive matching cannot use it; use the sampling-hash form with upper-cased sampling plus an `OrdinalIgnoreCase` confirm. Normalizing the input up front to reach a plain switch is measurably worse at every size
- The external report's 0.15x figure is an if-chain comparison at 67 values; **0.5x at 16 keys is the realistic expectation**
