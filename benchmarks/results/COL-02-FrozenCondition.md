# COL-02: FrozenDictionary adoption conditions (string keys)

- Verdict: conditional - and for string keys at these sizes the condition is NOT met
- Build: 10.2x slower at 16 entries (147 -> 1,493 ns), 7.4x at 256 (2,323 -> 17,139 ns); allocation 4.25x both
- Lookup (non-interned probes): 1.05x at 16, 1.04x at 256 - CIs overlap, i.e. NO measurable lookup advantage
- With no lookup win, the 7-10x build cost never amortizes: keep Dictionary for string keys, or use a domain-specific table (COL-04 sampled hash: 0.56-0.84x)
- Consistent with R-08 (unconditional Frozen adoption rejected)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Count | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|-------:|----------:|----------:|------------:|
| **BuildDictionary** | **16**    |    **147.1 ns** |   **3.96 ns** |   **5.67 ns** |    **129.5 ns** |    **158.8 ns** |    **153.4 ns** |  **1.00** |    **0.05** | **0.0362** |   **7,259 B** |     **608 B** |        **1.00** |
| BuildFrozen     | 16    |  1,493.4 ns |  38.27 ns |  57.29 ns |  1,419.1 ns |  1,625.6 ns |  1,569.0 ns | 10.17 |    0.55 | 0.1526 |  43,315 B |    2584 B |        4.25 |
|                 |       |             |           |           |             |             |             |       |         |        |           |           |             |
| **BuildDictionary** | **256**   |  **2,323.3 ns** | **157.37 ns** | **230.67 ns** |  **2,113.4 ns** |  **2,904.9 ns** |  **2,757.0 ns** |  **1.01** |    **0.13** | **0.4959** |   **7,410 B** |    **8336 B** |        **1.00** |
| BuildFrozen     | 256   | 17,138.8 ns | 440.27 ns | 617.20 ns | 16,264.3 ns | 18,528.6 ns | 18,005.2 ns |  7.44 |    0.69 | 2.1057 |  38,501 B |   35416 B |        4.25 |

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Count | Mean       | Error    | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |------ |-----------:|---------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| **LookupDictionary** | **16**    |   **105.4 ns** |  **1.83 ns** |   **2.56 ns** |   **100.8 ns** |   **113.5 ns** |   **108.1 ns** |  **1.00** |    **0.03** |   **1,018 B** |         **-** |          **NA** |
| LookupFrozen     | 16    |   110.8 ns |  3.00 ns |   4.30 ns |   105.9 ns |   121.6 ns |   117.2 ns |  1.05 |    0.05 |   1,065 B |         - |          NA |
|                  |       |            |          |           |            |            |            |       |         |           |           |             |
| **LookupDictionary** | **256**   | **1,736.5 ns** | **48.55 ns** |  **72.67 ns** | **1,644.7 ns** | **1,898.6 ns** | **1,831.0 ns** |  **1.00** |    **0.06** |   **1,057 B** |         **-** |          **NA** |
| LookupFrozen     | 256   | 1,806.8 ns | 77.33 ns | 110.91 ns | 1,631.5 ns | 1,984.0 ns | 1,918.5 ns |  1.04 |    0.08 |   1,070 B |         - |          NA |
