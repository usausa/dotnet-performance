# LAB: InlineArray-backed small list with heap spill vs List<T> (STK-08 follow-up)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*InlineListBenchmark*"` and replace the tables below. Verdicts rest on generated code and allocation counts, which do not depend on the machine.
>
> ❗ **想定外 / unexpected** — this result contradicts (or goes beyond) the source article's claim. The verdict written below is provisional; decide adoption or rejection only after the HX 370 run confirms or overturns it.

- Verdict: conditional. When the elements fit the inline buffer (4 of 8): 0.35x of List<int> and 0 B (7.56 vs 21.64 ns)
- When it spills (32 elements): 0.68x of a default List, but 1.62x SLOWER than a List created with the right capacity (90.2 vs 55.6 ns, 240 vs 184 B) — the inline copy plus two resizes cost more than one exact allocation
- Adopt only where the upper bound is known and the buffer is sized to cover it; otherwise List<T>(capacity) wins

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Items | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------- |------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **ListDefault**      | **4**     |  **21.642 ns** | **1.3269 ns** | **1.9861 ns** |  **17.837 ns** |  **24.885 ns** |  **23.585 ns** |  **1.01** |    **0.13** |     **691 B** | **0.0043** |      **72 B** |        **1.00** |
| ListWithCapacity | 4     |  16.450 ns | 0.4079 ns | 0.5979 ns |  14.352 ns |  17.301 ns |  17.021 ns |  0.77 |    0.08 |     349 B | 0.0043 |      72 B |        1.00 |
| InlineListStruct | 4     |   7.559 ns | 0.5922 ns | 0.8863 ns |   6.132 ns |   9.494 ns |   8.768 ns |  0.35 |    0.05 |     378 B |      - |         - |        0.00 |
|                  |       |            |           |           |            |            |            |       |         |           |        |           |             |
| **ListDefault**      | **32**    | **133.479 ns** | **5.1207 ns** | **7.6644 ns** | **119.350 ns** | **146.236 ns** | **143.056 ns** |  **1.00** |    **0.08** |   **1,931 B** | **0.0219** |     **368 B** |        **1.00** |
| ListWithCapacity | 32    |  55.627 ns | 2.1092 ns | 3.1570 ns |  48.815 ns |  62.722 ns |  60.178 ns |  0.42 |    0.03 |     349 B | 0.0110 |     184 B |        0.50 |
| InlineListStruct | 32    |  90.154 ns | 4.8246 ns | 7.2213 ns |  77.237 ns | 102.245 ns |  99.259 ns |  0.68 |    0.07 |     994 B | 0.0143 |     240 B |        0.65 |
