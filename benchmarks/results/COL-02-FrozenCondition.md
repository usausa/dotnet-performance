# COL-02: FrozenDictionary adoption conditions (string keys)

## net11 / net10 re-verification of the build side (2026-11-18, HX 370)

- Verdict: unchanged. Building a `FrozenDictionary` is not cheaper on net11
- Build ratio against `Dictionary`: 9.4 / 7.6 / 5.4x on net10 and 10.7 / 8.3 / 5.5x on net11 (16 / 256 / 1024 entries);
  allocation is identical on both (2,584 / 35,416 / 122,344 B)
- The net11 pre-sizing of the temporary dictionary (#128300) does not show for a `KeyValuePair<string, int>[]`
  source, whose count is already known. The external 4,096-entry result (0.36x) uses a different source shape
- The lookup side was not re-measured; "1.19x slower lookup at 1024" below stands

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.27 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method          | Job                 | Runtime   | Count | Mean         | Error      | StdDev     | Min          | Max          | P90          | Ratio | RatioSD | Code Size | Gen0    | Gen1   | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|----------:|--------:|-------:|----------:|------------:|
| **BuildDictionary** | **.NET 10.0**           | **.NET 10.0** | **16**    |     **83.02 ns** |   **0.679 ns** |   **0.952 ns** |     **81.24 ns** |     **84.64 ns** |     **84.11 ns** |  **1.00** |    **0.00** |   **4,946 B** |  **0.0631** |      **-** |     **528 B** |        **1.00** |
| BuildFrozen     | .NET 10.0           | .NET 10.0 | 16    |    779.57 ns |   5.970 ns |   8.750 ns |    765.41 ns |    801.18 ns |    791.75 ns |  9.39 |    0.15 |  13,657 B |  0.3080 |      - |    2584 B |        4.89 |
|                 |                     |           |       |              |            |            |              |              |              |       |         |           |         |        |           |             |
| BuildDictionary | MediumRun-.NET 11.0 | .NET 11.0 | 16    |     82.78 ns |   0.420 ns |   0.602 ns |     82.04 ns |     84.12 ns |     83.70 ns |  1.00 |    0.00 |   5,481 B |  0.0631 |      - |     528 B |        1.00 |
| BuildFrozen     | MediumRun-.NET 11.0 | .NET 11.0 | 16    |    886.91 ns |   7.101 ns |  10.628 ns |    872.35 ns |    918.06 ns |    897.91 ns | 10.71 |    0.15 |  14,034 B |  0.3080 | 0.0010 |    2584 B |        4.89 |
|                 |                     |           |       |              |            |            |              |              |              |       |         |           |         |        |           |             |
| **BuildDictionary** | **.NET 10.0**           | **.NET 10.0** | **256**   |  **1,350.88 ns** |   **9.452 ns** |  **13.854 ns** |  **1,326.81 ns** |  **1,387.32 ns** |  **1,368.49 ns** |  **1.00** |    **0.00** |   **5,313 B** |  **0.9880** | **0.0038** |    **8256 B** |        **1.00** |
| BuildFrozen     | .NET 10.0           | .NET 10.0 | 256   | 10,329.55 ns | 198.110 ns | 284.124 ns |  9,962.16 ns | 10,675.37 ns | 10,656.62 ns |  7.65 |    0.22 |  14,417 B |  4.2267 | 0.2441 |   35416 B |        4.29 |
|                 |                     |           |       |              |            |            |              |              |              |       |         |           |         |        |           |             |
| BuildDictionary | MediumRun-.NET 11.0 | .NET 11.0 | 256   |  1,360.82 ns |   5.985 ns |   8.958 ns |  1,346.67 ns |  1,382.20 ns |  1,372.88 ns |  1.00 |    0.00 |   5,833 B |  0.9880 | 0.0038 |    8256 B |        1.00 |
| BuildFrozen     | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 11,266.02 ns |  49.089 ns |  65.532 ns | 11,176.84 ns | 11,486.40 ns | 11,337.83 ns |  8.28 |    0.07 |  14,946 B |  4.2267 |      - |   35416 B |        4.29 |
|                 |                     |           |       |              |            |            |              |              |              |       |         |           |         |        |           |             |
| **BuildDictionary** | **.NET 10.0**           | **.NET 10.0** | **1024**  |  **5,883.97 ns** |  **19.527 ns** |  **28.005 ns** |  **5,819.93 ns** |  **5,935.54 ns** |  **5,916.66 ns** |  **1.00** |    **0.00** |   **4,956 B** |  **3.6850** |      **-** |   **30936 B** |        **1.00** |
| BuildFrozen     | .NET 10.0           | .NET 10.0 | 1024  | 31,702.82 ns | 133.637 ns | 182.924 ns | 31,369.38 ns | 32,104.58 ns | 31,982.79 ns |  5.39 |    0.04 |  13,592 B | 14.5874 | 1.4038 |  122344 B |        3.95 |
|                 |                     |           |       |              |            |            |              |              |              |       |         |           |         |        |           |             |
| BuildDictionary | MediumRun-.NET 11.0 | .NET 11.0 | 1024  |  5,971.99 ns |  26.196 ns |  37.570 ns |  5,903.48 ns |  6,053.27 ns |  6,018.62 ns |  1.00 |    0.00 |   5,485 B |  3.6850 |      - |   30936 B |        1.00 |
| BuildFrozen     | MediumRun-.NET 11.0 | .NET 11.0 | 1024  | 33,143.49 ns | 287.081 ns | 402.447 ns | 32,436.01 ns | 33,870.61 ns | 33,610.75 ns |  5.55 |    0.07 |  13,992 B | 14.5874 | 1.5869 |  122344 B |        3.95 |

## Earlier record

- Verdict: conditional - and for string keys the condition is NOT met at any measured size (16 / 256 / 1024)
- Build: 10.0x slower at 16 entries (82 -> 817 ns), 8.5x at 256 (1,309 -> 11,091 ns), 5.3x at 1024 (6.2 -> 32.5 us, allocation 122 KB)
- Lookup (non-interned probes): 1.07x at 16, 0.97x at 256 (CIs overlap = no measurable advantage); **1.19x SLOWER at 1024** (5,970 vs 5,031 ns, non-overlapping CIs)
- Scale does not rescue the trade-off: the larger the table, the worse the lookup side gets relative to Dictionary
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
