# TXT-08: SearchValues<char> vs IndexOfAny(char[])

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - SearchValues is 0.99x / 0.33x / 0.20x of the array overload at 3 / 8 / 32 candidates on net11, the same as net10

### SearchValuesBenchmark

net10 → net11 per row:

| Method | Candidates | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| IndexOfAnyArray | 3 | 6.554 ns | 6.530 ns | 1.00 | - → - | 2,552 B → 2,585 B |
| IndexOfAnySearchValues | 3 | 6.510 ns | 6.480 ns | 1.00 | - → - | 995 B → 992 B |
| IndexOfAnyArray | 8 | 14.512 ns | 14.154 ns | 0.98 | - → - | 2,797 B → 2,905 B |
| IndexOfAnySearchValues | 8 | 4.581 ns | 4.617 ns | 1.01 | - → - | 623 B → 668 B |
| IndexOfAnyArray | 32 | 24.236 ns | 24.177 ns | 1.00 | - → - | 2,809 B → 2,905 B |
| IndexOfAnySearchValues | 32 | 4.669 ns | 4.815 ns | 1.03 | - → - | 623 B → 668 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.33 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                 | Job                 | Runtime   | Candidates | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|----------------------- |-------------------- |---------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| **IndexOfAnyArray**        | **.NET 10.0**           | **.NET 10.0** | **3**          |  **6.554 ns** | **0.0225 ns** | **0.0315 ns** |  **6.518 ns** |  **6.646 ns** |  **6.596 ns** |  **1.00** |   **2,552 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | .NET 10.0           | .NET 10.0 | 3          |  6.510 ns | 0.0374 ns | 0.0512 ns |  6.439 ns |  6.693 ns |  6.555 ns |  0.99 |     995 B |         - |          NA |
|                        |                     |           |            |           |           |           |           |           |           |       |           |           |             |
| IndexOfAnyArray        | MediumRun-.NET 11.0 | .NET 11.0 | 3          |  6.530 ns | 0.0177 ns | 0.0259 ns |  6.488 ns |  6.587 ns |  6.559 ns |  1.00 |   2,585 B |         - |          NA |
| IndexOfAnySearchValues | MediumRun-.NET 11.0 | .NET 11.0 | 3          |  6.480 ns | 0.0180 ns | 0.0258 ns |  6.438 ns |  6.536 ns |  6.516 ns |  0.99 |     992 B |         - |          NA |
|                        |                     |           |            |           |           |           |           |           |           |       |           |           |             |
| **IndexOfAnyArray**        | **.NET 10.0**           | **.NET 10.0** | **8**          | **14.512 ns** | **0.0817 ns** | **0.1223 ns** | **14.343 ns** | **14.720 ns** | **14.660 ns** |  **1.00** |   **2,797 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | .NET 10.0           | .NET 10.0 | 8          |  4.581 ns | 0.1312 ns | 0.1882 ns |  4.353 ns |  4.909 ns |  4.817 ns |  0.32 |     623 B |         - |          NA |
|                        |                     |           |            |           |           |           |           |           |           |       |           |           |             |
| IndexOfAnyArray        | MediumRun-.NET 11.0 | .NET 11.0 | 8          | 14.154 ns | 0.0878 ns | 0.1231 ns | 13.952 ns | 14.332 ns | 14.288 ns |  1.00 |   2,905 B |         - |          NA |
| IndexOfAnySearchValues | MediumRun-.NET 11.0 | .NET 11.0 | 8          |  4.617 ns | 0.0288 ns | 0.0430 ns |  4.542 ns |  4.691 ns |  4.668 ns |  0.33 |     668 B |         - |          NA |
|                        |                     |           |            |           |           |           |           |           |           |       |           |           |             |
| **IndexOfAnyArray**        | **.NET 10.0**           | **.NET 10.0** | **32**         | **24.236 ns** | **0.1173 ns** | **0.1644 ns** | **23.992 ns** | **24.600 ns** | **24.443 ns** |  **1.00** |   **2,809 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | .NET 10.0           | .NET 10.0 | 32         |  4.669 ns | 0.1646 ns | 0.2463 ns |  4.371 ns |  5.024 ns |  4.958 ns |  0.19 |     623 B |         - |          NA |
|                        |                     |           |            |           |           |           |           |           |           |       |           |           |             |
| IndexOfAnyArray        | MediumRun-.NET 11.0 | .NET 11.0 | 32         | 24.177 ns | 0.2052 ns | 0.2877 ns | 23.810 ns | 24.857 ns | 24.517 ns |  1.00 |   2,905 B |         - |          NA |
| IndexOfAnySearchValues | MediumRun-.NET 11.0 | .NET 11.0 | 32         |  4.815 ns | 0.1720 ns | 0.2467 ns |  4.512 ns |  5.126 ns |  5.080 ns |  0.20 |     668 B |         - |          NA |

## Earlier record

- Verdict: adopted
- vs array overload: 0.96x (3 candidates) / 0.33x (8) / 0.20x (32); SearchValues time is flat (~4.5-5.5 ns) regardless of candidate count
- Code size 623-995 B vs 3,629-3,957 B
- R-07 remains valid: for 2-3 candidates the dedicated IndexOfAny(a, b, c) overload is still the fastest option; SearchValues wins over the ARRAY overload

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Candidates | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **IndexOfAnyArray**        | **3**          |  **5.661 ns** | **0.0683 ns** | **0.1001 ns** |  **5.570 ns** |  **5.998 ns** |  **5.816 ns** |  **1.00** |    **0.02** |   **3,629 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | 3          |  5.458 ns | 0.0262 ns | 0.0376 ns |  5.398 ns |  5.553 ns |  5.499 ns |  0.96 |    0.02 |     995 B |         - |          NA |
|                        |            |           |           |           |           |           |           |       |         |           |           |             |
| **IndexOfAnyArray**        | **8**          | **13.866 ns** | **0.1322 ns** | **0.1897 ns** | **13.644 ns** | **14.372 ns** | **14.166 ns** |  **1.00** |    **0.02** |   **3,957 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | 8          |  4.614 ns | 0.0817 ns | 0.1198 ns |  4.409 ns |  4.869 ns |  4.783 ns |  0.33 |    0.01 |     623 B |         - |          NA |
|                        |            |           |           |           |           |           |           |       |         |           |           |             |
| **IndexOfAnyArray**        | **32**         | **23.127 ns** | **0.1201 ns** | **0.1645 ns** | **22.890 ns** | **23.357 ns** | **23.315 ns** |  **1.00** |    **0.01** |   **3,957 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | 32         |  4.540 ns | 0.0580 ns | 0.0813 ns |  4.386 ns |  4.656 ns |  4.634 ns |  0.20 |    0.00 |     623 B |         - |          NA |
