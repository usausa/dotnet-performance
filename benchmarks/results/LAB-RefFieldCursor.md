# LAB: ref-field cursor for iteration (rejected, R-12)

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
