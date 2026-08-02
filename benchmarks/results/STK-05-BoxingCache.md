# STK-05: Boxing vs cached boxes (-1/0/1 flag values)

- Verdict: conditional (allocation vs time tradeoff)
- Direct boxing: 5.98 ns + 24 B per op; cached-box switch: 7.66 ns / 0 B (1.29x SLOWER in time)
- On net10 a heap box is cheap (pointer-bump alloc); the cache wins only where GC pressure matters more than nanoseconds (long-running, high-rate paths)
- Escape analysis already stack-allocates non-escaping boxes (see pattern body: 0.004 ns) - cache only escaping, hot, known-value boxes

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| DirectBoxing | 5.977 ns | 0.3405 ns | 0.4884 ns | 5.430 ns | 7.204 ns | 6.748 ns |  1.01 |    0.11 | 0.0014 |     597 B | 0.0000 |      24 B |        1.00 |
| CachedBox    | 7.659 ns | 0.2143 ns | 0.3074 ns | 7.136 ns | 8.227 ns | 7.993 ns |  1.29 |    0.11 |      - |     677 B |      - |         - |        0.00 |
