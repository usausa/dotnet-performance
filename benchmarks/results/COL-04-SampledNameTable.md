# BIT-01 / COL-04: SampledNameTable

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. String key: 0.62x / 0.61x / 0.75x of Dictionary at 4 / 16 / 32 names on both generations
- Span key on net11: 0.57x / 0.59x / 0.69x of Dictionary AlternateLookup and ahead of Frozen AlternateLookup at every size (net10 0.54x / 0.57x / 0.70x)
- Linear scan got 0.83-0.90x faster on net11 and now ties the table at 4 names (12.13 vs 12.10 ns); it still degrades to 1.53x / 3.40x at 16 / 32

### SampledNameTableBenchmark

net10 → net11 per row:

| Method | Columns | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| DictionaryLookup | 4 | 19.95 ns | 19.63 ns | 0.98 | - → - | 1,216 B → 1,180 B |
| LinearScan | 4 | 14.63 ns | 12.13 ns | 0.83 | - → - | 641 B → 637 B |
| SampledHashTable | 4 | 12.37 ns | 12.10 ns | 0.98 | - → - | 766 B → 742 B |
| DictionaryLookup | 16 | 77.54 ns | 75.78 ns | 0.98 | - → - | 1,210 B → 1,194 B |
| LinearScan | 16 | 129.83 ns | 116.15 ns | 0.89 | - → - | 641 B → 628 B |
| SampledHashTable | 16 | 47.66 ns | 46.41 ns | 0.97 | - → - | 763 B → 741 B |
| DictionaryLookup | 32 | 150.36 ns | 146.70 ns | 0.98 | - → - | 1,198 B → 1,182 B |
| LinearScan | 32 | 552.56 ns | 498.55 ns | 0.90 | - → - | 656 B → 637 B |
| SampledHashTable | 32 | 112.69 ns | 109.73 ns | 0.97 | - → - | 770 B → 758 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.32 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Columns | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |-------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **DictionaryLookup** | **.NET 10.0**           | **.NET 10.0** | **4**       |  **19.95 ns** | **0.065 ns** | **0.096 ns** |  **19.81 ns** |  **20.15 ns** |  **20.06 ns** |  **1.00** |    **0.00** |   **1,216 B** |         **-** |          **NA** |
| LinearScan       | .NET 10.0           | .NET 10.0 | 4       |  14.63 ns | 0.551 ns | 0.824 ns |  13.45 ns |  15.64 ns |  15.53 ns |  0.73 |    0.04 |     641 B |         - |          NA |
| SampledHashTable | .NET 10.0           | .NET 10.0 | 4       |  12.37 ns | 0.078 ns | 0.106 ns |  12.22 ns |  12.51 ns |  12.48 ns |  0.62 |    0.01 |     766 B |         - |          NA |
|                  |                     |           |         |           |          |          |           |           |           |       |         |           |           |             |
| DictionaryLookup | MediumRun-.NET 11.0 | .NET 11.0 | 4       |  19.63 ns | 0.058 ns | 0.084 ns |  19.49 ns |  19.79 ns |  19.72 ns |  1.00 |    0.00 |   1,180 B |         - |          NA |
| LinearScan       | MediumRun-.NET 11.0 | .NET 11.0 | 4       |  12.13 ns | 0.047 ns | 0.069 ns |  11.99 ns |  12.23 ns |  12.20 ns |  0.62 |    0.00 |     637 B |         - |          NA |
| SampledHashTable | MediumRun-.NET 11.0 | .NET 11.0 | 4       |  12.10 ns | 0.064 ns | 0.091 ns |  11.95 ns |  12.22 ns |  12.20 ns |  0.62 |    0.01 |     742 B |         - |          NA |
|                  |                     |           |         |           |          |          |           |           |           |       |         |           |           |             |
| **DictionaryLookup** | **.NET 10.0**           | **.NET 10.0** | **16**      |  **77.54 ns** | **0.193 ns** | **0.264 ns** |  **76.95 ns** |  **78.17 ns** |  **77.83 ns** |  **1.00** |    **0.00** |   **1,210 B** |         **-** |          **NA** |
| LinearScan       | .NET 10.0           | .NET 10.0 | 16      | 129.83 ns | 0.576 ns | 0.807 ns | 128.29 ns | 131.40 ns | 130.82 ns |  1.67 |    0.01 |     641 B |         - |          NA |
| SampledHashTable | .NET 10.0           | .NET 10.0 | 16      |  47.66 ns | 0.188 ns | 0.269 ns |  47.24 ns |  48.15 ns |  47.98 ns |  0.61 |    0.00 |     763 B |         - |          NA |
|                  |                     |           |         |           |          |          |           |           |           |       |         |           |           |             |
| DictionaryLookup | MediumRun-.NET 11.0 | .NET 11.0 | 16      |  75.78 ns | 0.193 ns | 0.277 ns |  75.19 ns |  76.44 ns |  76.11 ns |  1.00 |    0.00 |   1,194 B |         - |          NA |
| LinearScan       | MediumRun-.NET 11.0 | .NET 11.0 | 16      | 116.15 ns | 0.364 ns | 0.545 ns | 114.98 ns | 117.12 ns | 116.78 ns |  1.53 |    0.01 |     628 B |         - |          NA |
| SampledHashTable | MediumRun-.NET 11.0 | .NET 11.0 | 16      |  46.41 ns | 0.117 ns | 0.168 ns |  46.14 ns |  46.75 ns |  46.63 ns |  0.61 |    0.00 |     741 B |         - |          NA |
|                  |                     |           |         |           |          |          |           |           |           |       |         |           |           |             |
| **DictionaryLookup** | **.NET 10.0**           | **.NET 10.0** | **32**      | **150.36 ns** | **0.574 ns** | **0.786 ns** | **148.97 ns** | **152.47 ns** | **151.27 ns** |  **1.00** |    **0.00** |   **1,198 B** |         **-** |          **NA** |
| LinearScan       | .NET 10.0           | .NET 10.0 | 32      | 552.56 ns | 2.336 ns | 3.423 ns | 546.76 ns | 560.41 ns | 556.71 ns |  3.67 |    0.03 |     656 B |         - |          NA |
| SampledHashTable | .NET 10.0           | .NET 10.0 | 32      | 112.69 ns | 0.261 ns | 0.366 ns | 112.11 ns | 113.64 ns | 113.05 ns |  0.75 |    0.00 |     770 B |         - |          NA |
|                  |                     |           |         |           |          |          |           |           |           |       |         |           |           |             |
| DictionaryLookup | MediumRun-.NET 11.0 | .NET 11.0 | 32      | 146.70 ns | 0.534 ns | 0.749 ns | 145.59 ns | 148.41 ns | 147.65 ns |  1.00 |    0.00 |   1,182 B |         - |          NA |
| LinearScan       | MediumRun-.NET 11.0 | .NET 11.0 | 32      | 498.55 ns | 4.133 ns | 6.186 ns | 486.62 ns | 509.32 ns | 506.13 ns |  3.40 |    0.04 |     637 B |         - |          NA |
| SampledHashTable | MediumRun-.NET 11.0 | .NET 11.0 | 32      | 109.73 ns | 0.891 ns | 1.306 ns | 107.77 ns | 113.32 ns | 111.47 ns |  0.75 |    0.01 |     758 B |         - |          NA |

### SampledNameTableSpanKeyBenchmark

net10 → net11 per row:

| Method | Columns | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| DictionaryAlternateLookup | 4 | 23.52 ns | 20.64 ns | 0.88 | - → - | 1,482 B → 1,210 B |
| FrozenAlternateLookup | 4 | 22.88 ns | 21.03 ns | 0.92 | - → - | 1,968 B → 1,143 B |
| SampledHashTable | 4 | 12.48 ns | 11.82 ns | 0.95 | - → - | 779 B → 751 B |
| DictionaryAlternateLookup | 16 | 84.40 ns | 79.26 ns | 0.94 | - → - | 1,466 B → 1,210 B |
| FrozenAlternateLookup | 16 | 72.58 ns | 74.21 ns | 1.02 | - → - | 2,034 B → 2,019 B |
| SampledHashTable | 16 | 48.09 ns | 46.61 ns | 0.97 | - → - | 779 B → 753 B |
| DictionaryAlternateLookup | 32 | 160.30 ns | 158.60 ns | 0.99 | - → - | 1,239 B → 1,221 B |
| FrozenAlternateLookup | 32 | 139.48 ns | 140.44 ns | 1.01 | - → - | 2,031 B → 2,016 B |
| SampledHashTable | 32 | 112.01 ns | 109.61 ns | 0.98 | - → - | 789 B → 772 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.52 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                    | Job                 | Runtime   | Columns | Mean      | Error    | StdDev   | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------------- |-------------------- |---------- |-------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **DictionaryAlternateLookup** | **.NET 10.0**           | **.NET 10.0** | **4**       |  **23.52 ns** | **1.824 ns** | **2.617 ns** |  **21.26 ns** |  **21.01 ns** |  **26.51 ns** |  **26.35 ns** |  **1.00** |    **0.00** |   **1,482 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | .NET 10.0           | .NET 10.0 | 4       |  22.88 ns | 0.090 ns | 0.132 ns |  22.92 ns |  22.61 ns |  23.15 ns |  23.04 ns |  0.98 |    0.11 |   1,968 B |         - |          NA |
| SampledHashTable          | .NET 10.0           | .NET 10.0 | 4       |  12.48 ns | 0.024 ns | 0.034 ns |  12.48 ns |  12.43 ns |  12.54 ns |  12.52 ns |  0.54 |    0.06 |     779 B |         - |          NA |
|                           |                     |           |         |           |          |          |           |           |           |           |       |         |           |           |             |
| DictionaryAlternateLookup | MediumRun-.NET 11.0 | .NET 11.0 | 4       |  20.64 ns | 0.095 ns | 0.136 ns |  20.63 ns |  20.44 ns |  20.94 ns |  20.81 ns |  1.00 |    0.00 |   1,210 B |         - |          NA |
| FrozenAlternateLookup     | MediumRun-.NET 11.0 | .NET 11.0 | 4       |  21.03 ns | 0.140 ns | 0.209 ns |  21.05 ns |  20.70 ns |  21.39 ns |  21.28 ns |  1.02 |    0.01 |   1,143 B |         - |          NA |
| SampledHashTable          | MediumRun-.NET 11.0 | .NET 11.0 | 4       |  11.82 ns | 0.051 ns | 0.075 ns |  11.81 ns |  11.69 ns |  11.99 ns |  11.92 ns |  0.57 |    0.01 |     751 B |         - |          NA |
|                           |                     |           |         |           |          |          |           |           |           |           |       |         |           |           |             |
| **DictionaryAlternateLookup** | **.NET 10.0**           | **.NET 10.0** | **16**      |  **84.40 ns** | **1.050 ns** | **1.539 ns** |  **84.91 ns** |  **82.03 ns** |  **86.41 ns** |  **86.08 ns** |  **1.00** |    **0.00** |   **1,466 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | .NET 10.0           | .NET 10.0 | 16      |  72.58 ns | 0.832 ns | 1.219 ns |  73.10 ns |  70.71 ns |  74.39 ns |  73.91 ns |  0.86 |    0.02 |   2,034 B |         - |          NA |
| SampledHashTable          | .NET 10.0           | .NET 10.0 | 16      |  48.09 ns | 0.288 ns | 0.423 ns |  48.08 ns |  47.45 ns |  48.60 ns |  48.58 ns |  0.57 |    0.01 |     779 B |         - |          NA |
|                           |                     |           |         |           |          |          |           |           |           |           |       |         |           |           |             |
| DictionaryAlternateLookup | MediumRun-.NET 11.0 | .NET 11.0 | 16      |  79.26 ns | 0.268 ns | 0.384 ns |  79.30 ns |  78.49 ns |  80.02 ns |  79.74 ns |  1.00 |    0.00 |   1,210 B |         - |          NA |
| FrozenAlternateLookup     | MediumRun-.NET 11.0 | .NET 11.0 | 16      |  74.21 ns | 0.589 ns | 0.845 ns |  74.23 ns |  72.99 ns |  75.44 ns |  75.13 ns |  0.94 |    0.01 |   2,019 B |         - |          NA |
| SampledHashTable          | MediumRun-.NET 11.0 | .NET 11.0 | 16      |  46.61 ns | 0.080 ns | 0.115 ns |  46.60 ns |  46.40 ns |  46.84 ns |  46.75 ns |  0.59 |    0.00 |     753 B |         - |          NA |
|                           |                     |           |         |           |          |          |           |           |           |           |       |         |           |           |             |
| **DictionaryAlternateLookup** | **.NET 10.0**           | **.NET 10.0** | **32**      | **160.30 ns** | **0.831 ns** | **1.243 ns** | **160.32 ns** | **158.58 ns** | **162.62 ns** | **162.08 ns** |  **1.00** |    **0.00** |   **1,239 B** |         **-** |          **NA** |
| FrozenAlternateLookup     | .NET 10.0           | .NET 10.0 | 32      | 139.48 ns | 0.505 ns | 0.724 ns | 139.57 ns | 137.96 ns | 140.42 ns | 140.32 ns |  0.87 |    0.01 |   2,031 B |         - |          NA |
| SampledHashTable          | .NET 10.0           | .NET 10.0 | 32      | 112.01 ns | 0.284 ns | 0.388 ns | 111.90 ns | 111.40 ns | 112.90 ns | 112.61 ns |  0.70 |    0.01 |     789 B |         - |          NA |
|                           |                     |           |         |           |          |          |           |           |           |           |       |         |           |           |             |
| DictionaryAlternateLookup | MediumRun-.NET 11.0 | .NET 11.0 | 32      | 158.60 ns | 1.377 ns | 1.931 ns | 157.94 ns | 156.81 ns | 164.94 ns | 160.64 ns |  1.00 |    0.00 |   1,221 B |         - |          NA |
| FrozenAlternateLookup     | MediumRun-.NET 11.0 | .NET 11.0 | 32      | 140.44 ns | 0.798 ns | 1.144 ns | 140.39 ns | 138.21 ns | 142.52 ns | 141.91 ns |  0.89 |    0.01 |   2,016 B |         - |          NA |
| SampledHashTable          | MediumRun-.NET 11.0 | .NET 11.0 | 32      | 109.61 ns | 0.621 ns | 0.910 ns | 109.55 ns | 106.98 ns | 111.21 ns | 110.90 ns |  0.69 |    0.01 |     772 B |         - |          NA |

## Earlier record

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
