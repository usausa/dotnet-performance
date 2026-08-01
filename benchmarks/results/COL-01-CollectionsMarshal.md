# COL-01: CollectionsMarshal(実測)

判定: 検証済(AsSpan 反復 0.52 / 辞書 ref 化 0.66 / SetCount 一括構築 0.22〜0.26)

## List 反復

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ForEachList   | 486.9 ns | 10.19 ns | 15.25 ns | 454.3 ns | 512.0 ns | 504.2 ns |  1.00 |    0.04 |      68 B |         - |          NA |
| ForList       | 480.4 ns |  7.44 ns | 11.13 ns | 462.8 ns | 500.3 ns | 494.6 ns |  0.99 |    0.04 |      80 B |         - |          NA |
| AsSpanFor     | 252.8 ns |  6.84 ns | 10.02 ns | 235.6 ns | 268.3 ns | 266.2 ns |  0.52 |    0.03 |      68 B |         - |          NA |
| AsSpanForEach | 267.0 ns |  3.24 ns |  4.85 ns | 260.1 ns | 278.0 ns | 274.3 ns |  0.55 |    0.02 |      68 B |         - |          NA |

## 辞書 read-modify-write

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean     | Error    | StdDev   | Min       | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|------------- |---------:|---------:|---------:|----------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| DoubleLookup | 15.51 μs | 0.223 μs | 0.320 μs | 15.023 μs | 16.25 μs | 15.97 μs |  1.00 |    0.03 | 1.3123 |   5,132 B | 0.0610 |  21.71 KB |        1.00 |
| RefLookup    | 10.29 μs | 0.272 μs | 0.408 μs |  9.673 μs | 11.32 μs | 10.74 μs |  0.66 |    0.03 | 1.3275 |   7,232 B | 0.0458 |  21.71 KB |        1.00 |

## SetCount による一括構築

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Size | Mean        | Error     | StdDev     | Min         | Max         | P90         | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------------- |----- |------------:|----------:|-----------:|------------:|------------:|------------:|------:|--------:|-------:|----------:|----------:|------------:|
| **AddLoop**                   | **16**   |    **54.05 ns** |  **3.537 ns** |   **5.293 ns** |    **48.39 ns** |    **65.58 ns** |    **62.83 ns** |  **1.01** |    **0.13** | **0.0129** |   **1,888 B** |     **216 B** |        **1.00** |
| AddLoopCapacity           | 16   |    24.95 ns |  1.384 ns |   2.029 ns |    21.51 ns |    27.74 ns |    27.28 ns |  0.47 |    0.06 | 0.0072 |     273 B |     120 B |        0.56 |
| SetCountSpanWrite         | 16   |    13.96 ns |  0.471 ns |   0.675 ns |    12.69 ns |    15.71 ns |    14.71 ns |  0.26 |    0.03 | 0.0053 |     646 B |      88 B |        0.41 |
| SetCountCapacitySpanWrite | 16   |    17.19 ns |  0.919 ns |   1.347 ns |    14.99 ns |    19.37 ns |    19.07 ns |  0.32 |    0.04 | 0.0072 |     336 B |     120 B |        0.56 |
|                           |      |             |           |            |             |             |             |       |         |        |           |           |             |
| **AddLoop**                   | **1024** | **1,656.02 ns** | **78.766 ns** | **117.894 ns** | **1,448.38 ns** | **1,858.80 ns** | **1,795.98 ns** |  **1.00** |    **0.10** | **0.5035** |   **1,884 B** |    **8424 B** |        **1.00** |
| AddLoopCapacity           | 1024 |   982.72 ns | 79.710 ns | 119.306 ns |   827.53 ns | 1,161.18 ns | 1,123.25 ns |  0.60 |    0.08 | 0.2480 |     273 B |    4152 B |        0.49 |
| SetCountSpanWrite         | 1024 |   356.22 ns |  6.914 ns |  10.134 ns |   337.02 ns |   376.11 ns |   368.88 ns |  0.22 |    0.02 | 0.2460 |     646 B |    4120 B |        0.49 |
| SetCountCapacitySpanWrite | 1024 |   377.22 ns | 19.423 ns |  28.470 ns |   343.28 ns |   445.56 ns |   422.35 ns |  0.23 |    0.02 | 0.2480 |     336 B |    4152 B |        0.49 |
