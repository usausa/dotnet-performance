# LAB: Unsafe.CopyBlockUnaligned (rejected, R-14)

- Verdict: rejected
- Variable length: 0.98-1.03x vs Span.CopyTo (same Memmove)
- Constant 16 B: 0.83x, code 61 B vs 106 B - marginal
- Array.Copy is slowest with 1.7 KB code

## CopyVariableBenchmark

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Size | Mean      | Error     | StdDev    | Min        | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |----- |----------:|----------:|----------:|-----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SpanCopyTo**         | **16**   |  **2.401 ns** | **0.4158 ns** | **0.6224 ns** |  **1.4157 ns** |  **3.188 ns** |  **3.043 ns** |  **1.08** |    **0.44** |     **673 B** |         **-** |          **NA** |
| ArrayCopy          | 16   |  3.078 ns | 0.1303 ns | 0.1868 ns |  2.7887 ns |  3.469 ns |  3.289 ns |  1.39 |    0.44 |   1,706 B |         - |          NA |
| CopyBlockUnaligned | 16   |  1.827 ns | 0.5201 ns | 0.7624 ns |  0.9734 ns |  2.891 ns |  2.660 ns |  0.83 |    0.44 |     635 B |         - |          NA |
|                    |      |           |           |           |            |           |           |       |         |           |           |             |
| **SpanCopyTo**         | **512**  |  **9.328 ns** | **0.1499 ns** | **0.2244 ns** |  **8.9511 ns** |  **9.875 ns** |  **9.658 ns** |  **1.00** |    **0.03** |     **654 B** |         **-** |          **NA** |
| ArrayCopy          | 512  | 10.017 ns | 0.4071 ns | 0.6093 ns |  8.9523 ns | 11.143 ns | 10.682 ns |  1.07 |    0.07 |   1,687 B |         - |          NA |
| CopyBlockUnaligned | 512  |  9.170 ns | 0.1412 ns | 0.2113 ns |  8.6641 ns |  9.497 ns |  9.397 ns |  0.98 |    0.03 |     616 B |         - |          NA |
|                    |      |           |           |           |            |           |           |       |         |           |           |             |
| **SpanCopyTo**         | **4096** | **45.331 ns** | **3.2149 ns** | **4.8120 ns** | **36.8707 ns** | **51.096 ns** | **49.662 ns** |  **1.01** |    **0.16** |     **631 B** |         **-** |          **NA** |
| ArrayCopy          | 4096 | 49.591 ns | 1.1152 ns | 1.6692 ns | 46.2714 ns | 52.735 ns | 51.708 ns |  1.11 |    0.13 |   1,740 B |         - |          NA |
| CopyBlockUnaligned | 4096 | 46.267 ns | 1.5413 ns | 2.3070 ns | 39.7751 ns | 49.725 ns | 48.943 ns |  1.03 |    0.13 |     593 B |         - |          NA |

## CopyConstantBenchmark (16 B)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SpanCopyTo16         | 1.1533 ns | 0.0584 ns | 0.0874 ns | 0.9017 ns | 1.283 ns | 1.264 ns |  1.01 |    0.11 |     106 B |         - |          NA |
| CopyBlockUnaligned16 | 0.9563 ns | 0.0457 ns | 0.0684 ns | 0.7887 ns | 1.079 ns | 1.039 ns |  0.83 |    0.09 |      61 B |         - |          NA |

