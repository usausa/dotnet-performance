# SEQ-02: SpanTokenizer

## SpanTokenizerBenchmark(string.Split との比較)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 8.0  : .NET 8.0.29 (8.0.29, 8.0.2926.32403), X64 RyuJIT x86-64-v3
  MediumRun-.NET 9.0  : .NET 9.0.18 (9.0.18, 9.0.1826.31522), X64 RyuJIT x86-64-v3

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Tokens | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| **StringSplit**   | **MediumRun-.NET 10.0** | **.NET 10.0** | **4**      |  **44.24 ns** |  **0.555 ns** |  **0.778 ns** |  **42.53 ns** |  **45.81 ns** |  **45.12 ns** |  **1.00** |    **0.02** |   **4,814 B** | **0.0129** |      **-** |     **216 B** |        **1.00** |
| SpanTokenizer | MediumRun-.NET 10.0 | .NET 10.0 | 4      |  15.21 ns |  0.156 ns |  0.233 ns |  14.72 ns |  15.63 ns |  15.49 ns |  0.34 |    0.01 |     548 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |           |           |           |           |           |       |         |           |        |        |           |             |
| StringSplit   | MediumRun-.NET 8.0  | .NET 8.0  | 4      |  51.48 ns |  1.056 ns |  1.548 ns |  48.70 ns |  54.01 ns |  53.55 ns |  1.00 |    0.04 |   2,635 B | 0.0129 |      - |     216 B |        1.00 |
| SpanTokenizer | MediumRun-.NET 8.0  | .NET 8.0  | 4      |  15.47 ns |  0.281 ns |  0.420 ns |  14.69 ns |  16.50 ns |  16.02 ns |  0.30 |    0.01 |     579 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |           |           |           |           |           |       |         |           |        |        |           |             |
| StringSplit   | MediumRun-.NET 9.0  | .NET 9.0  | 4      |  49.41 ns |  0.644 ns |  0.944 ns |  46.59 ns |  51.10 ns |  50.46 ns |  1.00 |    0.03 |   2,922 B | 0.0129 |      - |     216 B |        1.00 |
| SpanTokenizer | MediumRun-.NET 9.0  | .NET 9.0  | 4      |  15.98 ns |  0.529 ns |  0.775 ns |  14.93 ns |  17.23 ns |  16.85 ns |  0.32 |    0.02 |     586 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |           |           |           |           |           |       |         |           |        |        |           |             |
| **StringSplit**   | **MediumRun-.NET 10.0** | **.NET 10.0** | **64**     | **534.65 ns** | **10.922 ns** | **15.665 ns** | **508.20 ns** | **567.71 ns** | **555.88 ns** |  **1.00** |    **0.04** |   **4,998 B** | **0.1850** | **0.0019** |    **3096 B** |        **1.00** |
| SpanTokenizer | MediumRun-.NET 10.0 | .NET 10.0 | 64     | 371.66 ns |  2.160 ns |  3.233 ns | 360.43 ns | 377.63 ns | 375.22 ns |  0.70 |    0.02 |     573 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |           |           |           |           |           |       |         |           |        |        |           |             |
| StringSplit   | MediumRun-.NET 8.0  | .NET 8.0  | 64     | 601.93 ns | 12.393 ns | 18.550 ns | 565.16 ns | 637.82 ns | 625.21 ns |  1.00 |    0.04 |   2,648 B | 0.1850 | 0.0019 |    3096 B |        1.00 |
| SpanTokenizer | MediumRun-.NET 8.0  | .NET 8.0  | 64     | 373.19 ns |  2.619 ns |  3.839 ns | 365.10 ns | 382.34 ns | 378.78 ns |  0.62 |    0.02 |     582 B |      - |      - |         - |        0.00 |
|               |                     |           |        |           |           |           |           |           |           |       |         |           |        |        |           |             |
| StringSplit   | MediumRun-.NET 9.0  | .NET 9.0  | 64     | 537.42 ns | 18.136 ns | 27.145 ns | 493.03 ns | 588.80 ns | 573.95 ns |  1.00 |    0.07 |   2,941 B | 0.1850 | 0.0019 |    3096 B |        1.00 |
| SpanTokenizer | MediumRun-.NET 9.0  | .NET 9.0  | 64     | 363.24 ns |  4.172 ns |  6.244 ns | 356.14 ns | 378.98 ns | 372.14 ns |  0.68 |    0.04 |     567 B |      - |      - |         - |        0.00 |

## SpanTokenizerBclComparisonBenchmark(MemoryExtensions.Split との比較、.NET 9+)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 9.0  : .NET 9.0.18 (9.0.18, 9.0.1826.31522), X64 RyuJIT x86-64-v3

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Tokens | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SpanTokenizer**         | **MediumRun-.NET 10.0** | **.NET 10.0** | **4**      |  **14.55 ns** | **0.229 ns** | **0.343 ns** |  **14.01 ns** |  **15.42 ns** |  **15.00 ns** |  **1.00** |    **0.03** |     **548 B** |         **-** |          **NA** |
| MemoryExtensionsSplit | MediumRun-.NET 10.0 | .NET 10.0 | 4      |  18.33 ns | 0.107 ns | 0.147 ns |  17.98 ns |  18.57 ns |  18.50 ns |  1.26 |    0.03 |     751 B |         - |          NA |
|                       |                     |           |        |           |          |          |           |           |           |       |         |           |           |             |
| SpanTokenizer         | MediumRun-.NET 9.0  | .NET 9.0  | 4      |  14.82 ns | 0.215 ns | 0.315 ns |  14.37 ns |  15.62 ns |  15.34 ns |  1.00 |    0.03 |     586 B |         - |          NA |
| MemoryExtensionsSplit | MediumRun-.NET 9.0  | .NET 9.0  | 4      |  18.49 ns | 0.186 ns | 0.278 ns |  17.86 ns |  18.91 ns |  18.82 ns |  1.25 |    0.03 |     749 B |         - |          NA |
|                       |                     |           |        |           |          |          |           |           |           |       |         |           |           |             |
| **SpanTokenizer**         | **MediumRun-.NET 10.0** | **.NET 10.0** | **64**     | **357.72 ns** | **2.432 ns** | **3.640 ns** | **351.15 ns** | **365.10 ns** | **361.30 ns** |  **1.00** |    **0.01** |     **573 B** |         **-** |          **NA** |
| MemoryExtensionsSplit | MediumRun-.NET 10.0 | .NET 10.0 | 64     | 385.90 ns | 3.814 ns | 5.708 ns | 377.65 ns | 398.11 ns | 394.07 ns |  1.08 |    0.02 |     776 B |         - |          NA |
|                       |                     |           |        |           |          |          |           |           |           |       |         |           |           |             |
| SpanTokenizer         | MediumRun-.NET 9.0  | .NET 9.0  | 64     | 358.31 ns | 4.791 ns | 7.171 ns | 349.94 ns | 376.13 ns | 369.82 ns |  1.00 |    0.03 |     567 B |         - |          NA |
| MemoryExtensionsSplit | MediumRun-.NET 9.0  | .NET 9.0  | 64     | 384.86 ns | 2.161 ns | 3.235 ns | 380.22 ns | 391.43 ns | 389.37 ns |  1.07 |    0.02 |     730 B |         - |          NA |
