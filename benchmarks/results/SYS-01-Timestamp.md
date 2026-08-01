# SYS-01: Low-cost timestamp acquisition

- Verdict: adopted
- Environment.TickCount64: 0.05x (1.13 ns, ~10-16 ms resolution, monotonic)
- Stopwatch.GetTimestamp: 0.77x (high resolution, monotonic)
- DateTime.UtcNow / DateTimeOffset.UtcNow: baseline (24.8 / 25.3 ns)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| DateTimeUtcNow         | 24.825 ns | 0.1711 ns | 0.2562 ns | 24.517 ns | 25.328 ns | 25.187 ns |  1.00 |    0.01 |     628 B |         - |          NA |
| DateTimeOffsetUtcNow   | 25.274 ns | 0.5076 ns | 0.6948 ns | 24.677 ns | 27.111 ns | 25.973 ns |  1.02 |    0.03 |     633 B |         - |          NA |
| EnvironmentTickCount64 |  1.127 ns | 0.0069 ns | 0.0103 ns |  1.111 ns |  1.146 ns |  1.140 ns |  0.05 |    0.00 |      63 B |         - |          NA |
| StopwatchGetTimestamp  | 19.115 ns | 0.2040 ns | 0.2860 ns | 18.748 ns | 19.705 ns | 19.471 ns |  0.77 |    0.01 |      77 B |         - |          NA |
