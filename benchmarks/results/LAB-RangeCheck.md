# R-18 (was BIT-01): Unsigned single-comparison range check

- Verdict: no effect on net10 (JIT already fuses the two-comparison form)
- 210.9 vs 211.7 ns (CIs overlap), Code Size 60 B both
- Tier1 JitDisasm: both forms compile to the same unsigned trick - the only difference is 'sub r8d, 100' vs 'add r8d, -100' (identical semantics/size)
- Keep the manual form only for compound conditions the JIT cannot prove; for plain min/max checks write the readable two-comparison form

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| TwoComparisons           | 210.9 ns | 1.50 ns | 2.06 ns | 208.3 ns | 215.7 ns | 213.8 ns |  1.00 |    0.01 |      60 B |         - |          NA |
| UnsignedSingleComparison | 211.7 ns | 2.32 ns | 3.25 ns | 208.3 ns | 221.8 ns | 216.8 ns |  1.00 |    0.02 |      60 B |         - |          NA |
