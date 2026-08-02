# R-18 (was BIT-01): Unsigned single-comparison range check

- Verdict: no effect on net10 (JIT already fuses the two-comparison form)
- 548.5 vs 553.7 ns (CIs overlap), Code Size 60 B both
- Tier1 JitDisasm: both forms compile to the same unsigned trick - the only difference is 'sub r8d, 100' vs 'add r8d, -100' (identical semantics/size)
- Keep the manual form only for compound conditions the JIT cannot prove; for plain min/max checks write the readable two-comparison form

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| TwoComparisons           | 548.5 ns | 34.19 ns | 50.11 ns | 491.8 ns | 642.4 ns | 623.0 ns |  1.01 |    0.13 |      60 B |         - |          NA |
| UnsignedSingleComparison | 553.7 ns | 22.42 ns | 32.86 ns | 505.7 ns | 624.5 ns | 595.7 ns |  1.02 |    0.11 |      60 B |         - |          NA |
