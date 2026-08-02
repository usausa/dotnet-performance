# TXT-08: SearchValues<char> vs IndexOfAny(char[])

- Verdict: adopted
- vs array overload: 0.94x (3 candidates) / 0.34x (8) / 0.18x (32); SearchValues time is flat (~6 ns) regardless of candidate count
- Code size 621 B vs 3,589-3,601 B
- R-07 remains valid: for 2-3 candidates the dedicated IndexOfAny(a, b, c) overload is still the fastest option; SearchValues wins over the ARRAY overload

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Candidates | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **IndexOfAnyArray**        | **3**          |  **7.126 ns** | **0.0796 ns** | **0.1167 ns** |  **6.935 ns** |  **7.426 ns** |  **7.277 ns** |  **1.00** |    **0.02** |   **3,099 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | 3          |  6.670 ns | 0.0350 ns | 0.0490 ns |  6.593 ns |  6.807 ns |  6.717 ns |  0.94 |    0.02 |     745 B |         - |          NA |
|                        |            |           |           |           |           |           |           |       |         |           |           |             |
| **IndexOfAnyArray**        | **8**          | **17.760 ns** | **0.3161 ns** | **0.4633 ns** | **17.200 ns** | **18.929 ns** | **18.407 ns** |  **1.00** |    **0.04** |   **3,589 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | 8          |  6.058 ns | 0.0495 ns | 0.0694 ns |  5.974 ns |  6.239 ns |  6.145 ns |  0.34 |    0.01 |     621 B |         - |          NA |
|                        |            |           |           |           |           |           |           |       |         |           |           |             |
| **IndexOfAnyArray**        | **32**         | **34.707 ns** | **0.3034 ns** | **0.4447 ns** | **33.893 ns** | **35.768 ns** | **35.403 ns** |  **1.00** |    **0.02** |   **3,601 B** |         **-** |          **NA** |
| IndexOfAnySearchValues | 32         |  6.149 ns | 0.0362 ns | 0.0531 ns |  6.062 ns |  6.282 ns |  6.196 ns |  0.18 |    0.00 |     621 B |         - |          NA |
