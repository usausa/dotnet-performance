# ASY-03: System.IO.Pipelines

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
