# ASY-02: System.Threading.Channels

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted
- The single reader/writer options still help: 0.83x of the default on net10 and 0.92x on net11
- Every channel path is slower on net11: unbounded default 1.02x, single reader/writer 1.14x, bounded 1.24x (allocation unchanged at 3-6 B per item)

### ChannelsBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| UnboundedDefault | 41.85 ns | 42.89 ns | 1.02 | 6 B → 5 B | 24,269 B → 28,815 B |
| UnboundedSingleReaderWriter | 34.66 ns | 39.56 ns | 1.14 | 4 B → 3 B | 22,943 B → 27,571 B |
| Bounded | 62.40 ns | 77.51 ns | 1.24 | - → - | 19,655 B → 22,182 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.06 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                      | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| UnboundedDefault            | .NET 10.0           | .NET 10.0 | 41.85 ns | 0.882 ns | 1.321 ns | 40.07 ns | 43.76 ns | 43.55 ns |  1.00 |    0.00 | 0.0008 |  24,269 B | 0.0001 |       6 B |        1.00 |
| UnboundedSingleReaderWriter | .NET 10.0           | .NET 10.0 | 34.66 ns | 0.232 ns | 0.332 ns | 33.96 ns | 35.31 ns | 35.09 ns |  0.83 |    0.03 | 0.0004 |  22,943 B |      - |       4 B |        0.67 |
| Bounded                     | .NET 10.0           | .NET 10.0 | 62.40 ns | 0.938 ns | 1.374 ns | 61.13 ns | 66.82 ns | 64.80 ns |  1.49 |    0.06 |      - |  19,655 B |      - |         - |        0.00 |
|                             |                     |           |          |          |          |          |          |          |       |         |        |           |        |           |             |
| UnboundedDefault            | MediumRun-.NET 11.0 | .NET 11.0 | 42.89 ns | 0.504 ns | 0.754 ns | 41.44 ns | 44.59 ns | 43.71 ns |  1.00 |    0.00 | 0.0005 |  28,815 B |      - |       5 B |        1.00 |
| UnboundedSingleReaderWriter | MediumRun-.NET 11.0 | .NET 11.0 | 39.56 ns | 4.458 ns | 6.673 ns | 34.31 ns | 54.39 ns | 52.05 ns |  0.92 |    0.15 | 0.0003 |  27,571 B |      - |       3 B |        0.60 |
| Bounded                     | MediumRun-.NET 11.0 | .NET 11.0 | 77.51 ns | 1.610 ns | 2.410 ns | 74.19 ns | 84.76 ns | 80.41 ns |  1.81 |    0.06 |      - |  22,182 B |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted
- ~39 ns/item unbounded (producer/consumer pump, 10,000 items)
- SingleReader/SingleWriter options: 0.89x (non-overlapping CIs) - a modest real gain even single-producer/single-consumer
- Bounded(128): 1.63x - the price of backpressure

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                      | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| UnboundedDefault            | 38.78 ns | 0.271 ns | 0.389 ns | 38.18 ns | 39.66 ns | 39.22 ns |  1.00 |    0.01 | 0.0003 |  26,238 B |       3 B |        1.00 |
| UnboundedSingleReaderWriter | 34.70 ns | 0.369 ns | 0.517 ns | 33.98 ns | 35.81 ns | 35.47 ns |  0.89 |    0.02 | 0.0002 |  21,237 B |       2 B |        0.67 |
| Bounded                     | 63.02 ns | 0.619 ns | 0.868 ns | 61.17 ns | 64.74 ns | 64.28 ns |  1.63 |    0.03 |      - |  24,335 B |         - |        0.00 |
