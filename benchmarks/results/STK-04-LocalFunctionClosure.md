# STK-04: Local function closure avoidance (delegate conversion)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - conditional. The capturing local function allocates 80 B on net11 (88 B on net10, the smaller delegate); the static form stays 0 B at 2.18x of the time

### LocalFunctionClosureBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| CapturingLocalFunction | 6.867 ns | 6.917 ns | 1.01 | 88 B → 80 B | 300 B → 300 B |
| StaticLocalFunction | 15.193 ns | 15.104 ns | 0.99 | - → - | 257 B → 257 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.56 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                 | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| CapturingLocalFunction | .NET 10.0           | .NET 10.0 |  6.867 ns | 0.0690 ns | 0.1012 ns |  6.706 ns |  7.021 ns |  7.006 ns |  1.00 |    0.00 | 0.0105 |     300 B |      88 B |        1.00 |
| StaticLocalFunction    | .NET 10.0           | .NET 10.0 | 15.193 ns | 0.2319 ns | 0.3400 ns | 14.584 ns | 16.102 ns | 15.610 ns |  2.21 |    0.06 |      - |     257 B |         - |        0.00 |
|                        |                     |           |           |           |           |           |           |           |       |         |        |           |           |             |
| CapturingLocalFunction | MediumRun-.NET 11.0 | .NET 11.0 |  6.917 ns | 0.0833 ns | 0.1221 ns |  6.704 ns |  7.159 ns |  7.044 ns |  1.00 |    0.00 | 0.0096 |     300 B |      80 B |        1.00 |
| StaticLocalFunction    | MediumRun-.NET 11.0 | .NET 11.0 | 15.104 ns | 0.2596 ns | 0.3885 ns | 14.472 ns | 15.839 ns | 15.585 ns |  2.18 |    0.07 |      - |     257 B |         - |        0.00 |

## Earlier record

- Verdict: conditional (allocation claim holds; time depends on delegate shape)
- Capturing local function: 7.00 ns + 88 B per call; static local function + state arg: 15.26 ns / 0 B (2.18x time)
- For delegate-passing hot paths prefer the static LAMBDA + TState form (DSP-04, which caches the delegate); static local functions shine for direct calls (inlined) and iterator/validation splitting, not as cached delegates

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| CapturingLocalFunction |  7.003 ns | 0.1019 ns | 0.1361 ns |  6.798 ns |  7.243 ns |  7.156 ns |  1.00 |    0.03 |     300 B | 0.0105 |      88 B |        1.00 |
| StaticLocalFunction    | 15.260 ns | 0.2905 ns | 0.4259 ns | 14.493 ns | 16.214 ns | 15.907 ns |  2.18 |    0.07 |     257 B |      - |         - |        0.00 |
