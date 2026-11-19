# TXT-01: Utf8DateTimeFormatter (lookup table)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the table formatter got faster on net11 (12.2 → 8.4 ns) and is 0.26x of ToString + encode, 0 B

### Utf8DateTimeFormatterBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ToStringEncode | 32.933 ns | 32.177 ns | 0.98 | 56 B → 56 B | 9,873 B → 10,106 B |
| TryFormatEncode | 27.703 ns | 28.765 ns | 1.04 | - → - | 9,177 B → 9,480 B |
| TableFormat | 12.162 ns | 8.361 ns | 0.69 | - → - | 1,344 B → 1,128 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.16 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method          | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| ToStringEncode  | .NET 10.0           | .NET 10.0 | 32.933 ns | 1.6038 ns | 2.1953 ns | 31.231 ns | 30.633 ns | 35.578 ns | 35.343 ns |  1.00 |    0.00 |   9,873 B | 0.0067 |      56 B |        1.00 |
| TryFormatEncode | .NET 10.0           | .NET 10.0 | 27.703 ns | 0.0913 ns | 0.1310 ns | 27.704 ns | 27.431 ns | 27.897 ns | 27.864 ns |  0.84 |    0.05 |   9,177 B |      - |         - |        0.00 |
| TableFormat     | .NET 10.0           | .NET 10.0 | 12.162 ns | 0.0312 ns | 0.0458 ns | 12.164 ns | 12.087 ns | 12.290 ns | 12.214 ns |  0.37 |    0.02 |   1,344 B |      - |         - |        0.00 |
|                 |                     |           |           |           |           |           |           |           |           |       |         |           |        |           |             |
| ToStringEncode  | MediumRun-.NET 11.0 | .NET 11.0 | 32.177 ns | 0.3837 ns | 0.5503 ns | 32.155 ns | 31.349 ns | 32.950 ns | 32.809 ns |  1.00 |    0.00 |  10,106 B | 0.0067 |      56 B |        1.00 |
| TryFormatEncode | MediumRun-.NET 11.0 | .NET 11.0 | 28.765 ns | 0.2637 ns | 0.3782 ns | 28.743 ns | 28.268 ns | 29.664 ns | 29.178 ns |  0.89 |    0.02 |   9,480 B |      - |         - |        0.00 |
| TableFormat     | MediumRun-.NET 11.0 | .NET 11.0 |  8.361 ns | 0.0343 ns | 0.0481 ns |  8.364 ns |  8.292 ns |  8.485 ns |  8.424 ns |  0.26 |    0.00 |   1,128 B |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted (implemented)
- 0.41x vs ToString + Encoding.GetBytes, 56 B -> 0 B
- Code size ~10 KB -> 0.9 KB
- DateTime.TryFormat + encode: 0.90x - a small time win over ToString, but the table is still ~2.2x faster than it

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| ToStringEncode  | 30.53 ns | 0.329 ns | 0.439 ns | 30.13 ns | 32.11 ns | 30.69 ns |  1.00 |    0.02 | 0.0067 |   9,716 B |      56 B |        1.00 |
| TryFormatEncode | 27.35 ns | 0.222 ns | 0.311 ns | 26.71 ns | 27.95 ns | 27.74 ns |  0.90 |    0.02 |      - |   9,149 B |         - |        0.00 |
| TableFormat     | 12.38 ns | 0.024 ns | 0.035 ns | 12.29 ns | 12.46 ns | 12.43 ns |  0.41 |    0.01 |      - |   1,348 B |         - |        0.00 |
