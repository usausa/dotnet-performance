# TXT-07: string.Create vs interpolation vs builders

- Verdict: adopted
- string.Create: 0.57x vs interpolation, allocation = result string only (80 B)
- ValueStringBuilder: 0.81x, same 80 B; StringBuilder(capacity): 1.03x with 3.5x allocation (280 B)
- Use string.Create when total length is computable up front; ValueStringBuilder otherwise

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| Interpolation           | 26.82 ns | 0.347 ns | 0.497 ns | 25.80 ns | 27.76 ns | 27.54 ns |  1.00 |    0.03 |   4,154 B | 0.0048 |      80 B |        1.00 |
| Concat                  | 29.86 ns | 0.256 ns | 0.368 ns | 28.83 ns | 30.48 ns | 30.20 ns |  1.11 |    0.02 |   1,835 B | 0.0105 |     176 B |        2.20 |
| StringBuilderCapacity   | 27.66 ns | 0.387 ns | 0.555 ns | 26.28 ns | 28.72 ns | 28.35 ns |  1.03 |    0.03 |   2,339 B | 0.0167 |     280 B |        3.50 |
| ValueStringBuilderBuild | 21.66 ns | 0.225 ns | 0.336 ns | 21.04 ns | 22.36 ns | 22.02 ns |  0.81 |    0.02 |   1,835 B | 0.0048 |      80 B |        1.00 |
| StringCreate            | 15.30 ns | 0.448 ns | 0.670 ns | 14.45 ns | 16.59 ns | 16.32 ns |  0.57 |    0.03 |   2,374 B | 0.0048 |      80 B |        1.00 |
