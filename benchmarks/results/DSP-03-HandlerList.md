# DSP-03: Handler array vs multicast delegate

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
