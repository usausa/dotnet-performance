# TXT-05: Utf8.TryWrite による UTF-8 直接整形

判定: 収録(string 補間+Encode 比 0.54 倍・0B。char TryWrite+Encode より速い)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| StringInterpolationEncode | 42.10 ns | 0.489 ns | 0.732 ns | 40.61 ns | 43.28 ns | 43.00 ns |  1.00 |    0.02 |   6,955 B | 0.0062 |     104 B |        1.00 |
| CharTryWriteEncode        | 25.20 ns | 0.158 ns | 0.236 ns | 24.75 ns | 25.63 ns | 25.47 ns |  0.60 |    0.01 |   4,533 B |      - |         - |        0.00 |
| Utf8TryWrite              | 22.71 ns | 0.175 ns | 0.262 ns | 22.34 ns | 23.38 ns | 23.04 ns |  0.54 |    0.01 |   4,490 B |      - |         - |        0.00 |
