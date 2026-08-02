# JIT-03: typeof(T) branch specialization

- Verdict: adopted (the branch is free)
- Generic with typeof branch 250.5 ns vs handwritten int sum 299.7 ns - parity or better; code size 35 vs 32 B (near-identical, branch folded per instantiation)
- The apparent 16% win is micro-variance (alignment), not a real advantage of the generic form; the claim verified is 'no cost', fallback path covered by Verify

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| HandwrittenIntSum       | 299.7 ns | 18.46 ns | 27.63 ns | 267.4 ns | 373.1 ns | 343.2 ns |  1.01 |    0.12 |      32 B |         - |          NA |
| GenericWithTypeofBranch | 250.5 ns | 15.54 ns | 22.77 ns | 227.6 ns | 313.2 ns | 280.7 ns |  0.84 |    0.10 |      35 B |         - |          NA |
