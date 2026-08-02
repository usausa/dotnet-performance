# R-02 (was MEM-02): GetArrayDataReference

- Verdict: NO speed benefit on net10 - use only where a ref (not a Span) is structurally required
- Sequential walk: ref form 595.7 ns vs plain for 457.4 ns (1.30x SLOWER - defeats auto-vectorization, same finding as MEM-01/R-02)
- Random access with mask-guaranteed indices: 461.4 vs 466.2 ns (CIs overlap - the bounds check the ref form removes is free here)
- Code size is smaller (55 vs 67-72 B) but that is the only measured advantage

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SequentialFor     | 457.4 ns |  4.40 ns |  6.58 ns | 449.0 ns | 472.5 ns | 468.5 ns |  1.00 |    0.02 |      67 B |         - |          NA |
| SequentialRefWalk | 595.7 ns | 13.13 ns | 19.65 ns | 571.2 ns | 625.5 ns | 623.9 ns |  1.30 |    0.05 |      55 B |         - |          NA |
| RandomIndexed     | 466.2 ns |  9.48 ns | 14.18 ns | 448.5 ns | 506.9 ns | 481.0 ns |  1.02 |    0.03 |      72 B |         - |          NA |
| RandomRefAdd      | 461.4 ns | 15.52 ns | 23.23 ns | 432.2 ns | 511.5 ns | 492.3 ns |  1.01 |    0.05 |      55 B |         - |          NA |
