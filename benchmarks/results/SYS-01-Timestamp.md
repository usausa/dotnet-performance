# SYS-01: Low-cost timestamp acquisition

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - Environment.TickCount64 is still the cheapest timestamp (0.09x of DateTime.UtcNow on net11)
- **TickCount64 is 1.39x slower on net11** (1.33 → 1.85 ns, disjoint intervals, code 63 → 102 B); DateTime.UtcNow and Stopwatch are unchanged

### TimestampBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DateTimeUtcNow | 21.333 ns | 21.298 ns | 1.00 | - → - | 1,355 B → 2,081 B |
| DateTimeOffsetUtcNow | 21.538 ns | 21.504 ns | 1.00 | - → - | 1,368 B → 2,094 B |
| EnvironmentTickCount64 | 1.331 ns | 1.850 ns | 1.39 | - → - | 63 B → 102 B |
| StopwatchGetTimestamp | 16.025 ns | 16.019 ns | 1.00 | - → - | 77 B → 77 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.61 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                 | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|----------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| DateTimeUtcNow         | .NET 10.0           | .NET 10.0 | 21.333 ns | 0.0336 ns | 0.0482 ns | 21.284 ns | 21.507 ns | 21.391 ns |  1.00 |   1,355 B |         - |          NA |
| DateTimeOffsetUtcNow   | .NET 10.0           | .NET 10.0 | 21.538 ns | 0.0338 ns | 0.0506 ns | 21.476 ns | 21.671 ns | 21.601 ns |  1.01 |   1,368 B |         - |          NA |
| EnvironmentTickCount64 | .NET 10.0           | .NET 10.0 |  1.331 ns | 0.0424 ns | 0.0635 ns |  1.215 ns |  1.454 ns |  1.407 ns |  0.06 |      63 B |         - |          NA |
| StopwatchGetTimestamp  | .NET 10.0           | .NET 10.0 | 16.025 ns | 0.0132 ns | 0.0180 ns | 16.007 ns | 16.087 ns | 16.041 ns |  0.75 |      77 B |         - |          NA |
|                        |                     |           |           |           |           |           |           |           |       |           |           |             |
| DateTimeUtcNow         | MediumRun-.NET 11.0 | .NET 11.0 | 21.298 ns | 0.0153 ns | 0.0219 ns | 21.268 ns | 21.349 ns | 21.325 ns |  1.00 |   2,081 B |         - |          NA |
| DateTimeOffsetUtcNow   | MediumRun-.NET 11.0 | .NET 11.0 | 21.504 ns | 0.0219 ns | 0.0321 ns | 21.467 ns | 21.578 ns | 21.550 ns |  1.01 |   2,094 B |         - |          NA |
| EnvironmentTickCount64 | MediumRun-.NET 11.0 | .NET 11.0 |  1.850 ns | 0.0226 ns | 0.0331 ns |  1.796 ns |  1.921 ns |  1.889 ns |  0.09 |     102 B |         - |          NA |
| StopwatchGetTimestamp  | MediumRun-.NET 11.0 | .NET 11.0 | 16.019 ns | 0.0139 ns | 0.0204 ns | 15.995 ns | 16.061 ns | 16.052 ns |  0.75 |      77 B |         - |          NA |

## Earlier record

- Verdict: adopted
- Environment.TickCount64: 0.05x (1.13 ns, ~10-16 ms resolution, monotonic)
- Stopwatch.GetTimestamp: 0.77x (high resolution, monotonic)
- DateTime.UtcNow / DateTimeOffset.UtcNow: baseline (24.8 / 25.3 ns)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| DateTimeUtcNow         | 21.516 ns | 0.0183 ns | 0.0262 ns | 21.481 ns | 21.584 ns | 21.550 ns |  1.00 |     628 B |         - |          NA |
| DateTimeOffsetUtcNow   | 21.713 ns | 0.0306 ns | 0.0439 ns | 21.649 ns | 21.850 ns | 21.756 ns |  1.01 |     633 B |         - |          NA |
| EnvironmentTickCount64 |  1.079 ns | 0.0068 ns | 0.0089 ns |  1.067 ns |  1.109 ns |  1.087 ns |  0.05 |      63 B |         - |          NA |
| StopwatchGetTimestamp  | 16.035 ns | 0.0166 ns | 0.0244 ns | 16.006 ns | 16.090 ns | 16.075 ns |  0.75 |      77 B |         - |          NA |
