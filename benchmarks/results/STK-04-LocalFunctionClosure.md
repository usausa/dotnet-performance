# STK-04: Local function closure avoidance (delegate conversion)

- Verdict: conditional (allocation claim holds; time depends on delegate shape)
- Capturing local function: 11.45 ns + 88 B per call; static local function + state arg: 15.13 ns / 0 B (1.33x time)
- For delegate-passing hot paths prefer the static LAMBDA + TState form (DSP-04: 4.66 ns / 0 B); static local functions shine for direct calls and iterator/validation splitting, not as cached delegates

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| CapturingLocalFunction | 11.45 ns | 0.644 ns | 0.923 ns | 10.14 ns | 12.88 ns | 12.46 ns |  1.01 |    0.11 |     300 B | 0.0052 |      88 B |        1.00 |
| StaticLocalFunction    | 15.13 ns | 0.302 ns | 0.452 ns | 14.64 ns | 16.29 ns | 15.77 ns |  1.33 |    0.11 |     257 B |      - |         - |        0.00 |
