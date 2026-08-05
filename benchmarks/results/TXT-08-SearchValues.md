# TXT-08: SearchValues<char> vs IndexOfAny(char[])

- Verdict: adopted
- vs array overload: 0.94x (3 candidates) / 0.34x (8) / 0.18x (32); SearchValues time is flat (~6 ns) regardless of candidate count
- Code size 621 B vs 3,589-3,601 B
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
