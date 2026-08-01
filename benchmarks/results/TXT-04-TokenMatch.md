# TXT-04: バイト列トークンの直接判定

判定: 収録(string 化回避で 0.23 倍・0B。SequenceEqual u8 と uint 定数比較は同速、uint はコードサイズ減のみ)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| StringSwitch        | 499.5 ns | 7.37 ns | 11.03 ns | 481.6 ns | 522.8 ns | 515.7 ns |  1.00 |    0.03 |   1,192 B | 0.1221 |    2048 B |        1.00 |
| SequenceEqualChain  | 115.4 ns | 0.76 ns |  1.13 ns | 113.4 ns | 117.9 ns | 117.0 ns |  0.23 |    0.01 |     226 B |      - |         - |        0.00 |
| UIntConstantCompare | 115.2 ns | 1.31 ns |  1.96 ns | 111.5 ns | 117.8 ns | 117.6 ns |  0.23 |    0.01 |     166 B |      - |         - |        0.00 |
