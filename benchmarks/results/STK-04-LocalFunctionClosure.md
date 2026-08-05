# STK-04: Local function closure avoidance (delegate conversion)

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
