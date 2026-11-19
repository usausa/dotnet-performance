# LAB: InlineArray-backed small list with heap spill vs List<T> (STK-08 follow-up, rejected as R-23)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - not adopted (R-23). Fits: 0.61x of List (net10 0.59x). Spills: 0.67x of a default List but 1.49x of a sized List on net11 (37.6 vs 25.2 ns; 1.54x on net10)

### InlineListBenchmark

net10 → net11 per row:

| Method | Items | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| ListDefault | 4 | 8.392 ns | 8.438 ns | 1.01 | 72 B → 72 B | 1,738 B → 1,779 B |
| ListWithCapacity | 4 | 7.261 ns | 7.286 ns | 1.00 | 72 B → 72 B | 500 B → 321 B |
| InlineListStruct | 4 | 4.916 ns | 5.162 ns | 1.05 | - → - | 377 B → 397 B |
| ListDefault | 32 | 58.128 ns | 55.778 ns | 0.96 | 368 B → 368 B | 1,738 B → 166 B |
| ListWithCapacity | 32 | 24.780 ns | 25.191 ns | 1.02 | 184 B → 184 B | 500 B → 321 B |
| InlineListStruct | 32 | 38.064 ns | 37.603 ns | 0.99 | 240 B → 240 B | 1,007 B → 1,234 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.95 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Items | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| **ListDefault**      | **.NET 10.0**           | **.NET 10.0** | **4**     |  **8.392 ns** | **0.1354 ns** | **0.1898 ns** |  **8.339 ns** |  **8.225 ns** |  **9.039 ns** |  **8.611 ns** |  **1.00** |    **0.00** | **0.0086** |   **1,738 B** |      **72 B** |        **1.00** |
| ListWithCapacity | .NET 10.0           | .NET 10.0 | 4     |  7.261 ns | 0.0256 ns | 0.0341 ns |  7.252 ns |  7.200 ns |  7.328 ns |  7.303 ns |  0.87 |    0.02 | 0.0086 |     500 B |      72 B |        1.00 |
| InlineListStruct | .NET 10.0           | .NET 10.0 | 4     |  4.916 ns | 0.0233 ns | 0.0334 ns |  4.907 ns |  4.878 ns |  5.018 ns |  4.963 ns |  0.59 |    0.01 |      - |     377 B |         - |        0.00 |
|                  |                     |           |       |           |           |           |           |           |           |           |       |         |        |           |           |             |
| ListDefault      | MediumRun-.NET 11.0 | .NET 11.0 | 4     |  8.438 ns | 0.0214 ns | 0.0293 ns |  8.441 ns |  8.366 ns |  8.502 ns |  8.473 ns |  1.00 |    0.00 | 0.0086 |   1,779 B |      72 B |        1.00 |
| ListWithCapacity | MediumRun-.NET 11.0 | .NET 11.0 | 4     |  7.286 ns | 0.0177 ns | 0.0254 ns |  7.280 ns |  7.239 ns |  7.362 ns |  7.316 ns |  0.86 |    0.00 | 0.0086 |     321 B |      72 B |        1.00 |
| InlineListStruct | MediumRun-.NET 11.0 | .NET 11.0 | 4     |  5.162 ns | 0.0555 ns | 0.0814 ns |  5.214 ns |  5.069 ns |  5.270 ns |  5.249 ns |  0.61 |    0.01 |      - |     397 B |         - |        0.00 |
|                  |                     |           |       |           |           |           |           |           |           |           |       |         |        |           |           |             |
| **ListDefault**      | **.NET 10.0**           | **.NET 10.0** | **32**    | **58.128 ns** | **0.6083 ns** | **0.8916 ns** | **57.801 ns** | **56.738 ns** | **60.448 ns** | **59.323 ns** |  **1.00** |    **0.00** | **0.0440** |   **1,738 B** |     **368 B** |        **1.00** |
| ListWithCapacity | .NET 10.0           | .NET 10.0 | 32    | 24.780 ns | 0.1283 ns | 0.1841 ns | 24.781 ns | 24.544 ns | 25.401 ns | 24.911 ns |  0.43 |    0.01 | 0.0220 |     500 B |     184 B |        0.50 |
| InlineListStruct | .NET 10.0           | .NET 10.0 | 32    | 38.064 ns | 0.8657 ns | 1.2958 ns | 38.188 ns | 36.228 ns | 40.935 ns | 39.723 ns |  0.65 |    0.02 | 0.0287 |   1,007 B |     240 B |        0.65 |
|                  |                     |           |       |           |           |           |           |           |           |           |       |         |        |           |           |             |
| ListDefault      | MediumRun-.NET 11.0 | .NET 11.0 | 32    | 55.778 ns | 0.2187 ns | 0.2994 ns | 55.775 ns | 55.195 ns | 56.569 ns | 56.111 ns |  1.00 |    0.00 | 0.0440 |     166 B |     368 B |        1.00 |
| ListWithCapacity | MediumRun-.NET 11.0 | .NET 11.0 | 32    | 25.191 ns | 0.2054 ns | 0.2945 ns | 25.144 ns | 24.759 ns | 25.811 ns | 25.613 ns |  0.45 |    0.01 | 0.0220 |     321 B |     184 B |        0.50 |
| InlineListStruct | MediumRun-.NET 11.0 | .NET 11.0 | 32    | 37.603 ns | 0.8361 ns | 1.2256 ns | 37.046 ns | 35.976 ns | 39.636 ns | 39.021 ns |  0.67 |    0.02 | 0.0287 |   1,234 B |     240 B |        0.65 |

## Earlier record

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
