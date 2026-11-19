# ASY-03: System.IO.Pipelines

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted (conditional)
- The pipe pump is 1.91x of MemoryStream on net10 and 2.12x on net11 (the pipe itself is 1.12x slower on net11)
- Allocation unchanged: 128.2 KB against 1.8-1.9 KB

### PipelinesBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| MemoryStreamPump | 3.268 μs | 3.309 μs | 1.01 | 128.2 KB → 128.2 KB | 4,157 B → 4,860 B |
| PipePump | 6.251 μs | 7.019 μs | 1.12 | 1.9 KB → 1.83 KB | 34,242 B → 31,272 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.52 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|--------:|-------:|----------:|------------:|
| MemoryStreamPump | .NET 10.0           | .NET 10.0 | 3.268 μs | 0.0157 μs | 0.0230 μs | 3.222 μs | 3.312 μs | 3.292 μs |  1.00 |    0.00 |   4,157 B | 15.6250 | 3.9024 |  128.2 KB |        1.00 |
| PipePump         | .NET 10.0           | .NET 10.0 | 6.251 μs | 0.0376 μs | 0.0552 μs | 6.139 μs | 6.356 μs | 6.310 μs |  1.91 |    0.02 |  34,242 B |  0.2289 |      - |    1.9 KB |        0.01 |
|                  |                     |           |          |           |           |          |          |          |       |         |           |         |        |           |             |
| MemoryStreamPump | MediumRun-.NET 11.0 | .NET 11.0 | 3.309 μs | 0.0175 μs | 0.0262 μs | 3.257 μs | 3.349 μs | 3.342 μs |  1.00 |    0.00 |   4,860 B | 15.6250 | 3.9024 |  128.2 KB |        1.00 |
| PipePump         | MediumRun-.NET 11.0 | .NET 11.0 | 7.019 μs | 0.1349 μs | 0.1977 μs | 6.590 μs | 7.463 μs | 7.166 μs |  2.12 |    0.06 |  31,272 B |  0.2136 |      - |   1.83 KB |        0.01 |

## Earlier record

- Verdict: adopted (conditional)
- 2.2x vs MemoryStream for small same-thread transfer (4 KB x 16) - the synchronization machinery is the cost
- Allocation 128.2 KB -> 1.8 KB (1/70, pooled segments)
- Caution: sequential write-then-read deadlocks at PauseWriterThreshold (default 64 KB); reader must run concurrently

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Min      | Max       | P90      | Ratio | RatioSD | Code Size | Gen0    | Gen1   | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|---------:|----------:|---------:|------:|--------:|----------:|--------:|-------:|----------:|------------:|
| MemoryStreamPump | 3.233 μs | 0.0658 μs | 0.0900 μs | 3.026 μs |  3.351 μs | 3.332 μs |  1.00 |    0.04 |   2,717 B | 15.6250 | 3.9024 |  128.2 KB |        1.00 |
| PipePump         | 7.161 μs | 0.9357 μs | 1.3117 μs | 6.251 μs | 12.048 μs | 8.558 μs |  2.22 |    0.40 |  25,850 B |  0.2136 |      - |   1.83 KB |        0.01 |
