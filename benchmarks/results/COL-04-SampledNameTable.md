# BIT-01 / COL-04: SampledNameTable

- Verdict: adopted (implemented)
- string key: 0.84x (4 names) / 0.78x (16) / 0.77x (32) vs Dictionary
- span key: 0.65x (4) / 0.56x (16) / 0.56x (32) vs Dictionary AlternateLookup; beats FrozenDictionary AlternateLookup at every size
- Linear scan wins at 4 names (0.70x) but degrades fast: 2.73x at 16, 4.59x at 32
- Code size 620-638 B vs 1,043-1,073 B (Dictionary) / 2,001-2,481 B (Frozen)

## String key

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Columns | Mean        | Error     | StdDev    | Median      | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-------- |------------:|----------:|----------:|------------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| **DictionaryLookup** | **4**       |    **31.53 ns** |  **3.277 ns** |  **4.904 ns** |    **29.67 ns** |    **26.73 ns** |    **42.40 ns** |    **41.02 ns** |  **1.02** |    **0.21** |   **1,053 B** |         **-** |          **NA** |
| LinearScan       | 4       |    21.74 ns |  3.308 ns |  4.849 ns |    19.64 ns |    18.08 ns |    32.39 ns |    31.59 ns |  0.70 |    0.18 |     482 B |         - |          NA |
| SampledHashTable | 4       |    25.86 ns |  1.932 ns |  2.891 ns |    26.96 ns |    19.35 ns |    28.55 ns |    28.10 ns |  0.84 |    0.15 |     621 B |         - |          NA |
|                  |         |             |           |           |             |             |             |             |       |         |           |           |             |
| **DictionaryLookup** | **16**      |   **125.00 ns** | **13.151 ns** | **19.684 ns** |   **115.80 ns** |   **109.32 ns** |   **168.18 ns** |   **160.72 ns** |  **1.02** |    **0.21** |   **1,043 B** |         **-** |          **NA** |
| LinearScan       | 16      |   334.63 ns |  7.601 ns | 10.901 ns |   334.64 ns |   318.35 ns |   363.41 ns |   346.68 ns |  2.73 |    0.37 |     486 B |         - |          NA |
| SampledHashTable | 16      |    94.96 ns |  2.001 ns |  2.995 ns |    95.27 ns |    86.56 ns |    99.21 ns |    98.37 ns |  0.78 |    0.10 |     638 B |         - |          NA |
|                  |         |             |           |           |             |             |             |             |       |         |           |           |             |
| **DictionaryLookup** | **32**      |   **285.97 ns** | **23.747 ns** | **35.543 ns** |   **299.63 ns** |   **207.26 ns** |   **329.72 ns** |   **315.07 ns** |  **1.02** |    **0.19** |   **1,073 B** |         **-** |          **NA** |
| LinearScan       | 32      | 1,289.73 ns | 53.840 ns | 80.585 ns | 1,301.61 ns | 1,090.44 ns | 1,404.10 ns | 1,373.96 ns |  4.59 |    0.73 |     480 B |         - |          NA |
| SampledHashTable | 32      |   216.06 ns | 23.679 ns | 35.442 ns |   196.74 ns |   181.97 ns |   291.57 ns |   272.31 ns |  0.77 |    0.17 |     626 B |         - |          NA |

## Span key (.NET 9+ AlternateLookup comparison)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Columns | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------------- |-------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **DictionaryAlternateLookup** | **4**       |  **38.52 ns** |  **4.339 ns** |  **6.495 ns** |  **41.82 ns** |  **27.75 ns** |  **45.59 ns** |  **43.93 ns** |  **1.03** |    **0.26** |   **1,599 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | 4       |  49.43 ns |  1.663 ns |  2.489 ns |  49.33 ns |  44.05 ns |  55.07 ns |  52.64 ns |  1.33 |    0.26 |   2,481 B |         - |          NA |
| SampledHashTable          | 4       |  24.41 ns |  0.751 ns |  1.100 ns |  24.79 ns |  20.97 ns |  25.58 ns |  25.26 ns |  0.65 |    0.13 |     620 B |         - |          NA |
|                           |         |           |           |           |           |           |           |           |       |         |           |           |             |
| **DictionaryAlternateLookup** | **16**      | **161.72 ns** | **14.541 ns** | **21.765 ns** | **171.03 ns** | **111.79 ns** | **181.90 ns** | **178.23 ns** |  **1.02** |    **0.22** |   **1,608 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | 16      | 108.98 ns |  1.613 ns |  2.365 ns | 108.65 ns | 104.85 ns | 113.90 ns | 111.26 ns |  0.69 |    0.11 |   2,040 B |         - |          NA |
| SampledHashTable          | 16      |  89.28 ns |  7.517 ns | 11.251 ns |  94.80 ns |  68.81 ns | 101.24 ns |  98.23 ns |  0.56 |    0.12 |     638 B |         - |          NA |
|                           |         |           |           |           |           |           |           |           |       |         |           |           |             |
| **DictionaryAlternateLookup** | **32**      | **335.99 ns** |  **8.303 ns** | **12.427 ns** | **338.26 ns** | **293.13 ns** | **355.04 ns** | **348.88 ns** |  **1.00** |    **0.05** |   **1,599 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | 32      | 299.96 ns | 18.358 ns | 27.477 ns | 310.99 ns | 218.77 ns | 322.60 ns | 315.37 ns |  0.89 |    0.09 |   2,001 B |         - |          NA |
| SampledHashTable          | 32      | 188.27 ns |  3.182 ns |  4.763 ns | 187.49 ns | 180.47 ns | 198.33 ns | 195.53 ns |  0.56 |    0.03 |     621 B |         - |          NA |
