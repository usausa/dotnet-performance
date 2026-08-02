# MEM-01: SkipLocalsInit (stackalloc zero-init cost)

- Verdict: adopted
- stackalloc byte[4096] zero-init costs ~31 ns; [SkipLocalsInit] drops the call to 3.1-3.3 ns (0.09x, ~11x faster)
- Code size 610 B -> 177 B (the memset path disappears)
- Note: the stackalloc size is fixed at 4096 in both Size params (only the slice length varies), so the init cost is identical across rows - it scales with the stackalloc size, not the used length

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method   | Size | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **ZeroInit** | **512**  | **34.275 ns** | **0.6301 ns** | **0.9236 ns** | **34.124 ns** | **33.173 ns** | **36.254 ns** | **35.575 ns** |  **1.00** |    **0.04** |     **610 B** |         **-** |          **NA** |
| SkipInit | 512  |  3.145 ns | 0.0500 ns | 0.0748 ns |  3.139 ns |  3.028 ns |  3.277 ns |  3.249 ns |  0.09 |    0.00 |     177 B |         - |          NA |
|          |      |           |           |           |           |           |           |           |       |         |           |           |             |
| **ZeroInit** | **4096** | **34.745 ns** | **0.4554 ns** | **0.6674 ns** | **34.379 ns** | **33.644 ns** | **35.881 ns** | **35.575 ns** |  **1.00** |    **0.03** |     **610 B** |         **-** |          **NA** |
| SkipInit | 4096 |  3.254 ns | 0.1225 ns | 0.1834 ns |  3.188 ns |  3.087 ns |  3.812 ns |  3.551 ns |  0.09 |    0.01 |     177 B |         - |          NA |
