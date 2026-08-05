# BIT-03: BitOperations scan / popcount

- Verdict: adopted
- Set-bit scan via TrailingZeroCount + (mask &= mask - 1): 0.13x
- PopCount intrinsic vs manual loop: 0.01x

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| SetBitScanLoop    | 1,055.60 ns | 13.918 ns | 19.052 ns | 1,027.61 ns | 1,099.84 ns | 1,079.97 ns |  1.00 |    0.02 |      77 B |         - |          NA |
| SetBitScanTzcnt   |   141.46 ns |  0.957 ns |  1.309 ns |   140.55 ns |   147.38 ns |   142.14 ns |  0.13 |    0.00 |      77 B |         - |          NA |
| PopCountManual    |   853.59 ns |  2.274 ns |  3.262 ns |   847.59 ns |   860.19 ns |   857.82 ns |  0.81 |    0.01 |      73 B |         - |          NA |
| PopCountIntrinsic |    12.83 ns |  0.031 ns |  0.044 ns |    12.76 ns |    12.94 ns |    12.88 ns |  0.01 |    0.00 |      52 B |         - |          NA |
