# TXT-06: Ascii class comparison

## net11 / net10 re-verification (2026-11-18, HX 370)

- **Verdict: reversed on net11.** `string.Equals(..., OrdinalIgnoreCase)` dropped from 57.0 to 22.1 ns (code 1,828 → 967 B, disjoint intervals), so `Ascii.EqualsIgnoreCase` is now **2.16x slower** (47.7 ns) and even the manual OR-0x20 compare is 1.47x slower
- On net10 the ordering of the earlier record still holds (Ascii 0.77x, manual 0.57x of `string.Equals`)
- On net11 the plain `string.Equals(OrdinalIgnoreCase)` is the choice; a multi-target that includes net10 trades 1.31x on net10 for 2.16x on net11

### AsciiBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| StringEqualsIgnoreCase | 57.03 ns | 22.12 ns | 0.39 | - → - | 1,828 B → 967 B |
| AsciiEqualsIgnoreCase | 43.64 ns | 47.71 ns | 1.09 | - → - | 1,215 B → 1,227 B |
| ManualOr20Compare | 32.53 ns | 32.45 ns | 1.00 | - → - | 242 B → 242 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.63 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                 | Job                 | Runtime   | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| StringEqualsIgnoreCase | .NET 10.0           | .NET 10.0 | 57.03 ns | 0.379 ns | 0.544 ns | 56.93 ns | 56.25 ns | 58.17 ns | 57.94 ns |  1.00 |    0.00 |   1,828 B |         - |          NA |
| AsciiEqualsIgnoreCase  | .NET 10.0           | .NET 10.0 | 43.64 ns | 2.246 ns | 3.292 ns | 46.27 ns | 40.05 ns | 47.51 ns | 47.13 ns |  0.77 |    0.06 |   1,215 B |         - |          NA |
| ManualOr20Compare      | .NET 10.0           | .NET 10.0 | 32.53 ns | 0.151 ns | 0.201 ns | 32.57 ns | 32.13 ns | 33.02 ns | 32.76 ns |  0.57 |    0.01 |     242 B |         - |          NA |
|                        |                     |           |          |          |          |          |          |          |          |       |         |           |           |             |
| StringEqualsIgnoreCase | MediumRun-.NET 11.0 | .NET 11.0 | 22.12 ns | 0.206 ns | 0.296 ns | 22.00 ns | 21.76 ns | 22.89 ns | 22.55 ns |  1.00 |    0.00 |     967 B |         - |          NA |
| AsciiEqualsIgnoreCase  | MediumRun-.NET 11.0 | .NET 11.0 | 47.71 ns | 0.368 ns | 0.540 ns | 47.79 ns | 46.46 ns | 48.62 ns | 48.28 ns |  2.16 |    0.04 |   1,227 B |         - |          NA |
| ManualOr20Compare      | MediumRun-.NET 11.0 | .NET 11.0 | 32.45 ns | 0.146 ns | 0.215 ns | 32.47 ns | 32.10 ns | 32.91 ns | 32.68 ns |  1.47 |    0.02 |     242 B |         - |          NA |

## Earlier record

- Verdict: adopted
- Ascii.EqualsIgnoreCase (bytes) 0.76x vs string.Equals(OrdinalIgnoreCase)
- Manual (b | 0x20) compare 0.59x, but collides on symbol pairs ('@' vs backquote) - closed token sets only

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| StringEqualsIgnoreCase | 55.47 ns | 0.190 ns | 0.284 ns | 55.05 ns | 55.95 ns | 55.86 ns |  1.00 |    0.01 |   1,848 B |         - |          NA |
| AsciiEqualsIgnoreCase  | 41.94 ns | 2.897 ns | 4.336 ns | 39.49 ns | 57.22 ns | 48.95 ns |  0.76 |    0.08 |   1,215 B |         - |          NA |
| ManualOr20Compare      | 32.66 ns | 0.321 ns | 0.461 ns | 32.03 ns | 33.33 ns | 33.22 ns |  0.59 |    0.01 |     242 B |         - |          NA |
