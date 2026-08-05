# CON-01: Interlocked one-shot guard (dispose / init)

- Verdict: adopted
- Thread-safe exactly-once guards: Interlocked 1.81-1.96 ns vs lock (System.Threading.Lock) 4.73 ns (2.4-2.6x), code 33-56 B vs 2,612 B
- Plain bool 0.40 ns is fastest but not thread-safe - sufficient for single-threaded types

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| PlainBool           | 0.1770 ns | 0.0016 ns | 0.0024 ns | 0.1693 ns | 0.1813 ns | 0.1796 ns |  1.00 |    0.02 |      26 B |         - |          NA |
| VolatileBool        | 0.1804 ns | 0.0034 ns | 0.0050 ns | 0.1757 ns | 0.1964 ns | 0.1860 ns |  1.02 |    0.03 |      26 B |         - |          NA |
| LockGuard           | 8.8513 ns | 0.0359 ns | 0.0492 ns | 8.8167 ns | 9.0569 ns | 8.8808 ns | 50.02 |    0.73 |   2,612 B |         - |          NA |
| InterlockedCas      | 3.9773 ns | 0.0031 ns | 0.0046 ns | 3.9705 ns | 3.9875 ns | 3.9828 ns | 22.48 |    0.31 |      56 B |         - |          NA |
| InterlockedExchange | 3.9035 ns | 0.0042 ns | 0.0061 ns | 3.8929 ns | 3.9152 ns | 3.9126 ns | 22.06 |    0.30 |      33 B |         - |          NA |
