# JIT-03: typeof(T) branch specialization

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the typeof branch is free on both generations (220.0 vs 220.2 ns, 35 vs 32 B)

### TypeofBranchBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| HandwrittenIntSum | 220.2 ns | 220.2 ns | 1.00 | - → - | 32 B → 32 B |
| GenericWithTypeofBranch | 220.0 ns | 220.0 ns | 1.00 | - → - | 35 B → 35 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.86 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------------ |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| HandwrittenIntSum       | .NET 10.0           | .NET 10.0 | 220.2 ns | 0.45 ns | 0.63 ns | 218.8 ns | 221.9 ns | 220.9 ns |  1.00 |      32 B |         - |          NA |
| GenericWithTypeofBranch | .NET 10.0           | .NET 10.0 | 220.0 ns | 0.33 ns | 0.46 ns | 219.0 ns | 220.9 ns | 220.6 ns |  1.00 |      35 B |         - |          NA |
|                         |                     |           |          |         |         |          |          |          |       |           |           |             |
| HandwrittenIntSum       | MediumRun-.NET 11.0 | .NET 11.0 | 220.2 ns | 0.41 ns | 0.59 ns | 219.0 ns | 221.5 ns | 220.8 ns |  1.00 |      32 B |         - |          NA |
| GenericWithTypeofBranch | MediumRun-.NET 11.0 | .NET 11.0 | 220.0 ns | 0.52 ns | 0.78 ns | 218.8 ns | 222.3 ns | 221.0 ns |  1.00 |      35 B |         - |          NA |

## Earlier record

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
