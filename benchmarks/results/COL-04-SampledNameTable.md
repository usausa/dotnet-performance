# BIT-01 / COL-04: SampledNameTable

- Verdict: adopted (implemented)
- string key: 0.60x (4 names) / 0.62x (16) / 0.75x (32) vs Dictionary
- span key: 0.59x (4) / 0.60x (16) / 0.75x (32) vs Dictionary AlternateLookup; beats FrozenDictionary AlternateLookup at every size
- Linear scan is on par at 4 names (0.62x, same as the table) but degrades fast: 1.77x at 16, 3.23x at 32
- Code size 692-706 B vs 1,081-1,110 B (Dictionary) / 2,126-2,177 B (Frozen)

## String key

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Columns | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **DictionaryLookup** | **4**       |  **20.08 ns** |  **0.061 ns** |  **0.088 ns** |  **20.08 ns** |  **19.88 ns** |  **20.25 ns** |  **20.19 ns** |  **1.00** |    **0.01** |   **1,081 B** |         **-** |          **NA** |
| LinearScan       | 4       |  12.39 ns |  0.087 ns |  0.125 ns |  12.39 ns |  12.16 ns |  12.65 ns |  12.52 ns |  0.62 |    0.01 |     523 B |         - |          NA |
| SampledHashTable | 4       |  11.98 ns |  0.029 ns |  0.042 ns |  11.97 ns |  11.92 ns |  12.06 ns |  12.04 ns |  0.60 |    0.00 |     692 B |         - |          NA |
|                  |         |           |           |           |           |           |           |           |       |         |           |           |             |
| **DictionaryLookup** | **16**      |  **76.51 ns** |  **0.408 ns** |  **0.598 ns** |  **76.59 ns** |  **75.38 ns** |  **77.65 ns** |  **77.18 ns** |  **1.00** |    **0.01** |   **1,110 B** |         **-** |          **NA** |
| LinearScan       | 16      | 135.39 ns |  4.518 ns |  6.031 ns | 139.89 ns | 127.94 ns | 141.92 ns | 141.62 ns |  1.77 |    0.08 |     567 B |         - |          NA |
| SampledHashTable | 16      |  47.79 ns |  0.623 ns |  0.852 ns |  47.48 ns |  47.01 ns |  50.75 ns |  48.65 ns |  0.62 |    0.01 |     706 B |         - |          NA |
|                  |         |           |           |           |           |           |           |           |       |         |           |           |             |
| **DictionaryLookup** | **32**      | **150.13 ns** |  **0.911 ns** |  **1.364 ns** | **150.08 ns** | **148.15 ns** | **153.61 ns** | **152.28 ns** |  **1.00** |    **0.01** |   **1,102 B** |         **-** |          **NA** |
| LinearScan       | 32      | 485.10 ns | 32.674 ns | 44.724 ns | 468.34 ns | 458.59 ns | 667.58 ns | 516.85 ns |  3.23 |    0.29 |     557 B |         - |          NA |
| SampledHashTable | 32      | 113.02 ns |  0.488 ns |  0.731 ns | 113.04 ns | 111.88 ns | 114.90 ns | 113.90 ns |  0.75 |    0.01 |     697 B |         - |          NA |

## Span key (.NET 9+ AlternateLookup comparison)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Columns | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------------- |-------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **DictionaryAlternateLookup** | **4**       |  **20.30 ns** | **0.201 ns** | **0.275 ns** |  **20.01 ns** |  **21.36 ns** |  **20.45 ns** |  **1.00** |    **0.02** |   **1,700 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | 4       |  20.97 ns | 0.426 ns | 0.597 ns |  20.44 ns |  23.56 ns |  21.45 ns |  1.03 |    0.03 |   2,126 B |         - |          NA |
| SampledHashTable          | 4       |  11.89 ns | 0.043 ns | 0.061 ns |  11.77 ns |  12.00 ns |  11.99 ns |  0.59 |    0.01 |     693 B |         - |          NA |
|                           |         |           |          |          |           |           |           |       |         |           |           |             |
| **DictionaryAlternateLookup** | **16**      |  **79.29 ns** | **1.351 ns** | **1.938 ns** |  **77.27 ns** |  **84.70 ns** |  **82.25 ns** |  **1.00** |    **0.03** |   **1,752 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | 16      |  71.21 ns | 0.439 ns | 0.586 ns |  70.26 ns |  72.67 ns |  72.06 ns |  0.90 |    0.02 |   2,177 B |         - |          NA |
| SampledHashTable          | 16      |  47.42 ns | 0.185 ns | 0.277 ns |  46.90 ns |  48.10 ns |  47.76 ns |  0.60 |    0.01 |     706 B |         - |          NA |
|                           |         |           |          |          |           |           |           |       |         |           |           |             |
| **DictionaryAlternateLookup** | **32**      | **156.09 ns** | **0.464 ns** | **0.636 ns** | **154.96 ns** | **157.44 ns** | **156.90 ns** |  **1.00** |    **0.01** |   **1,757 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | 32      | 139.09 ns | 0.512 ns | 0.718 ns | 137.81 ns | 140.81 ns | 139.81 ns |  0.89 |    0.01 |   2,136 B |         - |          NA |
| SampledHashTable          | 32      | 117.18 ns | 5.652 ns | 8.459 ns | 111.33 ns | 140.87 ns | 128.63 ns |  0.75 |    0.05 |     694 B |         - |          NA |
