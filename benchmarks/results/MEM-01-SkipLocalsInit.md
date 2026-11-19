# MEM-01: SkipLocalsInit (stackalloc zero-init cost)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - SkipLocalsInit is 0.09x on both generations (1.69 vs 18.6 ns on net11)

### SkipLocalsInitBenchmark

net10 → net11 per row:

| Method | Size | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| ZeroInit | 512 | 18.774 ns | 18.618 ns | 0.99 | - → - | 605 B → 645 B |
| SkipInit | 512 | 1.742 ns | 1.697 ns | 0.97 | - → - | 177 B → 182 B |
| ZeroInit | 4096 | 18.923 ns | 18.592 ns | 0.98 | - → - | 605 B → 645 B |
| SkipInit | 4096 | 1.737 ns | 1.690 ns | 0.97 | - → - | 177 B → 182 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.49 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method   | Job                 | Runtime   | Size | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|--------- |-------------------- |---------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| **ZeroInit** | **.NET 10.0**           | **.NET 10.0** | **512**  | **18.774 ns** | **0.2005 ns** | **0.2938 ns** | **18.353 ns** | **19.216 ns** | **19.104 ns** |  **1.00** |     **605 B** |         **-** |          **NA** |
| SkipInit | .NET 10.0           | .NET 10.0 | 512  |  1.742 ns | 0.0240 ns | 0.0352 ns |  1.690 ns |  1.808 ns |  1.786 ns |  0.09 |     177 B |         - |          NA |
|          |                     |           |      |           |           |           |           |           |           |       |           |           |             |
| ZeroInit | MediumRun-.NET 11.0 | .NET 11.0 | 512  | 18.618 ns | 0.3099 ns | 0.4242 ns | 18.114 ns | 19.571 ns | 19.133 ns |  1.00 |     645 B |         - |          NA |
| SkipInit | MediumRun-.NET 11.0 | .NET 11.0 | 512  |  1.697 ns | 0.0090 ns | 0.0129 ns |  1.670 ns |  1.729 ns |  1.706 ns |  0.09 |     182 B |         - |          NA |
|          |                     |           |      |           |           |           |           |           |           |       |           |           |             |
| **ZeroInit** | **.NET 10.0**           | **.NET 10.0** | **4096** | **18.923 ns** | **0.0902 ns** | **0.1322 ns** | **18.731 ns** | **19.126 ns** | **19.088 ns** |  **1.00** |     **605 B** |         **-** |          **NA** |
| SkipInit | .NET 10.0           | .NET 10.0 | 4096 |  1.737 ns | 0.0288 ns | 0.0430 ns |  1.682 ns |  1.799 ns |  1.790 ns |  0.09 |     177 B |         - |          NA |
|          |                     |           |      |           |           |           |           |           |           |       |           |           |             |
| ZeroInit | MediumRun-.NET 11.0 | .NET 11.0 | 4096 | 18.592 ns | 0.1991 ns | 0.2980 ns | 18.111 ns | 19.065 ns | 18.931 ns |  1.00 |     645 B |         - |          NA |
| SkipInit | MediumRun-.NET 11.0 | .NET 11.0 | 4096 |  1.690 ns | 0.0102 ns | 0.0144 ns |  1.674 ns |  1.741 ns |  1.704 ns |  0.09 |     182 B |         - |          NA |

## Earlier record

- Verdict: adopted
- stackalloc byte[4096] zero-init costs ~17 ns; [SkipLocalsInit] drops the call to 1.6 ns (0.09x, ~11x faster)
- Code size 604 B -> 177 B (the memset path disappears)
- Note: the stackalloc size is fixed at 4096 in both Size params (only the slice length varies), so the init cost is identical across rows - it scales with the stackalloc size, not the used length

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method   | Size | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **ZeroInit** | **512**  | **18.630 ns** | **0.1977 ns** | **0.2571 ns** | **18.267 ns** | **19.115 ns** | **18.972 ns** |  **1.00** |    **0.02** |     **604 B** |         **-** |          **NA** |
| SkipInit | 512  |  1.626 ns | 0.0116 ns | 0.0163 ns |  1.604 ns |  1.664 ns |  1.643 ns |  0.09 |    0.00 |     177 B |         - |          NA |
|          |      |           |           |           |           |           |           |       |         |           |           |             |
| **ZeroInit** | **4096** | **19.068 ns** | **0.4014 ns** | **0.5494 ns** | **18.408 ns** | **20.246 ns** | **19.663 ns** |  **1.00** |    **0.04** |     **604 B** |         **-** |          **NA** |
| SkipInit | 4096 |  1.628 ns | 0.0088 ns | 0.0121 ns |  1.613 ns |  1.656 ns |  1.647 ns |  0.09 |    0.00 |     177 B |         - |          NA |
