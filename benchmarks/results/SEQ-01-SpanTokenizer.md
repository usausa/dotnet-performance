# SEQ-01: SpanTokenizer

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - zero allocation always; the time win stays limited to short inputs
- `string.Split` got faster on net11 (0.84x / 0.90x), so the tokenizer is 0.46x at 4 tokens and 1.24x at 64 tokens (1.16x on net10)
- Against `MemoryExtensions.Split`: 0.89x / 0.95x on net11

### SpanTokenizerBenchmark

net10 → net11 per row:

| Method | Tokens | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| StringSplit | 4 | 30.29 ns | 25.51 ns | 0.84 | 216 B → 216 B | 5,309 B → 6,222 B |
| SpanTokenizer | 4 | 12.22 ns | 11.83 ns | 0.97 | - → - | 738 B → 717 B |
| StringSplit | 64 | 361.80 ns | 323.94 ns | 0.90 | 3096 B → 3096 B | 5,425 B → 6,098 B |
| SpanTokenizer | 64 | 420.68 ns | 402.84 ns | 0.96 | - → - | 754 B → 728 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.55 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Tokens | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| **StringSplit**   | **.NET 10.0**           | **.NET 10.0** | **4**      |  **30.29 ns** | **2.130 ns** | **2.915 ns** |  **26.98 ns** |  **33.47 ns** |  **33.33 ns** |  **1.00** |    **0.00** |   **5,309 B** | **0.0258** |      **-** |     **216 B** |        **1.00** |
| SpanTokenizer | .NET 10.0           | .NET 10.0 | 4      |  12.22 ns | 0.036 ns | 0.052 ns |  12.16 ns |  12.38 ns |  12.30 ns |  0.41 |    0.04 |     738 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |          |          |           |           |           |       |         |           |        |        |           |             |
| StringSplit   | MediumRun-.NET 11.0 | .NET 11.0 | 4      |  25.51 ns | 0.114 ns | 0.160 ns |  25.13 ns |  25.81 ns |  25.75 ns |  1.00 |    0.00 |   6,222 B | 0.0258 |      - |     216 B |        1.00 |
| SpanTokenizer | MediumRun-.NET 11.0 | .NET 11.0 | 4      |  11.83 ns | 0.032 ns | 0.044 ns |  11.75 ns |  11.97 ns |  11.88 ns |  0.46 |    0.00 |     717 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |          |          |           |           |           |       |         |           |        |        |           |             |
| **StringSplit**   | **.NET 10.0**           | **.NET 10.0** | **64**     | **361.80 ns** | **3.708 ns** | **4.950 ns** | **353.85 ns** | **369.35 ns** | **367.45 ns** |  **1.00** |    **0.00** |   **5,425 B** | **0.3700** | **0.0043** |    **3096 B** |        **1.00** |
| SpanTokenizer | .NET 10.0           | .NET 10.0 | 64     | 420.68 ns | 2.366 ns | 3.541 ns | 416.80 ns | 432.81 ns | 424.90 ns |  1.16 |    0.02 |     754 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |          |          |           |           |           |       |         |           |        |        |           |             |
| StringSplit   | MediumRun-.NET 11.0 | .NET 11.0 | 64     | 323.94 ns | 3.266 ns | 4.685 ns | 318.59 ns | 338.52 ns | 329.35 ns |  1.00 |    0.00 |   6,098 B | 0.3700 | 0.0043 |    3096 B |        1.00 |
| SpanTokenizer | MediumRun-.NET 11.0 | .NET 11.0 | 64     | 402.84 ns | 0.593 ns | 0.888 ns | 401.33 ns | 404.22 ns | 403.81 ns |  1.24 |    0.02 |     728 B |      - |      - |         - |        0.00 |

### SpanTokenizerBclComparisonBenchmark

net10 → net11 per row:

| Method | Tokens | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| SpanTokenizer | 4 | 12.61 ns | 12.75 ns | 1.01 | - → - | 738 B → 717 B |
| MemoryExtensionsSplit | 4 | 13.89 ns | 14.32 ns | 1.03 | - → - | 941 B → 905 B |
| SpanTokenizer | 64 | 420.91 ns | 401.40 ns | 0.95 | - → - | 754 B → 728 B |
| MemoryExtensionsSplit | 64 | 438.80 ns | 423.09 ns | 0.96 | - → - | 957 B → 916 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.75 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Tokens | Mean      | Error    | StdDev   | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SpanTokenizer**         | **.NET 10.0**           | **.NET 10.0** | **4**      |  **12.61 ns** | **0.134 ns** | **0.197 ns** |  **12.53 ns** |  **12.40 ns** |  **13.09 ns** |  **12.91 ns** |  **1.00** |    **0.00** |     **738 B** |         **-** |          **NA** |
| MemoryExtensionsSplit | .NET 10.0           | .NET 10.0 | 4      |  13.89 ns | 0.056 ns | 0.084 ns |  13.88 ns |  13.77 ns |  14.04 ns |  14.01 ns |  1.10 |    0.02 |     941 B |         - |          NA |
|                       |                     |           |        |           |          |          |           |           |           |           |       |         |           |           |             |
| SpanTokenizer         | MediumRun-.NET 11.0 | .NET 11.0 | 4      |  12.75 ns | 0.501 ns | 0.719 ns |  13.34 ns |  11.94 ns |  13.48 ns |  13.43 ns |  1.00 |    0.00 |     717 B |         - |          NA |
| MemoryExtensionsSplit | MediumRun-.NET 11.0 | .NET 11.0 | 4      |  14.32 ns | 0.633 ns | 0.907 ns |  15.06 ns |  13.32 ns |  15.26 ns |  15.21 ns |  1.13 |    0.09 |     905 B |         - |          NA |
|                       |                     |           |        |           |          |          |           |           |           |           |       |         |           |           |             |
| **SpanTokenizer**         | **.NET 10.0**           | **.NET 10.0** | **64**     | **420.91 ns** | **4.112 ns** | **6.155 ns** | **420.85 ns** | **413.96 ns** | **428.10 ns** | **427.38 ns** |  **1.00** |    **0.00** |     **754 B** |         **-** |          **NA** |
| MemoryExtensionsSplit | .NET 10.0           | .NET 10.0 | 64     | 438.80 ns | 4.219 ns | 6.185 ns | 433.93 ns | 431.53 ns | 448.08 ns | 445.82 ns |  1.04 |    0.02 |     957 B |         - |          NA |
|                       |                     |           |        |           |          |          |           |           |           |           |       |         |           |           |             |
| SpanTokenizer         | MediumRun-.NET 11.0 | .NET 11.0 | 64     | 401.40 ns | 1.425 ns | 2.089 ns | 400.57 ns | 398.56 ns | 405.83 ns | 403.98 ns |  1.00 |    0.00 |     728 B |         - |          NA |
| MemoryExtensionsSplit | MediumRun-.NET 11.0 | .NET 11.0 | 64     | 423.09 ns | 0.600 ns | 0.897 ns | 423.21 ns | 421.85 ns | 424.58 ns | 424.31 ns |  1.05 |    0.01 |     916 B |         - |          NA |

## Earlier record

- Verdict: adopted (implemented) - zero allocation always; the time win is limited to short inputs
- 4 tokens: 0.47x vs string.Split; 64 tokens: 1.15x (slower, non-overlapping CIs) - string.Split's vectorized core scales better on long inputs
- Allocation 216 B / 3,096 B -> 0 B in every case; code size ~5 KB -> 0.7 KB
- Ahead of MemoryExtensions.Split (1.04-1.13x slower than this) among the span-based approaches
- Choose it for allocation elimination and short token counts, not as a blanket speed win

## SpanTokenizerBenchmark (vs string.Split)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Tokens | Mean      | Error    | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|-------------- |------- |----------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **StringSplit**   | **4**      |  **26.04 ns** | **0.429 ns** |  **0.587 ns** |  **25.22 ns** |  **27.75 ns** |  **26.59 ns** |  **1.00** |    **0.03** | **0.0258** |   **5,036 B** |      **-** |     **216 B** |        **1.00** |
| SpanTokenizer | 4      |  12.19 ns | 0.676 ns |  1.011 ns |  11.51 ns |  15.55 ns |  13.43 ns |  0.47 |    0.04 |      - |     707 B |      - |         - |        0.00 |
|               |        |           |          |           |           |           |           |       |         |        |           |        |           |             |
| **StringSplit**   | **64**     | **347.78 ns** | **8.806 ns** | **11.756 ns** | **328.95 ns** | **393.33 ns** | **355.30 ns** |  **1.00** |    **0.05** | **0.3700** |   **5,219 B** | **0.0043** |    **3096 B** |        **1.00** |
| SpanTokenizer | 64     | 399.93 ns | 0.803 ns |  1.177 ns | 398.15 ns | 402.55 ns | 401.46 ns |  1.15 |    0.04 |      - |     722 B |      - |         - |        0.00 |

## SpanTokenizerBclComparisonBenchmark (vs MemoryExtensions.Split, .NET 9+)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Tokens | Mean      | Error    | StdDev   | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------- |------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SpanTokenizer**         | **4**      |  **11.70 ns** | **0.123 ns** | **0.168 ns** |  **11.64 ns** |  **11.54 ns** |  **12.20 ns** |  **11.98 ns** |  **1.00** |    **0.02** |     **707 B** |         **-** |          **NA** |
| MemoryExtensionsSplit | 4      |  13.26 ns | 0.040 ns | 0.057 ns |  13.25 ns |  13.16 ns |  13.42 ns |  13.32 ns |  1.13 |    0.02 |     910 B |         - |          NA |
|                       |        |           |          |          |           |           |           |           |       |         |           |           |             |
| **SpanTokenizer**         | **64**     | **405.80 ns** | **4.386 ns** | **6.429 ns** | **404.18 ns** | **399.98 ns** | **426.29 ns** | **416.64 ns** |  **1.00** |    **0.02** |     **722 B** |         **-** |          **NA** |
| MemoryExtensionsSplit | 64     | 420.89 ns | 2.122 ns | 3.043 ns | 422.60 ns | 417.17 ns | 425.06 ns | 424.06 ns |  1.04 |    0.02 |     922 B |         - |          NA |

