# TXT-02: ValueStringBuilder

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - ValueStringBuilder is 0.28x of a default StringBuilder on net11, 760 → 216 B
- Its Tier1 code grew from 1.5 KB to 4.7 KB on net11 without a time cost

### ValueStringBuilderBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| StringBuilderDefault | 53.04 ns | 52.75 ns | 0.99 | 760 B → 760 B | 1,570 B → 1,516 B |
| StringBuilderCapacity | 22.96 ns | 23.88 ns | 1.04 | 544 B → 544 B | 1,574 B → 1,520 B |
| InterpolatedHandler | 15.08 ns | 15.21 ns | 1.01 | 216 B → 216 B | 1,304 B → 1,489 B |
| ValueStringBuilder | 15.50 ns | 14.77 ns | 0.95 | 216 B → 216 B | 1,513 B → 4,708 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.55 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|----------:|-------:|-------:|----------:|------------:|
| StringBuilderDefault  | .NET 10.0           | .NET 10.0 | 53.04 ns | 1.131 ns | 1.586 ns | 53.65 ns | 50.88 ns | 55.39 ns | 54.95 ns |  1.00 |   1,570 B | 0.0908 | 0.0002 |     760 B |        1.00 |
| StringBuilderCapacity | .NET 10.0           | .NET 10.0 | 22.96 ns | 0.160 ns | 0.230 ns | 22.98 ns | 22.53 ns | 23.43 ns | 23.24 ns |  0.43 |   1,574 B | 0.0650 | 0.0001 |     544 B |        0.72 |
| InterpolatedHandler   | .NET 10.0           | .NET 10.0 | 15.08 ns | 0.132 ns | 0.190 ns | 15.12 ns | 14.79 ns | 15.47 ns | 15.30 ns |  0.28 |   1,304 B | 0.0258 |      - |     216 B |        0.28 |
| ValueStringBuilder    | .NET 10.0           | .NET 10.0 | 15.50 ns | 0.192 ns | 0.281 ns | 15.42 ns | 15.09 ns | 15.94 ns | 15.83 ns |  0.29 |   1,513 B | 0.0258 |      - |     216 B |        0.28 |
|                       |                     |           |          |          |          |          |          |          |          |       |           |        |        |           |             |
| StringBuilderDefault  | MediumRun-.NET 11.0 | .NET 11.0 | 52.75 ns | 0.315 ns | 0.462 ns | 52.75 ns | 51.65 ns | 53.83 ns | 53.17 ns |  1.00 |   1,516 B | 0.0908 | 0.0002 |     760 B |        1.00 |
| StringBuilderCapacity | MediumRun-.NET 11.0 | .NET 11.0 | 23.88 ns | 0.116 ns | 0.174 ns | 23.85 ns | 23.62 ns | 24.19 ns | 24.14 ns |  0.45 |   1,520 B | 0.0650 | 0.0001 |     544 B |        0.72 |
| InterpolatedHandler   | MediumRun-.NET 11.0 | .NET 11.0 | 15.21 ns | 0.441 ns | 0.646 ns | 15.54 ns | 14.41 ns | 16.02 ns | 15.98 ns |  0.29 |   1,489 B | 0.0258 |      - |     216 B |        0.28 |
| ValueStringBuilder    | MediumRun-.NET 11.0 | .NET 11.0 | 14.77 ns | 0.108 ns | 0.151 ns | 14.78 ns | 14.51 ns | 15.03 ns | 14.95 ns |  0.28 |   4,708 B | 0.0258 |      - |     216 B |        0.28 |

## Earlier record

- Verdict: adopted (implemented)
- 0.31-0.33x vs StringBuilder (no capacity), 760 B -> 216 B (result string only)
- On par with stackalloc-backed DefaultInterpolatedStringHandler
- Capacity-specified StringBuilder alone gives 0.43-0.47x

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| StringBuilderDefault  | 50.28 ns | 1.421 ns | 1.993 ns | 48.33 ns | 58.30 ns | 52.02 ns |  1.00 |    0.05 | 0.0908 |   1,565 B | 0.0002 |     760 B |        1.00 |
| StringBuilderCapacity | 22.60 ns | 0.369 ns | 0.552 ns | 21.76 ns | 23.46 ns | 23.18 ns |  0.45 |    0.02 | 0.0650 |   1,577 B | 0.0001 |     544 B |        0.72 |
| InterpolatedHandler   | 14.88 ns | 0.770 ns | 1.105 ns | 13.61 ns | 19.00 ns | 15.78 ns |  0.30 |    0.02 | 0.0258 |   1,297 B |      - |     216 B |        0.28 |
| ValueStringBuilder    | 14.98 ns | 0.173 ns | 0.243 ns | 14.38 ns | 15.52 ns | 15.32 ns |  0.30 |    0.01 | 0.0258 |   1,496 B |      - |     216 B |        0.28 |
