# SYS-01: Low-cost timestamp acquisition

- Verdict: adopted
- Environment.TickCount64: 0.05x (1.13 ns, ~10-16 ms resolution, monotonic)
- Stopwatch.GetTimestamp: 0.77x (high resolution, monotonic)
- DateTime.UtcNow / DateTimeOffset.UtcNow: baseline (24.8 / 25.3 ns)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| DateTimeUtcNow         | 21.516 ns | 0.0183 ns | 0.0262 ns | 21.481 ns | 21.584 ns | 21.550 ns |  1.00 |     628 B |         - |          NA |
| DateTimeOffsetUtcNow   | 21.713 ns | 0.0306 ns | 0.0439 ns | 21.649 ns | 21.850 ns | 21.756 ns |  1.01 |     633 B |         - |          NA |
| EnvironmentTickCount64 |  1.079 ns | 0.0068 ns | 0.0089 ns |  1.067 ns |  1.109 ns |  1.087 ns |  0.05 |      63 B |         - |          NA |
| StopwatchGetTimestamp  | 16.035 ns | 0.0166 ns | 0.0244 ns | 16.006 ns | 16.090 ns | 16.075 ns |  0.75 |      77 B |         - |          NA |
