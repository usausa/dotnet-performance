# STK-05: Boxing vs cached boxes (-1/0/1 flag values)

- Verdict: conditional (allocation vs time tradeoff)
- Direct boxing: 5.98 ns + 24 B per op; cached-box switch: 7.66 ns / 0 B (1.29x SLOWER in time)
- On net10 a heap box is cheap (pointer-bump alloc); the cache wins only where GC pressure matters more than nanoseconds (long-running, high-rate paths)
- Escape analysis already stack-allocates non-escaping boxes (see pattern body: 0.004 ns) - cache only escaping, hot, known-value boxes

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| DirectBoxing | 3.677 ns | 0.0845 ns | 0.1156 ns | 3.281 ns | 3.911 ns | 3.808 ns |  1.00 |    0.04 | 0.0029 |     597 B | 0.0000 |      24 B |        1.00 |
| CachedBox    | 2.537 ns | 0.0182 ns | 0.0255 ns | 2.492 ns | 2.615 ns | 2.568 ns |  0.69 |    0.02 |      - |     677 B |      - |         - |        0.00 |
