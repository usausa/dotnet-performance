# TXT-06: ASCII 特化比較

判定: 収録(Ascii.EqualsIgnoreCase 0.62 倍。手書き | 0x20 は 0.43 倍だが記号衝突あり閉集合限定)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error    | StdDev   | Min      | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|---------:|---------:|---------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| StringEqualsIgnoreCase | 101.43 ns | 1.197 ns | 1.792 ns | 98.40 ns | 105.38 ns | 103.45 ns |  1.00 |    0.02 |   1,844 B |         - |          NA |
| AsciiEqualsIgnoreCase  |  63.27 ns | 0.710 ns | 0.995 ns | 61.89 ns |  65.75 ns |  64.52 ns |  0.62 |    0.01 |     907 B |         - |          NA |
| ManualOr20Compare      |  43.62 ns | 0.333 ns | 0.498 ns | 42.95 ns |  44.93 ns |  44.31 ns |  0.43 |    0.01 |     242 B |         - |          NA |
