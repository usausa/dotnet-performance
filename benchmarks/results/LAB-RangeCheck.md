# R-18 (was BIT-01): Unsigned single-comparison range check

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - both forms compile to the same 60 B on both generations (210.7 vs 211.9 ns on net11)

### RangeCheckBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| TwoComparisons | 215.7 ns | 210.7 ns | 0.98 | - → - | 60 B → 60 B |
| UnsignedSingleComparison | 213.4 ns | 211.9 ns | 0.99 | - → - | 60 B → 60 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.64 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                   | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| TwoComparisons           | .NET 10.0           | .NET 10.0 | 215.7 ns | 2.05 ns | 3.00 ns | 210.7 ns | 221.2 ns | 219.4 ns |  1.00 |    0.00 |      60 B |         - |          NA |
| UnsignedSingleComparison | .NET 10.0           | .NET 10.0 | 213.4 ns | 2.16 ns | 3.17 ns | 208.7 ns | 223.3 ns | 216.5 ns |  0.99 |    0.02 |      60 B |         - |          NA |
|                          |                     |           |          |         |         |          |          |          |       |         |           |           |             |
| TwoComparisons           | MediumRun-.NET 11.0 | .NET 11.0 | 210.7 ns | 0.82 ns | 1.18 ns | 209.4 ns | 213.5 ns | 212.8 ns |  1.00 |    0.00 |      60 B |         - |          NA |
| UnsignedSingleComparison | MediumRun-.NET 11.0 | .NET 11.0 | 211.9 ns | 1.57 ns | 2.30 ns | 209.4 ns | 218.8 ns | 214.6 ns |  1.01 |    0.01 |      60 B |         - |          NA |

## Earlier record

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
