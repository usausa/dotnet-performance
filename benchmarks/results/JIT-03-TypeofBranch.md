# JIT-03: typeof(T) branch specialization

- Verdict: adopted (the branch is free)
- Generic with typeof branch 212.4 ns vs handwritten int sum 213.7 ns - parity; code size 35 vs 32 B (near-identical, branch folded per instantiation)
- The claim verified is 'no cost': the JIT folds typeof(T) == typeof(int) to a constant per instantiation; fallback path covered by Verify

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| HandwrittenIntSum       | 213.7 ns | 2.60 ns | 3.72 ns | 210.6 ns | 223.3 ns | 220.4 ns |  1.00 |    0.02 |      32 B |         - |          NA |
| GenericWithTypeofBranch | 212.4 ns | 1.18 ns | 1.62 ns | 210.6 ns | 216.7 ns | 214.8 ns |  0.99 |    0.02 |      35 B |         - |          NA |
