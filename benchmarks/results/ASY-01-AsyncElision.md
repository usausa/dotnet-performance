# ASY-01: async ステートマシンの省略

判定: 収録(Task 直接返し 0.16 倍・73B→0B。ValueTask でも await ラッパーは 0.58 → 直接 0.15)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error     | StdDev    | Median    | Min      | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|---------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| TaskAwaitForward       | 11.781 ns | 1.4599 ns | 2.1851 ns | 10.658 ns | 9.781 ns | 16.779 ns | 15.266 ns |  1.03 |    0.25 | 0.0043 |   2,041 B |      73 B |        1.00 |
| TaskDirectForward      |  1.833 ns | 0.0504 ns | 0.0754 ns |  1.812 ns | 1.708 ns |  2.049 ns |  1.932 ns |  0.16 |    0.03 | 0.0000 |   1,248 B |       1 B |        0.01 |
| ValueTaskAwaitForward  |  6.687 ns | 0.1363 ns | 0.2040 ns |  6.690 ns | 6.403 ns |  7.114 ns |  6.976 ns |  0.58 |    0.09 | 0.0000 |   3,746 B |       1 B |        0.01 |
| ValueTaskDirectForward |  1.723 ns | 0.0244 ns | 0.0365 ns |  1.713 ns | 1.685 ns |  1.821 ns |  1.790 ns |  0.15 |    0.02 | 0.0000 |   2,228 B |       1 B |        0.01 |
