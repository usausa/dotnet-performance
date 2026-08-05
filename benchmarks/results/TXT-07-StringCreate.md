# TXT-07: string.Create vs interpolation vs builders

- Verdict: adopted
- string.Create: 0.57x vs interpolation, allocation = result string only (80 B)
- ValueStringBuilder: 0.81x, same 80 B; StringBuilder(capacity): 1.03x with 3.5x allocation (280 B)
- Use string.Create when total length is computable up front; ValueStringBuilder otherwise

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean      | Error     | StdDev    | Min       | Max      | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------------ |----------:|----------:|----------:|----------:|---------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| Interpolation           | 16.981 ns | 0.1398 ns | 0.1914 ns | 16.536 ns | 17.22 ns | 17.199 ns |  1.00 |    0.02 | 0.0095 |   4,138 B |      80 B |        1.00 |
| Concat                  | 19.603 ns | 0.1506 ns | 0.2111 ns | 19.162 ns | 19.97 ns | 19.893 ns |  1.15 |    0.02 | 0.0210 |   1,823 B |     176 B |        2.20 |
| StringBuilderCapacity   | 15.601 ns | 0.2071 ns | 0.2765 ns | 15.344 ns | 16.56 ns | 15.839 ns |  0.92 |    0.02 | 0.0335 |   2,327 B |     280 B |        3.50 |
| ValueStringBuilderBuild | 12.496 ns | 0.1449 ns | 0.1984 ns | 12.179 ns | 12.90 ns | 12.803 ns |  0.74 |    0.01 | 0.0096 |   1,810 B |      80 B |        1.00 |
| StringCreate            |  9.394 ns | 0.1525 ns | 0.2088 ns |  9.028 ns | 10.02 ns |  9.629 ns |  0.55 |    0.01 | 0.0096 |   2,359 B |      80 B |        1.00 |
