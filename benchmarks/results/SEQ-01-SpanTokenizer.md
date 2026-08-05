# SEQ-01: SpanTokenizer

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

