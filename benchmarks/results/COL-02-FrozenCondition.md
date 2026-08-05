# COL-02: FrozenDictionary adoption conditions (string keys)

- Verdict: conditional - and for string keys the condition is NOT met at any measured size (16 / 256 / 1024)
- Build: 10.0x slower at 16 entries (82 -> 817 ns), 8.5x at 256 (1,309 -> 11,091 ns), 5.3x at 1024 (6.2 -> 32.5 us, allocation 122 KB)
- Lookup (non-interned probes): 1.07x at 16, 0.97x at 256 (CIs overlap = no measurable advantage); **1.19x SLOWER at 1024** (5,970 vs 5,031 ns, non-overlapping CIs)
- The revival check at 1024 strengthened the rejection: scaling up does not help - the lookup side gets measurably worse, not better
- With no lookup win anywhere, the 5-10x build cost never amortizes: keep Dictionary for string keys, or use a domain-specific table (COL-04 sampled hash: 0.60-0.62x)
- Consistent with R-08 (unconditional Frozen adoption rejected)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Count | Mean         | Error      | StdDev     | Min          | Max          | P90          | Ratio | RatioSD | Gen0    | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------- |------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|--------:|----------:|-------:|----------:|------------:|
| **BuildDictionary** | **16**    |     **81.82 ns** |   **0.677 ns** |   **1.013 ns** |     **78.25 ns** |     **83.16 ns** |     **83.01 ns** |  **1.00** |    **0.02** |  **0.0726** |   **7,225 B** |      **-** |     **608 B** |        **1.00** |
| BuildFrozen     | 16    |    817.16 ns |   6.097 ns |   9.125 ns |    797.06 ns |    828.53 ns |    827.13 ns |  9.99 |    0.17 |  0.3080 |  43,353 B |      - |    2584 B |        4.25 |
|                 |       |              |            |            |              |              |              |       |         |         |           |        |           |             |
| **BuildDictionary** | **256**   |  **1,308.81 ns** |  **12.711 ns** |  **19.025 ns** |  **1,253.77 ns** |  **1,338.71 ns** |  **1,327.04 ns** |  **1.00** |    **0.02** |  **0.9956** |   **7,376 B** |      **-** |    **8336 B** |        **1.00** |
| BuildFrozen     | 256   | 11,091.43 ns | 256.323 ns | 367.611 ns | 10,700.36 ns | 12,104.41 ns | 11,583.95 ns |  8.48 |    0.30 |  4.2267 |  38,593 B |      - |   35416 B |        4.25 |
|                 |       |              |            |            |              |              |              |       |         |         |           |        |           |             |
| **BuildDictionary** | **1024**  |  **6,169.14 ns** |  **35.580 ns** |  **52.153 ns** |  **6,065.40 ns** |  **6,286.64 ns** |  **6,220.98 ns** |  **1.00** |    **0.01** |  **3.6850** |   **4,167 B** |      **-** |   **30936 B** |        **1.00** |
| BuildFrozen     | 1024  | 32,474.19 ns | 167.957 ns | 251.390 ns | 31,787.40 ns | 32,954.37 ns | 32,825.48 ns |  5.26 |    0.06 | 14.5874 |  41,903 B | 1.6479 |  122344 B |        3.95 |

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Count | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| **LookupDictionary** | **16**    |    **70.92 ns** |  **1.390 ns** |  **2.081 ns** |    **69.57 ns** |    **77.34 ns** |    **74.50 ns** |  **1.00** |    **0.04** |   **1,091 B** |         **-** |          **NA** |
| LookupFrozen     | 16    |    75.58 ns |  0.699 ns |  1.003 ns |    74.16 ns |    77.68 ns |    76.82 ns |  1.07 |    0.03 |   1,135 B |         - |          NA |
|                  |       |             |           |           |             |             |             |       |         |           |           |             |
| **LookupDictionary** | **256**   | **1,371.54 ns** | **17.468 ns** | **26.146 ns** | **1,319.88 ns** | **1,407.57 ns** | **1,396.12 ns** |  **1.00** |    **0.03** |   **1,127 B** |         **-** |          **NA** |
| LookupFrozen     | 256   | 1,326.54 ns | 19.418 ns | 28.462 ns | 1,271.76 ns | 1,367.46 ns | 1,357.24 ns |  0.97 |    0.03 |   1,140 B |         - |          NA |
|                  |       |             |           |           |             |             |             |       |         |           |           |             |
| **LookupDictionary** | **1024**  | **5,030.77 ns** | **38.582 ns** | **55.334 ns** | **4,914.05 ns** | **5,116.71 ns** | **5,098.15 ns** |  **1.00** |    **0.02** |   **1,119 B** |         **-** |          **NA** |
| LookupFrozen     | 1024  | 5,970.29 ns | 55.431 ns | 82.966 ns | 5,816.49 ns | 6,125.00 ns | 6,075.30 ns |  1.19 |    0.02 |   1,140 B |         - |          NA |
