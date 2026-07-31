# BUF-05: TemporaryBuffer

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
| Method          | Job                 | Runtime   | Size | Mean      | Error     | StdDev    | Min       | Max        | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |----- |----------:|----------:|----------:|----------:|-----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **AllocateArray**   | **MediumRun-.NET 10.0** | **.NET 10.0** | **64**   |  **3.506 ns** | **0.0433 ns** | **0.0607 ns** |  **3.376 ns** |   **3.610 ns** |  **3.565 ns** |  **1.00** |    **0.02** |      **67 B** | **0.0053** |      **88 B** |        **1.00** |
| TemporaryBuffer | MediumRun-.NET 10.0 | .NET 10.0 | 64   |  5.068 ns | 0.1934 ns | 0.2894 ns |  4.609 ns |   5.589 ns |  5.510 ns |  1.45 |    0.08 |     667 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 10.0 | .NET 10.0 | 64   |  9.168 ns | 0.1178 ns | 0.1763 ns |  8.809 ns |   9.533 ns |  9.347 ns |  2.62 |    0.07 |   2,493 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |            |           |       |         |           |        |           |             |
| AllocateArray   | MediumRun-.NET 8.0  | .NET 8.0  | 64   |  4.368 ns | 0.2016 ns | 0.2955 ns |  3.836 ns |   5.256 ns |  4.720 ns |  1.00 |    0.09 |      72 B | 0.0053 |      88 B |        1.00 |
| TemporaryBuffer | MediumRun-.NET 8.0  | .NET 8.0  | 64   |  5.537 ns | 0.2211 ns | 0.3309 ns |  5.104 ns |   5.983 ns |  5.912 ns |  1.27 |    0.11 |     416 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 8.0  | .NET 8.0  | 64   | 26.394 ns | 0.4707 ns | 0.7046 ns | 25.507 ns |  28.000 ns | 27.323 ns |  6.07 |    0.42 |   1,836 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |            |           |       |         |           |        |           |             |
| AllocateArray   | MediumRun-.NET 9.0  | .NET 9.0  | 64   |  3.729 ns | 0.0720 ns | 0.1078 ns |  3.545 ns |   4.038 ns |  3.861 ns |  1.00 |    0.04 |      72 B | 0.0053 |      88 B |        1.00 |
| TemporaryBuffer | MediumRun-.NET 9.0  | .NET 9.0  | 64   |  5.474 ns | 0.2225 ns | 0.3331 ns |  5.067 ns |   6.072 ns |  5.930 ns |  1.47 |    0.10 |     630 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 9.0  | .NET 9.0  | 64   | 10.108 ns | 0.0949 ns | 0.1420 ns |  9.855 ns |  10.371 ns | 10.298 ns |  2.71 |    0.08 |   2,030 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |            |           |       |         |           |        |           |             |
| **AllocateArray**   | **MediumRun-.NET 10.0** | **.NET 10.0** | **4096** | **84.167 ns** | **5.7324 ns** | **8.5799 ns** | **72.680 ns** | **100.958 ns** | **96.395 ns** |  **1.01** |    **0.14** |      **67 B** | **0.2462** |    **4120 B** |        **1.00** |
| TemporaryBuffer | MediumRun-.NET 10.0 | .NET 10.0 | 4096 |  9.015 ns | 0.0910 ns | 0.1362 ns |  8.802 ns |   9.286 ns |  9.228 ns |  0.11 |    0.01 |   2,894 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 10.0 | .NET 10.0 | 4096 |  9.044 ns | 0.1126 ns | 0.1650 ns |  8.734 ns |   9.384 ns |  9.217 ns |  0.11 |    0.01 |   2,686 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |            |           |       |         |           |        |           |             |
| AllocateArray   | MediumRun-.NET 8.0  | .NET 8.0  | 4096 | 84.456 ns | 2.7936 ns | 4.0948 ns | 75.112 ns |  91.176 ns | 89.185 ns |  1.00 |    0.07 |      72 B | 0.2462 |    4120 B |        1.00 |
| TemporaryBuffer | MediumRun-.NET 8.0  | .NET 8.0  | 4096 | 26.377 ns | 0.2178 ns | 0.3259 ns | 25.893 ns |  26.951 ns | 26.850 ns |  0.31 |    0.02 |   1,952 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 8.0  | .NET 8.0  | 4096 | 26.706 ns | 0.4638 ns | 0.6651 ns | 25.728 ns |  28.360 ns | 27.674 ns |  0.32 |    0.02 |   1,836 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |            |           |       |         |           |        |           |             |
| AllocateArray   | MediumRun-.NET 9.0  | .NET 9.0  | 4096 | 89.824 ns | 2.5026 ns | 3.6684 ns | 79.216 ns |  96.692 ns | 93.244 ns |  1.00 |    0.06 |      72 B | 0.2462 |    4120 B |        1.00 |
| TemporaryBuffer | MediumRun-.NET 9.0  | .NET 9.0  | 4096 | 12.345 ns | 0.1855 ns | 0.2776 ns | 11.946 ns |  13.098 ns | 12.675 ns |  0.14 |    0.01 |   2,370 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 9.0  | .NET 9.0  | 4096 | 10.199 ns | 0.1162 ns | 0.1703 ns |  9.850 ns |  10.496 ns | 10.398 ns |  0.11 |    0.01 |   2,030 B |      - |         - |        0.00 |
