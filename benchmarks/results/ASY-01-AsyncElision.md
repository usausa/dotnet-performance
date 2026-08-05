# ASY-01: Async state machine elision

- Verdict: adopted
- Task direct return 0.16x, 73 B -> ~0 B (async wrapper re-wraps even a cached completed Task)
- ValueTask direct return 0.15x vs await wrapper 0.58x (both 0 B)
- Apply only to pure forwards (single await, no try/using/lock across it)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| TaskAwaitForward       | 6.3668 ns | 0.1941 ns | 0.2721 ns | 6.1188 ns | 7.2739 ns | 6.5284 ns |  1.00 |    0.06 | 0.0087 |   2,041 B |      73 B |        1.00 |
| TaskDirectForward      | 0.9974 ns | 0.0322 ns | 0.0462 ns | 0.9715 ns | 1.1464 ns | 1.0639 ns |  0.16 |    0.01 | 0.0001 |   1,248 B |       1 B |        0.01 |
| ValueTaskAwaitForward  | 4.2302 ns | 0.0575 ns | 0.0825 ns | 4.1295 ns | 4.3567 ns | 4.3309 ns |  0.67 |    0.03 | 0.0001 |   3,761 B |       1 B |        0.01 |
| ValueTaskDirectForward | 0.8312 ns | 0.0119 ns | 0.0166 ns | 0.7965 ns | 0.8764 ns | 0.8514 ns |  0.13 |    0.01 | 0.0001 |   2,228 B |       1 B |        0.01 |
