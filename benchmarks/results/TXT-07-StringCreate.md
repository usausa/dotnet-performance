# TXT-07: string.Create vs interpolation vs builders

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - string.Create is 0.53x of interpolation on net11 (0.63x on net10), 80 B
- Interpolation itself is 1.15x slower on net11 (15.8 → 18.2 ns, code 4.2 → 8.3 KB)

### StringBuildBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| Interpolation | 15.804 ns | 18.227 ns | 1.15 | 80 B → 80 B | 4,235 B → 8,296 B |
| Concat | 19.332 ns | 18.549 ns | 0.96 | 176 B → 176 B | 2,970 B → 3,410 B |
| StringBuilderCapacity | 16.875 ns | 16.981 ns | 1.01 | 280 B → 280 B | 2,934 B → 2,981 B |
| ValueStringBuilderBuild | 13.311 ns | 13.830 ns | 1.04 | 80 B → 80 B | 2,788 B → 3,469 B |
| StringCreate | 9.987 ns | 9.587 ns | 0.96 | 80 B → 80 B | 3,330 B → 4,092 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.32 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------------ |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| Interpolation           | .NET 10.0           | .NET 10.0 | 15.804 ns | 0.1346 ns | 0.1973 ns | 15.749 ns | 15.530 ns | 16.238 ns | 16.071 ns |  1.00 |    0.00 |   4,235 B | 0.0095 |      80 B |        1.00 |
| Concat                  | .NET 10.0           | .NET 10.0 | 19.332 ns | 0.1056 ns | 0.1515 ns | 19.294 ns | 19.088 ns | 19.672 ns | 19.549 ns |  1.22 |    0.02 |   2,970 B | 0.0210 |     176 B |        2.20 |
| StringBuilderCapacity   | .NET 10.0           | .NET 10.0 | 16.875 ns | 0.1043 ns | 0.1495 ns | 16.902 ns | 16.583 ns | 17.147 ns | 17.026 ns |  1.07 |    0.02 |   2,934 B | 0.0335 |     280 B |        3.50 |
| ValueStringBuilderBuild | .NET 10.0           | .NET 10.0 | 13.311 ns | 0.0322 ns | 0.0472 ns | 13.310 ns | 13.236 ns | 13.410 ns | 13.376 ns |  0.84 |    0.01 |   2,788 B | 0.0096 |      80 B |        1.00 |
| StringCreate            | .NET 10.0           | .NET 10.0 |  9.987 ns | 0.0346 ns | 0.0507 ns |  9.977 ns |  9.896 ns | 10.099 ns | 10.048 ns |  0.63 |    0.01 |   3,330 B | 0.0096 |      80 B |        1.00 |
|                         |                     |           |           |           |           |           |           |           |           |       |         |           |        |           |             |
| Interpolation           | MediumRun-.NET 11.0 | .NET 11.0 | 18.227 ns | 0.4456 ns | 0.6532 ns | 17.741 ns | 17.495 ns | 19.076 ns | 19.028 ns |  1.00 |    0.00 |   8,296 B | 0.0095 |      80 B |        1.00 |
| Concat                  | MediumRun-.NET 11.0 | .NET 11.0 | 18.549 ns | 0.0795 ns | 0.1190 ns | 18.516 ns | 18.362 ns | 18.796 ns | 18.696 ns |  1.02 |    0.04 |   3,410 B | 0.0210 |     176 B |        2.20 |
| StringBuilderCapacity   | MediumRun-.NET 11.0 | .NET 11.0 | 16.981 ns | 0.1386 ns | 0.2032 ns | 16.933 ns | 16.777 ns | 17.617 ns | 17.190 ns |  0.93 |    0.03 |   2,981 B | 0.0335 |     280 B |        3.50 |
| ValueStringBuilderBuild | MediumRun-.NET 11.0 | .NET 11.0 | 13.830 ns | 0.0429 ns | 0.0643 ns | 13.831 ns | 13.711 ns | 13.975 ns | 13.900 ns |  0.76 |    0.03 |   3,469 B | 0.0096 |      80 B |        1.00 |
| StringCreate            | MediumRun-.NET 11.0 | .NET 11.0 |  9.587 ns | 0.0373 ns | 0.0535 ns |  9.592 ns |  9.481 ns |  9.690 ns |  9.651 ns |  0.53 |    0.02 |   4,092 B | 0.0096 |      80 B |        1.00 |

## Earlier record

- Verdict: adopted
- string.Create: 0.55x vs interpolation, allocation = result string only (80 B)
- ValueStringBuilder: 0.74x, same 80 B; StringBuilder(capacity): 0.92x with 3.5x allocation (280 B)
- Use string.Create when total length is computable up front; ValueStringBuilder otherwise

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean      | Error     | StdDev    | Min       | Max      | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|----------:|---------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| Interpolation           | 16.981 ns | 0.1398 ns | 0.1914 ns | 16.536 ns | 17.22 ns | 17.199 ns |  1.00 |    0.02 | 0.0095 |   4,138 B |      80 B |        1.00 |
| Concat                  | 19.603 ns | 0.1506 ns | 0.2111 ns | 19.162 ns | 19.97 ns | 19.893 ns |  1.15 |    0.02 | 0.0210 |   1,823 B |     176 B |        2.20 |
| StringBuilderCapacity   | 15.601 ns | 0.2071 ns | 0.2765 ns | 15.344 ns | 16.56 ns | 15.839 ns |  0.92 |    0.02 | 0.0335 |   2,327 B |     280 B |        3.50 |
| ValueStringBuilderBuild | 12.496 ns | 0.1449 ns | 0.1984 ns | 12.179 ns | 12.90 ns | 12.803 ns |  0.74 |    0.01 | 0.0096 |   1,810 B |      80 B |        1.00 |
| StringCreate            |  9.394 ns | 0.1525 ns | 0.2088 ns |  9.028 ns | 10.02 ns |  9.629 ns |  0.55 |    0.01 | 0.0096 |   2,359 B |      80 B |        1.00 |
