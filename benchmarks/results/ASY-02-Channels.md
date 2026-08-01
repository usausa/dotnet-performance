# ASY-02: System.Threading.Channels

- Verdict: adopted
- ~45 ns/item unbounded (producer/consumer pump, 10,000 items)
- SingleReader/SingleWriter options: no measurable effect in this scenario (0.97x)
- Bounded(128): 2.0x - the price of backpressure

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                      | Mean     | Error     | StdDev    | Min      | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------------- |---------:|----------:|----------:|---------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| UnboundedDefault            | 44.82 ns |  2.072 ns |  3.101 ns | 41.26 ns |  50.83 ns |  49.96 ns |  1.00 |    0.09 |  25,092 B |       1 B |        1.00 |
| UnboundedSingleReaderWriter | 43.46 ns |  3.260 ns |  4.879 ns | 37.09 ns |  51.38 ns |  49.83 ns |  0.97 |    0.13 |  23,182 B |       1 B |        1.00 |
| Bounded                     | 89.71 ns | 10.657 ns | 15.284 ns | 71.20 ns | 117.68 ns | 109.56 ns |  2.01 |    0.36 |  23,550 B |         - |        0.00 |
