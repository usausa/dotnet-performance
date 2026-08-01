# DSP-03: Handler array vs multicast delegate

- Verdict: adopted (implemented)
- 1 subscriber: 2.87x (multicast wins - keep a single delegate for that case)
- 2 subscribers: 0.61x / 4: 0.36x / 8: 0.42x (array wins from 2 subscribers up)
- Multicast cost grows steeply with subscriber count (0.84 -> 5.71 -> 11.70 -> 18.85 ns), array stays flat (2.38 -> 7.85 ns)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Subscribers | Mean       | Error     | StdDev    | Median     | Min        | Max       | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------------ |-----------:|----------:|----------:|-----------:|-----------:|----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| **MulticastDelegate** | **1**           |  **0.8388 ns** | **0.0619 ns** | **0.0926 ns** |  **0.8231 ns** |  **0.6985 ns** |  **1.060 ns** |  **0.9583 ns** |  **1.01** |    **0.15** |      **62 B** |         **-** |          **NA** |
| HandlerArray      | 1           |  2.3769 ns | 0.1448 ns | 0.2122 ns |  2.4224 ns |  2.0625 ns |  2.757 ns |  2.6668 ns |  2.87 |    0.39 |      92 B |         - |          NA |
|                   |             |            |           |           |            |            |           |            |       |         |           |           |             |
| **MulticastDelegate** | **2**           |  **5.7058 ns** | **0.0933 ns** | **0.1308 ns** |  **5.6843 ns** |  **5.4708 ns** |  **5.966 ns** |  **5.8645 ns** |  **1.00** |    **0.03** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | 2           |  3.4763 ns | 0.1407 ns | 0.2106 ns |  3.5893 ns |  3.0404 ns |  3.707 ns |  3.6839 ns |  0.61 |    0.04 |      92 B |         - |          NA |
|                   |             |            |           |           |            |            |           |            |       |         |           |           |             |
| **MulticastDelegate** | **4**           | **11.7023 ns** | **0.1410 ns** | **0.2110 ns** | **11.7370 ns** | **11.1464 ns** | **12.065 ns** | **11.9339 ns** |  **1.00** |    **0.03** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | 4           |  4.2612 ns | 0.4543 ns | 0.6660 ns |  4.6135 ns |  3.2444 ns |  5.158 ns |  4.9977 ns |  0.36 |    0.06 |      92 B |         - |          NA |
|                   |             |            |           |           |            |            |           |            |       |         |           |           |             |
| **MulticastDelegate** | **8**           | **18.8480 ns** | **1.6094 ns** | **2.4088 ns** | **19.0937 ns** | **15.5951 ns** | **21.599 ns** | **21.3347 ns** |  **1.02** |    **0.18** |      **33 B** |         **-** |          **NA** |
| HandlerArray      | 8           |  7.8520 ns | 0.7319 ns | 1.0728 ns |  8.3601 ns |  6.5392 ns |  9.268 ns |  9.0347 ns |  0.42 |    0.08 |      92 B |         - |          NA |
