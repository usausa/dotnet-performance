# R-02 (was MEM-02): GetArrayDataReference

- Verdict: NO speed benefit on net10 - use only where a ref (not a Span) is structurally required
- Sequential walk: ref form 595.7 ns vs plain for 457.4 ns (1.30x SLOWER - defeats auto-vectorization, same finding as MEM-01/R-02)
- Random access with mask-guaranteed indices: 461.4 vs 466.2 ns (CIs overlap - the bounds check the ref form removes is free here)
- Code size is smaller (55 vs 67-72 B) but that is the only measured advantage

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| SequentialFor     | 236.2 ns | 0.91 ns | 1.30 ns | 234.6 ns | 240.2 ns | 237.7 ns |  1.00 |      67 B |         - |          NA |
| SequentialRefWalk | 248.2 ns | 0.79 ns | 1.10 ns | 246.3 ns | 251.1 ns | 249.8 ns |  1.05 |      55 B |         - |          NA |
| RandomIndexed     | 246.3 ns | 1.05 ns | 1.48 ns | 244.2 ns | 249.9 ns | 248.4 ns |  1.04 |      72 B |         - |          NA |
| RandomRefAdd      | 245.2 ns | 1.12 ns | 1.61 ns | 243.1 ns | 249.4 ns | 247.6 ns |  1.04 |      55 B |         - |          NA |
