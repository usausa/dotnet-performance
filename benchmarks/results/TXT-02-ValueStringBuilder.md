# TXT-02: ValueStringBuilder

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 8.0  : .NET 8.0.29 (8.0.29, 8.0.2926.32403), X64 RyuJIT x86-64-v3
  MediumRun-.NET 9.0  : .NET 9.0.18 (9.0.18, 9.0.1826.31522), X64 RyuJIT x86-64-v3

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| StringBuilderDefault  | MediumRun-.NET 10.0 | .NET 10.0 | 78.71 ns | 0.798 ns | 1.169 ns | 76.60 ns | 81.09 ns | 80.15 ns |  1.00 |    0.02 |   1,577 B | 0.0454 |     760 B |        1.00 |
| StringBuilderCapacity | MediumRun-.NET 10.0 | .NET 10.0 | 33.96 ns | 0.951 ns | 1.363 ns | 31.24 ns | 37.38 ns | 35.53 ns |  0.43 |    0.02 |   1,594 B | 0.0325 |     544 B |        0.72 |
| InterpolatedHandler   | MediumRun-.NET 10.0 | .NET 10.0 | 25.85 ns | 0.556 ns | 0.815 ns | 24.53 ns | 27.49 ns | 26.81 ns |  0.33 |    0.01 |   1,344 B | 0.0129 |     216 B |        0.28 |
| ValueStringBuilder    | MediumRun-.NET 10.0 | .NET 10.0 | 24.75 ns | 0.873 ns | 1.252 ns | 23.01 ns | 27.96 ns | 26.87 ns |  0.31 |    0.02 |   1,543 B | 0.0129 |     216 B |        0.28 |
|                       |                     |           |          |          |          |          |          |          |       |         |           |        |           |             |
| StringBuilderDefault  | MediumRun-.NET 8.0  | .NET 8.0  | 80.95 ns | 1.177 ns | 1.726 ns | 78.17 ns | 84.72 ns | 83.49 ns |  1.00 |    0.03 |   1,559 B | 0.0454 |     760 B |        1.00 |
| StringBuilderCapacity | MediumRun-.NET 8.0  | .NET 8.0  | 37.99 ns | 1.928 ns | 2.886 ns | 33.83 ns | 44.39 ns | 41.73 ns |  0.47 |    0.04 |   1,808 B | 0.0325 |     544 B |        0.72 |
| InterpolatedHandler   | MediumRun-.NET 8.0  | .NET 8.0  | 28.17 ns | 0.987 ns | 1.416 ns | 26.12 ns | 31.47 ns | 30.48 ns |  0.35 |    0.02 |   1,263 B | 0.0129 |     216 B |        0.28 |
| ValueStringBuilder    | MediumRun-.NET 8.0  | .NET 8.0  | 26.82 ns | 0.295 ns | 0.432 ns | 25.94 ns | 27.49 ns | 27.37 ns |  0.33 |    0.01 |   1,386 B | 0.0129 |     216 B |        0.28 |
|                       |                     |           |          |          |          |          |          |          |       |         |           |        |           |             |
| StringBuilderDefault  | MediumRun-.NET 9.0  | .NET 9.0  | 81.12 ns | 3.427 ns | 5.129 ns | 75.46 ns | 91.16 ns | 90.21 ns |  1.00 |    0.09 |   1,619 B | 0.0454 |     760 B |        1.00 |
| StringBuilderCapacity | MediumRun-.NET 9.0  | .NET 9.0  | 34.54 ns | 0.644 ns | 0.923 ns | 31.97 ns | 35.96 ns | 35.52 ns |  0.43 |    0.03 |   1,858 B | 0.0325 |     544 B |        0.72 |
| InterpolatedHandler   | MediumRun-.NET 9.0  | .NET 9.0  | 27.36 ns | 1.038 ns | 1.553 ns | 25.33 ns | 30.93 ns | 29.47 ns |  0.34 |    0.03 |   1,292 B | 0.0129 |     216 B |        0.28 |
| ValueStringBuilder    | MediumRun-.NET 9.0  | .NET 9.0  | 25.15 ns | 0.325 ns | 0.467 ns | 24.31 ns | 26.23 ns | 25.76 ns |  0.31 |    0.02 |   1,464 B | 0.0129 |     216 B |        0.28 |
