# DSP-03: Handler array vs multicast delegate

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the multicast delegate wins for one subscriber (array 1.62x of it), the array wins from two subscribers (0.39x / 0.35x / 0.34x at 2 / 4 / 8 on net11)

### HandlerListBenchmark

net10 → net11 per row:

| Method | Subscribers | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| MulticastDelegate | 1 | 0.8653 ns | 0.8366 ns | 0.97 | - → - | 62 B → 62 B |
| HandlerArray | 1 | 1.3499 ns | 1.3512 ns | 1.00 | - → - | 92 B → 92 B |
| MulticastDelegate | 2 | 4.1946 ns | 4.2044 ns | 1.00 | - → - | 33 B → 33 B |
| HandlerArray | 2 | 1.6633 ns | 1.6595 ns | 1.00 | - → - | 92 B → 92 B |
| MulticastDelegate | 4 | 6.7715 ns | 6.6877 ns | 0.99 | - → - | 33 B → 33 B |
| HandlerArray | 4 | 2.3405 ns | 2.3384 ns | 1.00 | - → - | 92 B → 92 B |
| MulticastDelegate | 8 | 11.7780 ns | 11.9230 ns | 1.01 | - → - | 33 B → 33 B |
| HandlerArray | 8 | 4.2916 ns | 4.0261 ns | 0.94 | - → - | 92 B → 92 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.39 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Subscribers | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| **MulticastDelegate** | **.NET 10.0**           | **.NET 10.0** | **1**           |  **0.8653 ns** | **0.0296 ns** | **0.0424 ns** |  **0.8320 ns** |  **0.8210 ns** |  **0.9234 ns** |  **0.9141 ns** |  **1.00** |    **0.00** |      **62 B** |         **-** |          **NA** |
| HandlerArray      | .NET 10.0           | .NET 10.0 | 1           |  1.3499 ns | 0.0085 ns | 0.0121 ns |  1.3459 ns |  1.3348 ns |  1.3792 ns |  1.3667 ns |  1.56 |    0.08 |      92 B |         - |          NA |
|                   |                     |           |             |            |           |           |            |            |            |            |       |         |           |           |             |
| MulticastDelegate | MediumRun-.NET 11.0 | .NET 11.0 | 1           |  0.8366 ns | 0.0096 ns | 0.0141 ns |  0.8324 ns |  0.8176 ns |  0.8576 ns |  0.8529 ns |  1.00 |    0.00 |      62 B |         - |          NA |
| HandlerArray      | MediumRun-.NET 11.0 | .NET 11.0 | 1           |  1.3512 ns | 0.0049 ns | 0.0071 ns |  1.3509 ns |  1.3385 ns |  1.3647 ns |  1.3596 ns |  1.62 |    0.03 |      92 B |         - |          NA |
|                   |                     |           |             |            |           |           |            |            |            |            |       |         |           |           |             |
| **MulticastDelegate** | **.NET 10.0**           | **.NET 10.0** | **2**           |  **4.1946 ns** | **0.0233 ns** | **0.0341 ns** |  **4.1939 ns** |  **4.1119 ns** |  **4.2639 ns** |  **4.2388 ns** |  **1.00** |    **0.00** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | .NET 10.0           | .NET 10.0 | 2           |  1.6633 ns | 0.0058 ns | 0.0083 ns |  1.6634 ns |  1.6518 ns |  1.6825 ns |  1.6739 ns |  0.40 |    0.00 |      92 B |         - |          NA |
|                   |                     |           |             |            |           |           |            |            |            |            |       |         |           |           |             |
| MulticastDelegate | MediumRun-.NET 11.0 | .NET 11.0 | 2           |  4.2044 ns | 0.0146 ns | 0.0214 ns |  4.2070 ns |  4.1535 ns |  4.2424 ns |  4.2303 ns |  1.00 |    0.00 |      33 B |         - |          NA |
| HandlerArray      | MediumRun-.NET 11.0 | .NET 11.0 | 2           |  1.6595 ns | 0.0042 ns | 0.0059 ns |  1.6592 ns |  1.6493 ns |  1.6727 ns |  1.6661 ns |  0.39 |    0.00 |      92 B |         - |          NA |
|                   |                     |           |             |            |           |           |            |            |            |            |       |         |           |           |             |
| **MulticastDelegate** | **.NET 10.0**           | **.NET 10.0** | **4**           |  **6.7715 ns** | **0.0752 ns** | **0.1102 ns** |  **6.7567 ns** |  **6.6153 ns** |  **7.0231 ns** |  **6.9127 ns** |  **1.00** |    **0.00** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | .NET 10.0           | .NET 10.0 | 4           |  2.3405 ns | 0.0071 ns | 0.0102 ns |  2.3442 ns |  2.3239 ns |  2.3569 ns |  2.3514 ns |  0.35 |    0.01 |      92 B |         - |          NA |
|                   |                     |           |             |            |           |           |            |            |            |            |       |         |           |           |             |
| MulticastDelegate | MediumRun-.NET 11.0 | .NET 11.0 | 4           |  6.6877 ns | 0.0321 ns | 0.0460 ns |  6.6849 ns |  6.6179 ns |  6.7787 ns |  6.7413 ns |  1.00 |    0.00 |      33 B |         - |          NA |
| HandlerArray      | MediumRun-.NET 11.0 | .NET 11.0 | 4           |  2.3384 ns | 0.0089 ns | 0.0128 ns |  2.3362 ns |  2.3205 ns |  2.3772 ns |  2.3522 ns |  0.35 |    0.00 |      92 B |         - |          NA |
|                   |                     |           |             |            |           |           |            |            |            |            |       |         |           |           |             |
| **MulticastDelegate** | **.NET 10.0**           | **.NET 10.0** | **8**           | **11.7780 ns** | **0.0648 ns** | **0.0949 ns** | **11.7590 ns** | **11.5951 ns** | **11.9516 ns** | **11.8841 ns** |  **1.00** |    **0.00** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | .NET 10.0           | .NET 10.0 | 8           |  4.2916 ns | 0.5149 ns | 0.7384 ns |  4.0769 ns |  3.7178 ns |  6.2697 ns |  5.2804 ns |  0.36 |    0.06 |      92 B |         - |          NA |
|                   |                     |           |             |            |           |           |            |            |            |            |       |         |           |           |             |
| MulticastDelegate | MediumRun-.NET 11.0 | .NET 11.0 | 8           | 11.9230 ns | 0.1621 ns | 0.2219 ns | 11.9579 ns | 11.5929 ns | 12.2846 ns | 12.2019 ns |  1.00 |    0.00 |      33 B |         - |          NA |
| HandlerArray      | MediumRun-.NET 11.0 | .NET 11.0 | 8           |  4.0261 ns | 0.3505 ns | 0.5137 ns |  3.8061 ns |  3.7251 ns |  5.3220 ns |  5.0858 ns |  0.34 |    0.04 |      92 B |         - |          NA |

## Earlier record

- Verdict: adopted (implemented)
- 1 subscriber: multicast 0.12 ns vs array 0.68 ns (multicast wins - keep a single delegate for that case)
- 2 subscribers: 0.32x / 4: 0.31x / 8: 0.32x (array wins from 2 subscribers up)
- Multicast cost grows steeply with subscriber count (3.49 -> 6.05 -> 11.00 ns), the array loop stays flat and allocation-free (1.11 -> 3.47 ns)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Subscribers | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| **MulticastDelegate** | **1**           |  **0.1224 ns** | **0.0155 ns** | **0.0218 ns** |  **0.0890 ns** |  **0.1867 ns** |  **0.1482 ns** |  **1.03** |    **0.24** |      **62 B** |         **-** |          **NA** |
| HandlerArray      | 1           |  0.6815 ns | 0.0137 ns | 0.0182 ns |  0.6519 ns |  0.7095 ns |  0.7053 ns |  5.72 |    0.88 |      92 B |         - |          NA |
|                   |             |            |           |           |            |            |            |       |         |           |           |             |
| **MulticastDelegate** | **2**           |  **3.4934 ns** | **0.0490 ns** | **0.0671 ns** |  **3.4271 ns** |  **3.7355 ns** |  **3.5825 ns** |  **1.00** |    **0.03** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | 2           |  1.1092 ns | 0.0086 ns | 0.0124 ns |  1.0856 ns |  1.1332 ns |  1.1250 ns |  0.32 |    0.01 |      92 B |         - |          NA |
|                   |             |            |           |           |            |            |            |       |         |           |           |             |
| **MulticastDelegate** | **4**           |  **6.0515 ns** | **0.0611 ns** | **0.0837 ns** |  **5.9486 ns** |  **6.2467 ns** |  **6.1695 ns** |  **1.00** |    **0.02** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | 4           |  1.8544 ns | 0.0129 ns | 0.0186 ns |  1.8236 ns |  1.9103 ns |  1.8698 ns |  0.31 |    0.01 |      92 B |         - |          NA |
|                   |             |            |           |           |            |            |            |       |         |           |           |             |
| **MulticastDelegate** | **8**           | **10.9991 ns** | **0.1287 ns** | **0.1845 ns** | **10.7692 ns** | **11.4160 ns** | **11.2702 ns** |  **1.00** |    **0.02** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | 8           |  3.4675 ns | 0.0698 ns | 0.1001 ns |  3.3828 ns |  3.7603 ns |  3.6336 ns |  0.32 |    0.01 |      92 B |         - |          NA |
