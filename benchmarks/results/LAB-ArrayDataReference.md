# R-02 (was MEM-02): GetArrayDataReference

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - no speed benefit. The sequential ref walk stays 1.05x of the plain for, random access is equal, and the code sizes are identical on both generations

### ArrayDataReferenceBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| SequentialFor | 238.9 ns | 237.8 ns | 1.00 | - → - | 67 B → 67 B |
| SequentialRefWalk | 250.3 ns | 249.4 ns | 1.00 | - → - | 55 B → 55 B |
| RandomIndexed | 247.4 ns | 247.6 ns | 1.00 | - → - | 72 B → 72 B |
| RandomRefAdd | 245.9 ns | 245.2 ns | 1.00 | - → - | 55 B → 55 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.67 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| SequentialFor     | .NET 10.0           | .NET 10.0 | 238.9 ns | 1.13 ns | 1.69 ns | 236.1 ns | 242.7 ns | 241.1 ns |  1.00 |      67 B |         - |          NA |
| SequentialRefWalk | .NET 10.0           | .NET 10.0 | 250.3 ns | 1.37 ns | 2.01 ns | 248.1 ns | 256.2 ns | 252.8 ns |  1.05 |      55 B |         - |          NA |
| RandomIndexed     | .NET 10.0           | .NET 10.0 | 247.4 ns | 1.67 ns | 2.34 ns | 243.8 ns | 254.2 ns | 249.9 ns |  1.04 |      72 B |         - |          NA |
| RandomRefAdd      | .NET 10.0           | .NET 10.0 | 245.9 ns | 1.27 ns | 1.77 ns | 243.4 ns | 250.6 ns | 248.3 ns |  1.03 |      55 B |         - |          NA |
|                   |                     |           |          |         |         |          |          |          |       |           |           |             |
| SequentialFor     | MediumRun-.NET 11.0 | .NET 11.0 | 237.8 ns | 0.98 ns | 1.44 ns | 235.9 ns | 241.0 ns | 239.8 ns |  1.00 |      67 B |         - |          NA |
| SequentialRefWalk | MediumRun-.NET 11.0 | .NET 11.0 | 249.4 ns | 0.92 ns | 1.29 ns | 247.9 ns | 253.1 ns | 250.7 ns |  1.05 |      55 B |         - |          NA |
| RandomIndexed     | MediumRun-.NET 11.0 | .NET 11.0 | 247.6 ns | 1.46 ns | 2.09 ns | 244.6 ns | 251.8 ns | 251.0 ns |  1.04 |      72 B |         - |          NA |
| RandomRefAdd      | MediumRun-.NET 11.0 | .NET 11.0 | 245.2 ns | 0.88 ns | 1.24 ns | 244.0 ns | 248.9 ns | 246.8 ns |  1.03 |      55 B |         - |          NA |

## Earlier record

- Verdict: NO speed benefit on net10 - use only where a ref (not a Span) is structurally required
- Sequential walk: ref form 248.2 ns vs plain for 236.2 ns (1.05x slower - the indexed form auto-vectorizes, same finding as MEM-01/R-02)
- Random access with mask-guaranteed indices: 245.2 vs 246.3 ns (CIs overlap - the bounds check the ref form removes is free here)
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
