# JIT-01: AggressiveInlining on a loop-containing helper

- Verdict: attribute had no effect in this shape (default policy already inlined); NoInlining shows the stake
- Default 0.943 us vs Aggressive 0.959 us (CIs overlap) - call-site codegen IDENTICAL (100 B both): net10 default policy inlines the loop-containing helper
- NoInline 1.180 us: non-overlapping CIs vs default (+25%) - inlining itself matters; the attribute is insurance for shapes the heuristic declines

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DefaultPolicy |   943.1 ns |  4.21 ns |  5.48 ns |   934.2 ns |   955.6 ns |   949.8 ns |  1.00 |    0.01 |     100 B |         - |          NA |
| Aggressive    |   958.9 ns | 18.46 ns | 27.63 ns |   935.9 ns | 1,033.5 ns |   997.3 ns |  1.02 |    0.03 |     100 B |         - |          NA |
| NoInline      | 1,180.1 ns | 13.87 ns | 20.33 ns | 1,142.7 ns | 1,225.2 ns | 1,204.6 ns |  1.25 |    0.02 |     107 B |         - |          NA |
