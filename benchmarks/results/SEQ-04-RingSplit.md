# SEQ-04: Incremental delimiter search + deferred compaction

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. Incremental scan + deferred compaction is 0.80x of the naive rescan on net10 and 0.84x on net11 (1.37 vs 1.63 μs); the naive path got 0.95x faster

### RingSplitBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| NaiveRescanCompact | 1.709 μs | 1.627 μs | 0.95 | - → - | 1,799 B → 1,956 B |
| IncrementalDeferredCompact | 1.363 μs | 1.371 μs | 1.01 | - → - | 1,876 B → 2,009 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.25 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                     | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| NaiveRescanCompact         | .NET 10.0           | .NET 10.0 | 1.709 μs | 0.0370 μs | 0.0553 μs | 1.627 μs | 1.789 μs | 1.776 μs |  1.00 |    0.00 |   1,799 B |         - |          NA |
| IncrementalDeferredCompact | .NET 10.0           | .NET 10.0 | 1.363 μs | 0.0024 μs | 0.0037 μs | 1.355 μs | 1.369 μs | 1.367 μs |  0.80 |    0.03 |   1,876 B |         - |          NA |
|                            |                     |           |          |           |           |          |          |          |       |         |           |           |             |
| NaiveRescanCompact         | MediumRun-.NET 11.0 | .NET 11.0 | 1.627 μs | 0.0076 μs | 0.0110 μs | 1.600 μs | 1.647 μs | 1.638 μs |  1.00 |    0.00 |   1,956 B |         - |          NA |
| IncrementalDeferredCompact | MediumRun-.NET 11.0 | .NET 11.0 | 1.371 μs | 0.0034 μs | 0.0048 μs | 1.365 μs | 1.382 μs | 1.378 μs |  0.84 |    0.01 |   2,009 B |         - |          NA |

## Earlier record

- Verdict: adopted
- 0.67x vs naive full-rescan + per-line compaction (2 KB lines fed in 256 B chunks, 32 KB total; 1.70 -> 1.13 us)
- Both zero-alloc; the win comes from not re-scanning already-searched bytes and moving data once instead of per line
- Ring wrap-around (two-segment IndexOf) not exercised; measured in flat-buffer form

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| NaiveRescanCompact         | 1.695 μs | 0.0388 μs | 0.0543 μs | 1.666 μs | 1.904 μs | 1.762 μs |  1.00 |    0.04 |   1,772 B |         - |          NA |
| IncrementalDeferredCompact | 1.128 μs | 0.0054 μs | 0.0079 μs | 1.111 μs | 1.143 μs | 1.138 μs |  0.67 |    0.02 |   1,853 B |         - |          NA |
