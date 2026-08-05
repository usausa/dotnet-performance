# TXT-05: Utf8.TryWrite

- Verdict: adopted
- 0.54x vs string interpolation + Encoding.UTF8.GetBytes, 104 B -> 0 B
- Faster than char-based TryWrite + encode (0.60x)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| StringInterpolationEncode | 31.69 ns | 3.433 ns | 5.032 ns | 27.22 ns | 26.66 ns | 37.12 ns | 36.88 ns |  1.02 |    0.23 | 0.0124 |   7,252 B |     104 B |        1.00 |
| CharTryWriteEncode        | 16.23 ns | 0.963 ns | 1.442 ns | 15.57 ns | 15.43 ns | 21.07 ns | 18.31 ns |  0.52 |    0.09 |      - |   4,701 B |         - |        0.00 |
| Utf8TryWrite              | 13.82 ns | 0.055 ns | 0.081 ns | 13.83 ns | 13.68 ns | 14.00 ns | 13.94 ns |  0.45 |    0.07 |      - |   4,787 B |         - |        0.00 |
