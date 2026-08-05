# LAB: Unsafe.CopyBlockUnaligned (rejected, R-14)

- Verdict: rejected
- Variable length: 0.81-1.01x vs Span.CopyTo (both reach the same Memmove; the sub-ns gap at 16 B is call-shape overhead)
- Constant 16 B: 0.81x, code 61 B vs 106 B - marginal, and rejected on safety grounds
- Array.Copy is slowest (1.22-1.59x) with 1.7 KB code

## CopyVariableBenchmark

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Size | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |----- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| **SpanCopyTo**         | **16**   |  **1.0310 ns** | **0.0052 ns** | **0.0075 ns** |  **1.0326 ns** |  **1.0168 ns** |  **1.0428 ns** |  **1.0408 ns** |  **1.00** |    **0.01** |     **657 B** |         **-** |          **NA** |
| ArrayCopy          | 16   |  1.6386 ns | 0.0231 ns | 0.0332 ns |  1.6308 ns |  1.6092 ns |  1.7736 ns |  1.6533 ns |  1.59 |    0.03 |   1,694 B |         - |          NA |
| CopyBlockUnaligned | 16   |  0.8392 ns | 0.0093 ns | 0.0133 ns |  0.8380 ns |  0.8149 ns |  0.8643 ns |  0.8575 ns |  0.81 |    0.01 |     619 B |         - |          NA |
|                    |      |            |           |           |            |            |            |            |       |         |           |           |             |
| **SpanCopyTo**         | **512**  |  **4.7445 ns** | **0.0397 ns** | **0.0557 ns** |  **4.7286 ns** |  **4.6763 ns** |  **4.9087 ns** |  **4.7951 ns** |  **1.00** |    **0.02** |     **652 B** |         **-** |          **NA** |
| ArrayCopy          | 512  |  5.7657 ns | 0.0162 ns | 0.0243 ns |  5.7649 ns |  5.7246 ns |  5.8215 ns |  5.7944 ns |  1.22 |    0.01 |   1,671 B |         - |          NA |
| CopyBlockUnaligned | 512  |  4.3506 ns | 0.0186 ns | 0.0267 ns |  4.3497 ns |  4.3019 ns |  4.4156 ns |  4.3789 ns |  0.92 |    0.01 |     614 B |         - |          NA |
|                    |      |            |           |           |            |            |            |            |       |         |           |           |             |
| **SpanCopyTo**         | **4096** | **25.8064 ns** | **0.2123 ns** | **0.3178 ns** | **25.8598 ns** | **25.3305 ns** | **26.3525 ns** | **26.1954 ns** |  **1.00** |    **0.02** |     **619 B** |         **-** |          **NA** |
| ArrayCopy          | 4096 | 26.9068 ns | 0.2651 ns | 0.3628 ns | 26.8280 ns | 26.6048 ns | 28.0352 ns | 27.2880 ns |  1.04 |    0.02 |   1,744 B |         - |          NA |
| CopyBlockUnaligned | 4096 | 26.0984 ns | 0.3311 ns | 0.4748 ns | 26.3741 ns | 25.5297 ns | 26.6703 ns | 26.6365 ns |  1.01 |    0.02 |     581 B |         - |          NA |

## CopyConstantBenchmark (16 B)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| SpanCopyTo16         | 0.2233 ns | 0.0053 ns | 0.0079 ns | 0.2081 ns | 0.2354 ns | 0.2318 ns |  1.00 |    0.05 |     106 B |         - |          NA |
| CopyBlockUnaligned16 | 0.1801 ns | 0.0035 ns | 0.0052 ns | 0.1708 ns | 0.1903 ns | 0.1868 ns |  0.81 |    0.04 |      61 B |         - |          NA |

