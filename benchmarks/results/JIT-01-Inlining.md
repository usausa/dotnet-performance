# JIT-01: AggressiveInlining on a loop-containing helper

- Verdict: attribute had no effect in this shape (default policy already inlined); NoInlining shows the stake
- Default 1.451 us vs Aggressive 1.338 us (CIs overlap) - call-site codegen verified IDENTICAL (94 B both, Tier1 JitDisasm): net10 default policy inlines the loop-containing helper
- NoInline 1.560 us: non-overlapping CIs vs Aggressive (+17%) - inlining itself matters; the attribute is insurance for shapes the heuristic declines

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| DefaultPolicy | 1.451 μs | 0.0757 μs | 0.1109 μs | 1.346 μs | 1.727 μs | 1.618 μs |  1.01 |    0.10 |     100 B |         - |          NA |
| Aggressive    | 1.338 μs | 0.0244 μs | 0.0365 μs | 1.267 μs | 1.420 μs | 1.381 μs |  0.93 |    0.07 |     100 B |         - |          NA |
| NoInline      | 1.560 μs | 0.0187 μs | 0.0280 μs | 1.511 μs | 1.607 μs | 1.596 μs |  1.08 |    0.08 |     107 B |         - |          NA |
