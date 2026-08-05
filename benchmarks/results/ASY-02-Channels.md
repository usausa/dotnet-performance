# ASY-02: System.Threading.Channels

- Verdict: adopted
- ~45 ns/item unbounded (producer/consumer pump, 10,000 items)
- SingleReader/SingleWriter options: no measurable effect in this scenario (0.97x)
- Bounded(128): 2.0x - the price of backpressure

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                      | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| UnboundedDefault            | 38.78 ns | 0.271 ns | 0.389 ns | 38.18 ns | 39.66 ns | 39.22 ns |  1.00 |    0.01 | 0.0003 |  26,238 B |       3 B |        1.00 |
| UnboundedSingleReaderWriter | 34.70 ns | 0.369 ns | 0.517 ns | 33.98 ns | 35.81 ns | 35.47 ns |  0.89 |    0.02 | 0.0002 |  21,237 B |       2 B |        0.67 |
| Bounded                     | 63.02 ns | 0.619 ns | 0.868 ns | 61.17 ns | 64.74 ns | 64.28 ns |  1.63 |    0.03 |      - |  24,335 B |         - |        0.00 |
