# ASY-03: System.IO.Pipelines

- Verdict: adopted (conditional)
- 1.63x vs MemoryStream for small same-thread transfer (4 KB x 16)
- Allocation 128.2 KB -> 1.6 KB (1/80, pooled segments)
- Caution: sequential write-then-read deadlocks at PauseWriterThreshold (default 64 KB); reader must run concurrently

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Min      | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|---------:|----------:|----------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| MemoryStreamPump |  6.752 μs | 0.5604 μs | 0.8387 μs | 5.732 μs |  8.912 μs |  7.895 μs |  1.01 |    0.17 |   2,733 B | 7.8125 | 1.9455 |  128.2 KB |        1.00 |
| PipePump         | 10.857 μs | 0.8543 μs | 1.2252 μs | 9.344 μs | 14.407 μs | 12.454 μs |  1.63 |    0.26 |  25,837 B | 0.0916 |      - |    1.6 KB |        0.01 |
