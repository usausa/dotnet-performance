# COL-07: GetValueRefOrNullRef + Unsafe.IsNullRef (optional ref lookup)

- Verdict: adopted for the update path only
- Update: 0.48x (all hit), 0.62x (half miss), 0.51x (32 B value) against TryGetValue + indexer write-back
- Code size is where it shows first: the indexer setter drags the whole insert path into the caller
  (10 methods, 8,270 B) while the ref form needs 2 methods, 1,080 B
- The ratio is the same for an 8 B value and a 32 B value, so the gain is the single hash probe, not the
  avoided value copy
- Read path: no difference. 0.98-1.00x with overlapping CIs, identical instruction counts (199 vs 199 all hit),
  and code size moves both ways (+16 B / -14 B / -4 B). Keep TryGetValue for read-only lookups
- The ContainsKey + indexer shape is not measured because CA1854 rejects it at build time in this repository
- Probe keys are separate string instances from the stored keys, so reference equality cannot short-circuit

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Probe    | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------------- |--------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **TryGetValueRead**              | **AllHit**   | **2.327 μs** | **0.0666 μs** | **0.0997 μs** | **2.212 μs** | **2.514 μs** | **2.461 μs** |  **1.00** |    **0.06** |   **1,055 B** |         **-** |          **NA** |
| ValueRefOrNullRefRead        | AllHit   | 2.287 μs | 0.0637 μs | 0.0953 μs | 2.166 μs | 2.458 μs | 2.410 μs |  0.98 |    0.06 |   1,071 B |         - |          NA |
| TryGetValueThenIndexerUpdate | AllHit   | 4.997 μs | 0.1069 μs | 0.1600 μs | 4.755 μs | 5.235 μs | 5.202 μs |  2.15 |    0.11 |   8,270 B |         - |          NA |
| ValueRefOrNullRefUpdate      | AllHit   | 2.401 μs | 0.0637 μs | 0.0954 μs | 2.289 μs | 2.602 μs | 2.546 μs |  1.03 |    0.06 |   1,080 B |         - |          NA |
|                              |          |          |           |           |          |          |          |       |         |           |           |             |
| **TryGetValueRead**              | **HalfMiss** | **1.909 μs** | **0.0566 μs** | **0.0847 μs** | **1.793 μs** | **2.066 μs** | **2.023 μs** |  **1.00** |    **0.06** |     **906 B** |         **-** |          **NA** |
| ValueRefOrNullRefRead        | HalfMiss | 1.893 μs | 0.0493 μs | 0.0738 μs | 1.774 μs | 2.003 μs | 1.981 μs |  0.99 |    0.06 |     892 B |         - |          NA |
| TryGetValueThenIndexerUpdate | HalfMiss | 3.185 μs | 0.0818 μs | 0.1224 μs | 3.019 μs | 3.502 μs | 3.329 μs |  1.67 |    0.10 |   8,051 B |         - |          NA |
| ValueRefOrNullRefUpdate      | HalfMiss | 1.967 μs | 0.0603 μs | 0.0903 μs | 1.878 μs | 2.131 μs | 2.108 μs |  1.03 |    0.06 |     895 B |         - |          NA |

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| TryGetValueRead              | 2.486 μs | 0.0556 μs | 0.0832 μs | 2.383 μs | 2.632 μs | 2.607 μs |  1.00 |    0.05 |   1,087 B |         - |          NA |
| ValueRefOrNullRefRead        | 2.485 μs | 0.0667 μs | 0.0998 μs | 2.341 μs | 2.653 μs | 2.634 μs |  1.00 |    0.05 |   1,083 B |         - |          NA |
| TryGetValueThenIndexerUpdate | 5.028 μs | 0.1254 μs | 0.1798 μs | 4.853 μs | 5.466 μs | 5.290 μs |  2.03 |    0.10 |   3,294 B |         - |          NA |
| ValueRefOrNullRefUpdate      | 2.560 μs | 0.0747 μs | 0.1117 μs | 2.446 μs | 2.813 μs | 2.724 μs |  1.03 |    0.06 |   1,088 B |         - |          NA |

