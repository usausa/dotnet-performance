# MEM-01: SkipLocalsInit (stackalloc zero-init cost)

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
