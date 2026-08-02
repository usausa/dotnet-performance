# CON-01: Interlocked one-shot guard (dispose / init)

- Verdict: adopted
- Thread-safe exactly-once guards: Interlocked 1.81-1.96 ns vs lock (System.Threading.Lock) 4.73 ns (2.4-2.6x), code 33-56 B vs 2,612 B
- Plain bool 0.40 ns is fastest but not thread-safe - sufficient for single-threaded types

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| PlainBool           | 0.3964 ns | 0.0306 ns | 0.0439 ns | 0.3266 ns | 0.5160 ns | 0.4391 ns |  1.01 |    0.16 |      26 B |         - |          NA |
| VolatileBool        | 0.3021 ns | 0.0149 ns | 0.0214 ns | 0.2818 ns | 0.3572 ns | 0.3366 ns |  0.77 |    0.10 |      26 B |         - |          NA |
| LockGuard           | 4.7345 ns | 0.0645 ns | 0.0925 ns | 4.5251 ns | 4.9706 ns | 4.8552 ns | 12.09 |    1.33 |   2,612 B |         - |          NA |
| InterlockedCas      | 1.9588 ns | 0.0305 ns | 0.0456 ns | 1.9061 ns | 2.0700 ns | 2.0274 ns |  5.00 |    0.56 |      56 B |         - |          NA |
| InterlockedExchange | 1.8054 ns | 0.0689 ns | 0.0989 ns | 1.6892 ns | 1.9652 ns | 1.9283 ns |  4.61 |    0.56 |      33 B |         - |          NA |
