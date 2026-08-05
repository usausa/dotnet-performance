# DSP-04: Static lambda + TState vs capturing lambda

- Verdict: adopted
- Capturing a per-iteration local: 7.09 ns + 88 B per call (closure + delegate)
- static lambda + TState: 2.96 ns / 0 B (0.42x) - the compiler caches the delegate

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| CaptureLocal    | 7.089 ns | 0.1204 ns | 0.1648 ns | 6.650 ns | 7.533 ns | 7.245 ns |  1.00 |    0.03 | 0.0105 |     300 B |      88 B |        1.00 |
| StaticWithState | 2.957 ns | 0.0512 ns | 0.0734 ns | 2.852 ns | 3.127 ns | 3.082 ns |  0.42 |    0.01 |      - |     329 B |         - |        0.00 |
