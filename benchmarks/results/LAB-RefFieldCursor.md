# LAB: ref-field cursor for iteration (rejected, R-12)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - rejected. The ref-field cursor equals the indexed span loop (1.00x) and the span cursor stays 1.32x on net11

### RefFieldCursorBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| SumSpanIndex | 211.3 ns | 210.4 ns | 1.00 | - → - | 53 B → 53 B |
| SumSpanCursor | 274.6 ns | 278.0 ns | 1.01 | - → - | 78 B → 78 B |
| SumRefFieldCursor | 210.6 ns | 210.8 ns | 1.00 | - → - | 56 B → 56 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.53 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SumSpanIndex      | .NET 10.0           | .NET 10.0 | 211.3 ns | 0.77 ns |  1.11 ns | 209.7 ns | 214.6 ns | 212.6 ns |  1.00 |    0.00 |      53 B |         - |          NA |
| SumSpanCursor     | .NET 10.0           | .NET 10.0 | 274.6 ns | 2.29 ns |  3.43 ns | 269.7 ns | 281.6 ns | 278.8 ns |  1.30 |    0.02 |      78 B |         - |          NA |
| SumRefFieldCursor | .NET 10.0           | .NET 10.0 | 210.6 ns | 0.45 ns |  0.64 ns | 209.5 ns | 212.1 ns | 211.3 ns |  1.00 |    0.01 |      56 B |         - |          NA |
|                   |                     |           |          |         |          |          |          |          |       |         |           |           |             |
| SumSpanIndex      | MediumRun-.NET 11.0 | .NET 11.0 | 210.4 ns | 0.41 ns |  0.59 ns | 209.4 ns | 212.2 ns | 211.2 ns |  1.00 |    0.00 |      53 B |         - |          NA |
| SumSpanCursor     | MediumRun-.NET 11.0 | .NET 11.0 | 278.0 ns | 7.29 ns | 10.45 ns | 269.9 ns | 314.5 ns | 289.9 ns |  1.32 |    0.05 |      78 B |         - |          NA |
| SumRefFieldCursor | MediumRun-.NET 11.0 | .NET 11.0 | 210.8 ns | 0.61 ns |  0.86 ns | 209.4 ns | 213.6 ns | 211.5 ns |  1.00 |    0.00 |      56 B |         - |          NA |

## Earlier record

- Verdict: rejected (for iteration use)
- 1.02x vs plain span for-loop (209.5 ns baseline) - no gain even where it does not hurt
- SpanReader per-element Read(): 1.34x - cursors are for field-granularity structured reads

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SumSpanIndex      | 209.5 ns | 0.31 ns | 0.42 ns | 208.6 ns | 210.2 ns | 210.0 ns |  1.00 |    0.00 |      53 B |         - |          NA |
| SumSpanCursor     | 280.3 ns | 2.58 ns | 3.62 ns | 276.5 ns | 291.1 ns | 284.7 ns |  1.34 |    0.02 |      78 B |         - |          NA |
| SumRefFieldCursor | 214.2 ns | 5.37 ns | 7.70 ns | 209.1 ns | 237.7 ns | 225.1 ns |  1.02 |    0.04 |      56 B |         - |          NA |
