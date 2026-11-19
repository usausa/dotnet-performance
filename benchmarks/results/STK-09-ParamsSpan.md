# STK-09: params ReadOnlySpan<T> vs params T[]

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - params span is 0.31x of params array on both generations, 48 B → 0 B

### ParamsSpanBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ParamsArray | 5.958 ns | 5.927 ns | 0.99 | 48 B → 48 B | 117 B → 117 B |
| ParamsSpan | 1.854 ns | 1.844 ns | 0.99 | - → - | 115 B → 115 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.74 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method      | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------ |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|-------:|----------:|----------:|------------:|
| ParamsArray | .NET 10.0           | .NET 10.0 | 5.958 ns | 0.0447 ns | 0.0640 ns | 5.841 ns | 6.057 ns | 6.035 ns |  1.00 | 0.0057 |     117 B |      48 B |        1.00 |
| ParamsSpan  | .NET 10.0           | .NET 10.0 | 1.854 ns | 0.0111 ns | 0.0163 ns | 1.834 ns | 1.900 ns | 1.881 ns |  0.31 |      - |     115 B |         - |        0.00 |
|             |                     |           |          |           |           |          |          |          |       |        |           |           |             |
| ParamsArray | MediumRun-.NET 11.0 | .NET 11.0 | 5.927 ns | 0.0365 ns | 0.0523 ns | 5.807 ns | 6.000 ns | 5.980 ns |  1.00 | 0.0057 |     117 B |      48 B |        1.00 |
| ParamsSpan  | MediumRun-.NET 11.0 | .NET 11.0 | 1.844 ns | 0.0045 ns | 0.0063 ns | 1.834 ns | 1.860 ns | 1.852 ns |  0.31 |      - |     115 B |         - |        0.00 |

## Earlier record

- Verdict: adopted
- 0.25x with 3 args (4.46 -> 1.10 ns), allocation 48 B -> 0 B; same call-site syntax

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method      | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-------:|----------:|----------:|------------:|
| ParamsArray | 4.464 ns | 0.0321 ns | 0.0461 ns | 4.351 ns | 4.529 ns | 4.513 ns |  1.00 | 0.0057 |     115 B |      48 B |        1.00 |
| ParamsSpan  | 1.100 ns | 0.0297 ns | 0.0425 ns | 1.045 ns | 1.195 ns | 1.163 ns |  0.25 |      - |     115 B |         - |        0.00 |
