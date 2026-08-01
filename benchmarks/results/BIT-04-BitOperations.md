# BIT-04: BitOperations によるビット走査・計数

判定: 収録(TZCNT 走査 0.13 倍、PopCount 0.01 倍)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| SetBitScanLoop    | 1,603.95 ns | 18.165 ns | 27.189 ns | 1,553.77 ns | 1,661.31 ns | 1,636.76 ns | 1.000 |    0.02 |      77 B |         - |          NA |
| SetBitScanTzcnt   |   210.06 ns |  1.306 ns |  1.914 ns |   207.17 ns |   213.71 ns |   212.52 ns | 0.131 |    0.00 |      77 B |         - |          NA |
| PopCountManual    | 1,062.20 ns |  5.716 ns |  8.378 ns | 1,048.73 ns | 1,080.08 ns | 1,071.24 ns | 0.662 |    0.01 |      73 B |         - |          NA |
| PopCountIntrinsic |    15.81 ns |  0.104 ns |  0.153 ns |    15.46 ns |    16.18 ns |    16.06 ns | 0.010 |    0.00 |      52 B |         - |          NA |
