# TXT-05: Utf8.TryWrite

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - Utf8 TryWrite is 0.51x of interpolation + GetBytes on net11 (0.53x on net10), 104 B → 0 B

### Utf8WriteBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| StringInterpolationEncode | 27.90 ns | 27.89 ns | 1.00 | 104 B → 104 B | 8,630 B → 9,938 B |
| CharTryWriteEncode | 16.49 ns | 16.47 ns | 1.00 | - → - | 5,846 B → 6,766 B |
| Utf8TryWrite | 14.68 ns | 14.10 ns | 0.96 | - → - | 6,666 B → 6,009 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.1 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                    | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|-------:|----------:|----------:|------------:|
| StringInterpolationEncode | .NET 10.0           | .NET 10.0 | 27.90 ns | 0.186 ns | 0.272 ns | 27.42 ns | 28.27 ns | 28.21 ns |  1.00 | 0.0124 |   8,630 B |     104 B |        1.00 |
| CharTryWriteEncode        | .NET 10.0           | .NET 10.0 | 16.49 ns | 0.062 ns | 0.091 ns | 16.34 ns | 16.65 ns | 16.62 ns |  0.59 |      - |   5,846 B |         - |        0.00 |
| Utf8TryWrite              | .NET 10.0           | .NET 10.0 | 14.68 ns | 0.034 ns | 0.048 ns | 14.59 ns | 14.76 ns | 14.75 ns |  0.53 |      - |   6,666 B |         - |        0.00 |
|                           |                     |           |          |          |          |          |          |          |       |        |           |           |             |
| StringInterpolationEncode | MediumRun-.NET 11.0 | .NET 11.0 | 27.89 ns | 0.062 ns | 0.089 ns | 27.73 ns | 28.04 ns | 28.00 ns |  1.00 | 0.0124 |   9,938 B |     104 B |        1.00 |
| CharTryWriteEncode        | MediumRun-.NET 11.0 | .NET 11.0 | 16.47 ns | 0.103 ns | 0.147 ns | 16.26 ns | 16.77 ns | 16.68 ns |  0.59 |      - |   6,766 B |         - |        0.00 |
| Utf8TryWrite              | MediumRun-.NET 11.0 | .NET 11.0 | 14.10 ns | 0.088 ns | 0.123 ns | 13.91 ns | 14.33 ns | 14.24 ns |  0.51 |      - |   6,009 B |         - |        0.00 |

## Earlier record

- Verdict: adopted
- 0.45x vs string interpolation + Encoding.UTF8.GetBytes, 104 B -> 0 B
- Faster than char-based TryWrite + encode (0.52x)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| StringInterpolationEncode | 31.69 ns | 3.433 ns | 5.032 ns | 27.22 ns | 26.66 ns | 37.12 ns | 36.88 ns |  1.02 |    0.23 | 0.0124 |   7,252 B |     104 B |        1.00 |
| CharTryWriteEncode        | 16.23 ns | 0.963 ns | 1.442 ns | 15.57 ns | 15.43 ns | 21.07 ns | 18.31 ns |  0.52 |    0.09 |      - |   4,701 B |         - |        0.00 |
| Utf8TryWrite              | 13.82 ns | 0.055 ns | 0.081 ns | 13.83 ns | 13.68 ns | 14.00 ns | 13.94 ns |  0.45 |    0.07 |      - |   4,787 B |         - |        0.00 |
