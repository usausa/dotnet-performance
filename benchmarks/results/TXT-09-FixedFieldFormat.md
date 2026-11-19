# TXT-09: Fixed-field formatting idioms

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - mixed. TryFormat + Fill stays the fastest (3.75 ns on net11); the manual digit forms are 2.73x / 3.04x of it
- Vectorized trim is 0.51x of the manual loop on net11 (2.87 vs 5.62 ns; 0.49x on net10)

### FixedFieldFormatBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| TryFormatThenFill | 4.050 ns | 3.753 ns | 0.93 | - → - | 1,811 B → 2,560 B |
| ManualLsbThenReverse | 10.195 ns | 10.263 ns | 1.01 | - → - | 750 B → 759 B |
| ManualRightAlignShift | 11.414 ns | 11.396 ns | 1.00 | - → - | 965 B → 995 B |
| TrimManualLoop | 5.513 ns | 5.623 ns | 1.02 | - → - | 138 B → 138 B |
| TrimVectorized | 2.725 ns | 2.868 ns | 1.05 | - → - | 1,387 B → 1,529 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.24 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| TryFormatThenFill     | .NET 10.0           | .NET 10.0 |  4.050 ns | 0.0293 ns | 0.0410 ns |  3.991 ns |  4.176 ns |  4.089 ns |  1.00 |    0.00 |   1,811 B |         - |          NA |
| ManualLsbThenReverse  | .NET 10.0           | .NET 10.0 | 10.195 ns | 0.0139 ns | 0.0200 ns | 10.156 ns | 10.231 ns | 10.221 ns |  2.52 |    0.03 |     750 B |         - |          NA |
| ManualRightAlignShift | .NET 10.0           | .NET 10.0 | 11.414 ns | 0.0348 ns | 0.0499 ns | 11.332 ns | 11.534 ns | 11.485 ns |  2.82 |    0.03 |     965 B |         - |          NA |
| TrimManualLoop        | .NET 10.0           | .NET 10.0 |  5.513 ns | 0.1495 ns | 0.2192 ns |  5.243 ns |  5.825 ns |  5.761 ns |  1.36 |    0.05 |     138 B |         - |          NA |
| TrimVectorized        | .NET 10.0           | .NET 10.0 |  2.725 ns | 0.0115 ns | 0.0164 ns |  2.696 ns |  2.749 ns |  2.744 ns |  0.67 |    0.01 |   1,387 B |         - |          NA |
|                       |                     |           |           |           |           |           |           |           |       |         |           |           |             |
| TryFormatThenFill     | MediumRun-.NET 11.0 | .NET 11.0 |  3.753 ns | 0.0225 ns | 0.0323 ns |  3.718 ns |  3.852 ns |  3.784 ns |  1.00 |    0.00 |   2,560 B |         - |          NA |
| ManualLsbThenReverse  | MediumRun-.NET 11.0 | .NET 11.0 | 10.263 ns | 0.0724 ns | 0.1062 ns | 10.174 ns | 10.613 ns | 10.424 ns |  2.73 |    0.04 |     759 B |         - |          NA |
| ManualRightAlignShift | MediumRun-.NET 11.0 | .NET 11.0 | 11.396 ns | 0.0190 ns | 0.0266 ns | 11.341 ns | 11.460 ns | 11.431 ns |  3.04 |    0.03 |     995 B |         - |          NA |
| TrimManualLoop        | MediumRun-.NET 11.0 | .NET 11.0 |  5.623 ns | 0.1512 ns | 0.2263 ns |  5.326 ns |  5.995 ns |  5.875 ns |  1.50 |    0.06 |     138 B |         - |          NA |
| TrimVectorized        | MediumRun-.NET 11.0 | .NET 11.0 |  2.868 ns | 0.0070 ns | 0.0102 ns |  2.851 ns |  2.891 ns |  2.880 ns |  0.76 |    0.01 |   1,529 B |         - |          NA |

## Earlier record

- Verdict: mixed (manual digit tricks rejected, vectorized trim adopted)
- TryFormat + Fill: 3.33 ns (fastest) - BCL digit formatting is already optimal on net10
- Manual LSB-write + Reverse: 10.7 ns (3.21x SLOWER) - rejected
- Manual right-align + shift: 12.2 ns (3.67x SLOWER) - rejected; the digit-count-avoidance claim is obsolete because TryFormat handles it internally
- Trim: IndexOfAnyExcept/LastIndexOfAnyExcept 3.80 ns vs manual loop 4.50 ns (0.85x, non-overlapping CIs) - adopted

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| TryFormatThenFill     |  3.333 ns | 0.0260 ns | 0.0365 ns |  3.330 ns |  3.269 ns |  3.403 ns |  3.375 ns |  1.00 |    0.02 |     852 B |         - |          NA |
| ManualLsbThenReverse  | 10.690 ns | 0.0398 ns | 0.0517 ns | 10.680 ns | 10.611 ns | 10.855 ns | 10.740 ns |  3.21 |    0.04 |     754 B |         - |          NA |
| ManualRightAlignShift | 12.246 ns | 0.2423 ns | 0.3317 ns | 12.125 ns | 12.012 ns | 13.572 ns | 12.549 ns |  3.67 |    0.11 |     979 B |         - |          NA |
| TrimManualLoop        |  4.495 ns | 0.0727 ns | 0.1043 ns |  4.479 ns |  4.356 ns |  4.793 ns |  4.620 ns |  1.35 |    0.03 |     138 B |         - |          NA |
| TrimVectorized        |  3.804 ns | 0.2214 ns | 0.3104 ns |  4.002 ns |  3.435 ns |  4.220 ns |  4.158 ns |  1.14 |    0.09 |   1,605 B |         - |          NA |
