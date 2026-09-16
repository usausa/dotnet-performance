# LAB: InlineArray-backed small list with heap spill vs List<T> (STK-08 follow-up, rejected as R-23)

- Verdict: NOT adopted (R-23). When the elements fit the inline buffer (4 of 8): 0.49x of List<int> and 0 B (4.30 vs 8.78 ns; the sized List is 8.14 ns)
- When it spills (32 elements): 0.75x of a default List, but 1.76x SLOWER than a List created with the right capacity (44.1 vs 25.0 ns, CIs disjoint) and it allocates more (240 vs 184 B) — the inline copy plus two resizes cost more than one exact allocation
- The B550H (x86-64-v3) provisional run agrees on both signs (0.35x while it fits, 1.62x behind the sized List when it spills) with identical allocation counts (0 / 240 / 184 B), so the result does not depend on the machine
- Why rejected rather than conditional: it only wins when the upper bound is known and the inline capacity covers it — and in that case the spill path is never taken, so STK-08's fixed-length InlineArray already does the job. With an unknown bound it loses to List<T>(capacity) / BUF-05. There is no configuration left where the derived form adds value

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Items | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| **ListDefault**      | **4**     |  **8.776 ns** | **0.2172 ns** | **0.3115 ns** |  **8.776 ns** |  **8.330 ns** |  **9.495 ns** |  **9.191 ns** |  **1.00** |    **0.05** | **0.0086** |     **691 B** |      **72 B** |        **1.00** |
| ListWithCapacity | 4     |  8.138 ns | 0.0717 ns | 0.1028 ns |  8.142 ns |  7.871 ns |  8.350 ns |  8.245 ns |  0.93 |    0.03 | 0.0086 |     328 B |      72 B |        1.00 |
| InlineListStruct | 4     |  4.297 ns | 0.0397 ns | 0.0582 ns |  4.301 ns |  4.214 ns |  4.437 ns |  4.368 ns |  0.49 |    0.02 |      - |     377 B |         - |        0.00 |
|                  |       |           |           |           |           |           |           |           |       |         |        |           |           |             |
| **ListDefault**      | **32**    | **58.536 ns** | **0.7302 ns** | **1.0703 ns** | **58.414 ns** | **56.778 ns** | **61.009 ns** | **60.089 ns** |  **1.00** |    **0.03** | **0.0440** |   **1,915 B** |     **368 B** |        **1.00** |
| ListWithCapacity | 32    | 25.009 ns | 0.3396 ns | 0.4534 ns | 24.932 ns | 24.087 ns | 26.001 ns | 25.492 ns |  0.43 |    0.01 | 0.0220 |     328 B |     184 B |        0.50 |
| InlineListStruct | 32    | 44.143 ns | 0.9358 ns | 1.3717 ns | 43.416 ns | 41.979 ns | 46.120 ns | 45.703 ns |  0.75 |    0.03 | 0.0287 |     982 B |     240 B |        0.65 |
