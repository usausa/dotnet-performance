# TXT-10: Aggregating known-string matching into a switch

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged for string input - adopted with a size window. 16 keys on net11: switch 0.58x hit / 0.34x miss of the Equals chain; 128 keys still collapses (span switch 1.47x hit / 4.40x miss of the sampling switch)
- **Span input changed on net11:** the span Equals chain got 0.54x / 0.43x faster at 16 keys (2.73 → 1.49 ns hit, 4.94 → 2.12 ns miss), so it now beats the span switch on hit (1.49 vs 1.91 ns) and trails it on miss (2.12 vs 1.94 ns)
- At <=4 keys on span input the chain and the switch now compile to the same size (165 B hit / 157 B miss, from 249-259 B) and run within 4-12% of each other; the net10 advantage of the chain is gone
- 64 keys: the hand-written sampling switch is 1.17-1.19x slower on net11, so the plain span switch is 0.80x hit / 0.75x miss of it (1.05x / 0.98x on net10). The 64-key upper bound of the window holds with more margin

### StringSwitchSmallBenchmark

net10 → net11 per row:

| Method | Probe | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| EqualsChain | Hit | 1.2742 ns | 1.1742 ns | 0.92 | - → - | 263 B → 262 B |
| SpanEqualsChain | Hit | 1.1384 ns | 1.0713 ns | 0.94 | - → - | 249 B → 165 B |
| StringSwitch | Hit | 1.2727 ns | 1.1277 ns | 0.89 | - → - | 256 B → 258 B |
| SpanSwitch | Hit | 1.2505 ns | 1.0303 ns | 0.82 | - → - | 256 B → 165 B |
| EqualsChain | Miss | 1.7248 ns | 1.6359 ns | 0.95 | - → - | 256 B → 258 B |
| SpanEqualsChain | Miss | 1.6306 ns | 0.8863 ns | 0.54 | - → - | 259 B → 157 B |
| StringSwitch | Miss | 1.7514 ns | 1.6314 ns | 0.93 | - → - | 256 B → 258 B |
| SpanSwitch | Miss | 1.7498 ns | 0.7849 ns | 0.45 | - → - | 256 B → 157 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.29 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method          | Job                 | Runtime   | Probe | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **EqualsChain**     | **.NET 10.0**           | **.NET 10.0** | **Hit**   | **1.2742 ns** | **0.0148 ns** | **0.0217 ns** | **1.2766 ns** | **1.2389 ns** | **1.3186 ns** | **1.2962 ns** |  **1.00** |    **0.00** |     **263 B** |         **-** |          **NA** |
| SpanEqualsChain | .NET 10.0           | .NET 10.0 | Hit   | 1.1384 ns | 0.0080 ns | 0.0117 ns | 1.1354 ns | 1.1165 ns | 1.1569 ns | 1.1519 ns |  0.89 |    0.02 |     249 B |         - |          NA |
| StringSwitch    | .NET 10.0           | .NET 10.0 | Hit   | 1.2727 ns | 0.0161 ns | 0.0241 ns | 1.2715 ns | 1.2303 ns | 1.3235 ns | 1.3083 ns |  1.00 |    0.03 |     256 B |         - |          NA |
| SpanSwitch      | .NET 10.0           | .NET 10.0 | Hit   | 1.2505 ns | 0.0355 ns | 0.0509 ns | 1.2547 ns | 1.1652 ns | 1.3362 ns | 1.3123 ns |  0.98 |    0.04 |     256 B |         - |          NA |
|                 |                     |           |       |           |           |           |           |           |           |           |       |         |           |           |             |
| EqualsChain     | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.1742 ns | 0.0138 ns | 0.0198 ns | 1.1747 ns | 1.1349 ns | 1.2161 ns | 1.1949 ns |  1.00 |    0.00 |     262 B |         - |          NA |
| SpanEqualsChain | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.0713 ns | 0.0156 ns | 0.0234 ns | 1.0715 ns | 1.0226 ns | 1.1310 ns | 1.1042 ns |  0.91 |    0.02 |     165 B |         - |          NA |
| StringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.1277 ns | 0.0132 ns | 0.0198 ns | 1.1282 ns | 1.0908 ns | 1.1674 ns | 1.1528 ns |  0.96 |    0.02 |     258 B |         - |          NA |
| SpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.0303 ns | 0.0588 ns | 0.0880 ns | 1.0281 ns | 0.9046 ns | 1.1434 ns | 1.1252 ns |  0.88 |    0.08 |     165 B |         - |          NA |
|                 |                     |           |       |           |           |           |           |           |           |           |       |         |           |           |             |
| **EqualsChain**     | **.NET 10.0**           | **.NET 10.0** | **Miss**  | **1.7248 ns** | **0.0046 ns** | **0.0066 ns** | **1.7247 ns** | **1.7136 ns** | **1.7383 ns** | **1.7337 ns** |  **1.00** |    **0.00** |     **256 B** |         **-** |          **NA** |
| SpanEqualsChain | .NET 10.0           | .NET 10.0 | Miss  | 1.6306 ns | 0.1343 ns | 0.1926 ns | 1.4761 ns | 1.4404 ns | 1.8503 ns | 1.8382 ns |  0.95 |    0.11 |     259 B |         - |          NA |
| StringSwitch    | .NET 10.0           | .NET 10.0 | Miss  | 1.7514 ns | 0.0203 ns | 0.0285 ns | 1.7545 ns | 1.7184 ns | 1.8145 ns | 1.7951 ns |  1.02 |    0.02 |     256 B |         - |          NA |
| SpanSwitch      | .NET 10.0           | .NET 10.0 | Miss  | 1.7498 ns | 0.1412 ns | 0.2114 ns | 1.7109 ns | 1.5174 ns | 2.0249 ns | 2.0115 ns |  1.01 |    0.12 |     256 B |         - |          NA |
|                 |                     |           |       |           |           |           |           |           |           |           |       |         |           |           |             |
| EqualsChain     | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 1.6359 ns | 0.0043 ns | 0.0063 ns | 1.6358 ns | 1.6253 ns | 1.6504 ns | 1.6432 ns |  1.00 |    0.00 |     258 B |         - |          NA |
| SpanEqualsChain | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 0.8863 ns | 0.0651 ns | 0.0913 ns | 0.9550 ns | 0.7776 ns | 0.9847 ns | 0.9722 ns |  0.54 |    0.05 |     157 B |         - |          NA |
| StringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 1.6314 ns | 0.0054 ns | 0.0081 ns | 1.6303 ns | 1.6170 ns | 1.6563 ns | 1.6415 ns |  1.00 |    0.01 |     258 B |         - |          NA |
| SpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 0.7849 ns | 0.0044 ns | 0.0064 ns | 0.7851 ns | 0.7728 ns | 0.7958 ns | 0.7934 ns |  0.48 |    0.00 |     157 B |         - |          NA |

### StringSwitchBenchmark

net10 → net11 per row:

| Method | Probe | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| EqualsChain | Hit | 3.069 ns | 3.123 ns | 1.02 | - → - | 1,146 B → 1,152 B |
| SpanEqualsChain | Hit | 2.734 ns | 1.488 ns | 0.54 | - → - | 1,154 B → 885 B |
| StringSwitch | Hit | 1.824 ns | 1.806 ns | 0.99 | - → - | 1,158 B → 1,164 B |
| SpanSwitch | Hit | 1.925 ns | 1.906 ns | 0.99 | - → - | 1,171 B → 918 B |
| DictionaryLookup | Hit | 4.628 ns | 4.748 ns | 1.03 | - → - | 1,223 B → 1,185 B |
| SampledTable | Hit | 3.716 ns | 3.639 ns | 0.98 | - → - | 797 B → 748 B |
| EqualsChain | Miss | 5.525 ns | 5.455 ns | 0.99 | - → - | 1,146 B → 1,152 B |
| SpanEqualsChain | Miss | 4.942 ns | 2.123 ns | 0.43 | - → - | 1,178 B → 865 B |
| StringSwitch | Miss | 1.926 ns | 1.844 ns | 0.96 | - → - | 1,164 B → 1,170 B |
| SpanSwitch | Miss | 1.953 ns | 1.943 ns | 0.99 | - → - | 1,177 B → 949 B |
| DictionaryLookup | Miss | 3.058 ns | 2.936 ns | 0.96 | - → - | 519 B → 476 B |
| SampledTable | Miss | 4.440 ns | 4.366 ns | 0.98 | - → - | 807 B → 761 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.08 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Probe | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **EqualsChain**      | **.NET 10.0**           | **.NET 10.0** | **Hit**   | **3.069 ns** | **0.0134 ns** | **0.0193 ns** | **3.065 ns** | **3.032 ns** | **3.117 ns** | **3.092 ns** |  **1.00** |    **0.00** |   **1,146 B** |         **-** |          **NA** |
| SpanEqualsChain  | .NET 10.0           | .NET 10.0 | Hit   | 2.734 ns | 0.0075 ns | 0.0107 ns | 2.734 ns | 2.719 ns | 2.762 ns | 2.746 ns |  0.89 |    0.01 |   1,154 B |         - |          NA |
| StringSwitch     | .NET 10.0           | .NET 10.0 | Hit   | 1.824 ns | 0.0501 ns | 0.0719 ns | 1.828 ns | 1.734 ns | 1.900 ns | 1.899 ns |  0.59 |    0.02 |   1,158 B |         - |          NA |
| SpanSwitch       | .NET 10.0           | .NET 10.0 | Hit   | 1.925 ns | 0.0100 ns | 0.0147 ns | 1.922 ns | 1.899 ns | 1.959 ns | 1.945 ns |  0.63 |    0.01 |   1,171 B |         - |          NA |
| DictionaryLookup | .NET 10.0           | .NET 10.0 | Hit   | 4.628 ns | 0.0402 ns | 0.0602 ns | 4.611 ns | 4.559 ns | 4.828 ns | 4.713 ns |  1.51 |    0.02 |   1,223 B |         - |          NA |
| SampledTable     | .NET 10.0           | .NET 10.0 | Hit   | 3.716 ns | 0.0102 ns | 0.0144 ns | 3.714 ns | 3.697 ns | 3.753 ns | 3.732 ns |  1.21 |    0.01 |     797 B |         - |          NA |
|                  |                     |           |       |          |           |           |          |          |          |          |       |         |           |           |             |
| EqualsChain      | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 3.123 ns | 0.0116 ns | 0.0173 ns | 3.119 ns | 3.096 ns | 3.164 ns | 3.153 ns |  1.00 |    0.00 |   1,152 B |         - |          NA |
| SpanEqualsChain  | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.488 ns | 0.0316 ns | 0.0453 ns | 1.486 ns | 1.423 ns | 1.558 ns | 1.542 ns |  0.48 |    0.01 |     885 B |         - |          NA |
| StringSwitch     | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.806 ns | 0.0542 ns | 0.0777 ns | 1.756 ns | 1.717 ns | 1.939 ns | 1.902 ns |  0.58 |    0.02 |   1,164 B |         - |          NA |
| SpanSwitch       | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.906 ns | 0.0060 ns | 0.0086 ns | 1.906 ns | 1.891 ns | 1.923 ns | 1.916 ns |  0.61 |    0.00 |     918 B |         - |          NA |
| DictionaryLookup | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 4.748 ns | 0.1024 ns | 0.1469 ns | 4.695 ns | 4.549 ns | 5.131 ns | 4.934 ns |  1.52 |    0.05 |   1,185 B |         - |          NA |
| SampledTable     | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 3.639 ns | 0.1054 ns | 0.1478 ns | 3.596 ns | 3.497 ns | 4.004 ns | 3.831 ns |  1.16 |    0.05 |     748 B |         - |          NA |
|                  |                     |           |       |          |           |           |          |          |          |          |       |         |           |           |             |
| **EqualsChain**      | **.NET 10.0**           | **.NET 10.0** | **Miss**  | **5.525 ns** | **0.0639 ns** | **0.0917 ns** | **5.546 ns** | **5.359 ns** | **5.674 ns** | **5.624 ns** |  **1.00** |    **0.00** |   **1,146 B** |         **-** |          **NA** |
| SpanEqualsChain  | .NET 10.0           | .NET 10.0 | Miss  | 4.942 ns | 0.1593 ns | 0.2385 ns | 4.865 ns | 4.738 ns | 5.496 ns | 5.415 ns |  0.89 |    0.04 |   1,178 B |         - |          NA |
| StringSwitch     | .NET 10.0           | .NET 10.0 | Miss  | 1.926 ns | 0.0087 ns | 0.0130 ns | 1.924 ns | 1.905 ns | 1.957 ns | 1.941 ns |  0.35 |    0.01 |   1,164 B |         - |          NA |
| SpanSwitch       | .NET 10.0           | .NET 10.0 | Miss  | 1.953 ns | 0.0073 ns | 0.0110 ns | 1.954 ns | 1.932 ns | 1.977 ns | 1.967 ns |  0.35 |    0.01 |   1,177 B |         - |          NA |
| DictionaryLookup | .NET 10.0           | .NET 10.0 | Miss  | 3.058 ns | 0.0123 ns | 0.0176 ns | 3.059 ns | 3.030 ns | 3.092 ns | 3.079 ns |  0.55 |    0.01 |     519 B |         - |          NA |
| SampledTable     | .NET 10.0           | .NET 10.0 | Miss  | 4.440 ns | 0.0128 ns | 0.0187 ns | 4.438 ns | 4.412 ns | 4.482 ns | 4.467 ns |  0.80 |    0.01 |     807 B |         - |          NA |
|                  |                     |           |       |          |           |           |          |          |          |          |       |         |           |           |             |
| EqualsChain      | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 5.455 ns | 0.0513 ns | 0.0752 ns | 5.466 ns | 5.358 ns | 5.601 ns | 5.555 ns |  1.00 |    0.00 |   1,152 B |         - |          NA |
| SpanEqualsChain  | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 2.123 ns | 0.0625 ns | 0.0896 ns | 2.188 ns | 2.007 ns | 2.221 ns | 2.217 ns |  0.39 |    0.02 |     865 B |         - |          NA |
| StringSwitch     | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 1.844 ns | 0.0470 ns | 0.0659 ns | 1.868 ns | 1.739 ns | 1.976 ns | 1.899 ns |  0.34 |    0.01 |   1,170 B |         - |          NA |
| SpanSwitch       | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 1.943 ns | 0.0158 ns | 0.0227 ns | 1.941 ns | 1.911 ns | 2.000 ns | 1.979 ns |  0.36 |    0.01 |     949 B |         - |          NA |
| DictionaryLookup | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 2.936 ns | 0.0716 ns | 0.1027 ns | 2.900 ns | 2.848 ns | 3.253 ns | 3.032 ns |  0.54 |    0.02 |     476 B |         - |          NA |
| SampledTable     | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 4.366 ns | 0.0324 ns | 0.0486 ns | 4.366 ns | 4.278 ns | 4.450 ns | 4.424 ns |  0.80 |    0.01 |     761 B |         - |          NA |

### StringSwitchLongKeyBenchmark

net10 → net11 per row:

| Method | Probe | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| SampledSwitch | Hit | 1.920 ns | 1.894 ns | 0.99 | - → - | 2,022 B → 1,757 B |
| SpanSwitch | Hit | 2.147 ns | 2.073 ns | 0.97 | - → - | 1,651 B → 1,417 B |
| SampledTable | Hit | 3.508 ns | 3.443 ns | 0.98 | - → - | 793 B → 751 B |
| SampledSwitch | Miss | 1.918 ns | 1.749 ns | 0.91 | - → - | 2,032 B → 1,842 B |
| SpanSwitch | Miss | 2.067 ns | 1.980 ns | 0.96 | - → - | 1,640 B → 1,484 B |
| SampledTable | Miss | 3.161 ns | 2.850 ns | 0.90 | - → - | 779 B → 752 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.86 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Probe | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **.NET 10.0**           | **.NET 10.0** | **Hit**   | **1.920 ns** | **0.0481 ns** | **0.0705 ns** | **1.855 ns** | **2.120 ns** | **2.019 ns** |  **1.00** |    **0.00** |   **2,022 B** |         **-** |          **NA** |
| SpanSwitch    | .NET 10.0           | .NET 10.0 | Hit   | 2.147 ns | 0.0965 ns | 0.1444 ns | 2.034 ns | 2.617 ns | 2.353 ns |  1.12 |    0.08 |   1,651 B |         - |          NA |
| SampledTable  | .NET 10.0           | .NET 10.0 | Hit   | 3.508 ns | 0.0405 ns | 0.0568 ns | 3.404 ns | 3.585 ns | 3.573 ns |  1.83 |    0.07 |     793 B |         - |          NA |
|               |                     |           |       |          |           |           |          |          |          |       |         |           |           |             |
| SampledSwitch | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 1.894 ns | 0.0715 ns | 0.1047 ns | 1.781 ns | 2.191 ns | 2.042 ns |  1.00 |    0.00 |   1,757 B |         - |          NA |
| SpanSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 2.073 ns | 0.0362 ns | 0.0542 ns | 1.996 ns | 2.191 ns | 2.145 ns |  1.10 |    0.06 |   1,417 B |         - |          NA |
| SampledTable  | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 3.443 ns | 0.0573 ns | 0.0840 ns | 3.324 ns | 3.683 ns | 3.549 ns |  1.82 |    0.10 |     751 B |         - |          NA |
|               |                     |           |       |          |           |           |          |          |          |       |         |           |           |             |
| **SampledSwitch** | **.NET 10.0**           | **.NET 10.0** | **Miss**  | **1.918 ns** | **0.0177 ns** | **0.0260 ns** | **1.881 ns** | **1.979 ns** | **1.952 ns** |  **1.00** |    **0.00** |   **2,032 B** |         **-** |          **NA** |
| SpanSwitch    | .NET 10.0           | .NET 10.0 | Miss  | 2.067 ns | 0.0118 ns | 0.0169 ns | 2.035 ns | 2.101 ns | 2.087 ns |  1.08 |    0.02 |   1,640 B |         - |          NA |
| SampledTable  | .NET 10.0           | .NET 10.0 | Miss  | 3.161 ns | 0.0271 ns | 0.0397 ns | 3.100 ns | 3.227 ns | 3.204 ns |  1.65 |    0.03 |     779 B |         - |          NA |
|               |                     |           |       |          |           |           |          |          |          |       |         |           |           |             |
| SampledSwitch | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 1.749 ns | 0.0212 ns | 0.0311 ns | 1.694 ns | 1.826 ns | 1.779 ns |  1.00 |    0.00 |   1,842 B |         - |          NA |
| SpanSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 1.980 ns | 0.0093 ns | 0.0139 ns | 1.954 ns | 2.006 ns | 1.995 ns |  1.13 |    0.02 |   1,484 B |         - |          NA |
| SampledTable  | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 2.850 ns | 0.0099 ns | 0.0135 ns | 2.825 ns | 2.875 ns | 2.865 ns |  1.63 |    0.03 |     752 B |         - |          NA |

### StringSwitch64Benchmark

net10 → net11 per row:

| Method | Probe | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| SampledSwitch | Hit | 2.539 ns | 2.981 ns | 1.17 | - → - | 5,821 B → 4,770 B |
| SpanSwitch | Hit | 2.657 ns | 2.376 ns | 0.89 | - → - | 4,903 B → 3,263 B |
| SampledTable | Hit | 3.482 ns | 3.285 ns | 0.94 | - → - | 797 B → 755 B |
| SampledSwitch | Miss | 2.328 ns | 2.766 ns | 1.19 | - → - | 5,963 B → 4,801 B |
| SpanSwitch | Miss | 2.277 ns | 2.085 ns | 0.92 | - → - | 5,024 B → 3,596 B |
| SampledTable | Miss | 4.020 ns | 3.877 ns | 0.96 | - → - | 778 B → 742 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.75 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Probe | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **.NET 10.0**           | **.NET 10.0** | **Hit**   | **2.539 ns** | **0.0803 ns** | **0.1177 ns** | **2.601 ns** | **2.408 ns** | **2.719 ns** | **2.670 ns** |  **1.00** |    **0.00** |   **5,821 B** |         **-** |          **NA** |
| SpanSwitch    | .NET 10.0           | .NET 10.0 | Hit   | 2.657 ns | 0.0102 ns | 0.0149 ns | 2.655 ns | 2.623 ns | 2.696 ns | 2.672 ns |  1.05 |    0.05 |   4,903 B |         - |          NA |
| SampledTable  | .NET 10.0           | .NET 10.0 | Hit   | 3.482 ns | 0.0151 ns | 0.0221 ns | 3.479 ns | 3.443 ns | 3.545 ns | 3.508 ns |  1.37 |    0.06 |     797 B |         - |          NA |
|               |                     |           |       |          |           |           |          |          |          |          |       |         |           |           |             |
| SampledSwitch | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 2.981 ns | 0.0825 ns | 0.1184 ns | 2.998 ns | 2.757 ns | 3.163 ns | 3.131 ns |  1.00 |    0.00 |   4,770 B |         - |          NA |
| SpanSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 2.376 ns | 0.0099 ns | 0.0149 ns | 2.377 ns | 2.345 ns | 2.407 ns | 2.396 ns |  0.80 |    0.03 |   3,263 B |         - |          NA |
| SampledTable  | MediumRun-.NET 11.0 | .NET 11.0 | Hit   | 3.285 ns | 0.0153 ns | 0.0220 ns | 3.279 ns | 3.260 ns | 3.339 ns | 3.314 ns |  1.10 |    0.04 |     755 B |         - |          NA |
|               |                     |           |       |          |           |           |          |          |          |          |       |         |           |           |             |
| **SampledSwitch** | **.NET 10.0**           | **.NET 10.0** | **Miss**  | **2.328 ns** | **0.0220 ns** | **0.0329 ns** | **2.327 ns** | **2.271 ns** | **2.398 ns** | **2.369 ns** |  **1.00** |    **0.00** |   **5,963 B** |         **-** |          **NA** |
| SpanSwitch    | .NET 10.0           | .NET 10.0 | Miss  | 2.277 ns | 0.0207 ns | 0.0303 ns | 2.274 ns | 2.234 ns | 2.337 ns | 2.316 ns |  0.98 |    0.02 |   5,024 B |         - |          NA |
| SampledTable  | .NET 10.0           | .NET 10.0 | Miss  | 4.020 ns | 0.0108 ns | 0.0151 ns | 4.020 ns | 3.988 ns | 4.051 ns | 4.036 ns |  1.73 |    0.02 |     778 B |         - |          NA |
|               |                     |           |       |          |           |           |          |          |          |          |       |         |           |           |             |
| SampledSwitch | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 2.766 ns | 0.0413 ns | 0.0618 ns | 2.770 ns | 2.654 ns | 2.913 ns | 2.839 ns |  1.00 |    0.00 |   4,801 B |         - |          NA |
| SpanSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 2.085 ns | 0.0111 ns | 0.0167 ns | 2.082 ns | 2.057 ns | 2.114 ns | 2.110 ns |  0.75 |    0.02 |   3,596 B |         - |          NA |
| SampledTable  | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 3.877 ns | 0.0178 ns | 0.0250 ns | 3.871 ns | 3.838 ns | 3.937 ns | 3.913 ns |  1.40 |    0.03 |     742 B |         - |          NA |

### StringSwitch128Benchmark

net10 → net11 per row:

| Method | Probe | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| SampledSwitch | Hit | 2.954 ns | 2.650 ns | 0.90 | - → - | 11,506 B → 9,421 B |
| SpanSwitch | Hit | 3.913 ns | 3.881 ns | 0.99 | - → - | 11,591 B → 9,517 B |
| SampledTable | Hit | 3.614 ns | 3.434 ns | 0.95 | - → - | 800 B → 754 B |
| SampledSwitch | Miss | 2.782 ns | 2.330 ns | 0.84 | - → - | 11,517 B → 9,484 B |
| SpanSwitch | Miss | 10.074 ns | 10.244 ns | 1.02 | - → - | 12,043 B → 12,947 B |
| SampledTable | Miss | 4.277 ns | 4.038 ns | 0.94 | - → - | 793 B → 738 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.51 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Probe | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **.NET 10.0**           | **.NET 10.0** | **Hit**   |  **2.954 ns** | **0.1982 ns** | **0.2967 ns** |  **2.615 ns** |  **3.315 ns** |  **3.273 ns** |  **1.00** |    **0.00** |  **11,506 B** |         **-** |          **NA** |
| SpanSwitch    | .NET 10.0           | .NET 10.0 | Hit   |  3.913 ns | 0.1136 ns | 0.1701 ns |  3.683 ns |  4.251 ns |  4.136 ns |  1.34 |    0.14 |  11,591 B |         - |          NA |
| SampledTable  | .NET 10.0           | .NET 10.0 | Hit   |  3.614 ns | 0.0097 ns | 0.0145 ns |  3.589 ns |  3.641 ns |  3.632 ns |  1.24 |    0.12 |     800 B |         - |          NA |
|               |                     |           |       |           |           |           |           |           |           |       |         |           |           |             |
| SampledSwitch | MediumRun-.NET 11.0 | .NET 11.0 | Hit   |  2.650 ns | 0.1343 ns | 0.2010 ns |  2.422 ns |  2.938 ns |  2.884 ns |  1.00 |    0.00 |   9,421 B |         - |          NA |
| SpanSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Hit   |  3.881 ns | 0.1355 ns | 0.1943 ns |  3.686 ns |  4.316 ns |  4.143 ns |  1.47 |    0.13 |   9,517 B |         - |          NA |
| SampledTable  | MediumRun-.NET 11.0 | .NET 11.0 | Hit   |  3.434 ns | 0.0222 ns | 0.0326 ns |  3.380 ns |  3.491 ns |  3.466 ns |  1.30 |    0.10 |     754 B |         - |          NA |
|               |                     |           |       |           |           |           |           |           |           |       |         |           |           |             |
| **SampledSwitch** | **.NET 10.0**           | **.NET 10.0** | **Miss**  |  **2.782 ns** | **0.0331 ns** | **0.0496 ns** |  **2.691 ns** |  **2.878 ns** |  **2.840 ns** |  **1.00** |    **0.00** |  **11,517 B** |         **-** |          **NA** |
| SpanSwitch    | .NET 10.0           | .NET 10.0 | Miss  | 10.074 ns | 0.0668 ns | 0.0979 ns |  9.899 ns | 10.272 ns | 10.212 ns |  3.62 |    0.07 |  12,043 B |         - |          NA |
| SampledTable  | .NET 10.0           | .NET 10.0 | Miss  |  4.277 ns | 0.0138 ns | 0.0197 ns |  4.251 ns |  4.324 ns |  4.306 ns |  1.54 |    0.03 |     793 B |         - |          NA |
|               |                     |           |       |           |           |           |           |           |           |       |         |           |           |             |
| SampledSwitch | MediumRun-.NET 11.0 | .NET 11.0 | Miss  |  2.330 ns | 0.0112 ns | 0.0164 ns |  2.289 ns |  2.357 ns |  2.350 ns |  1.00 |    0.00 |   9,484 B |         - |          NA |
| SpanSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | Miss  | 10.244 ns | 0.0508 ns | 0.0744 ns | 10.101 ns | 10.396 ns | 10.347 ns |  4.40 |    0.04 |  12,947 B |         - |          NA |
| SampledTable  | MediumRun-.NET 11.0 | .NET 11.0 | Miss  |  4.038 ns | 0.0293 ns | 0.0421 ns |  3.969 ns |  4.106 ns |  4.093 ns |  1.73 |    0.02 |     738 B |         - |          NA |

## Earlier record

- Verdict: **adopted with a size window** - the default for 5-64 keys, useless at <=4, and not usable above 64
- 16 keys: **0.51x hit / 0.33x miss** vs an Equals chain; also beats Dictionary(Ordinal) (1.38x) and SampledNameTable (1.09x) on hit
- 64 keys: level with a hand-written sampling-hash switch (1.06x hit, 1.00x miss - CIs overlap)
- 128 keys: **collapses to 1.73x hit / 3.79x miss** - Roslyn switches to an FNV-1a hash over every character plus a 191-node binary search tree
- <=4 keys: the compiler emits **the same instruction stream as a hand-written Equals chain** (60 instructions, 263 B). On span input the chain is actually faster (1.17x hit / 1.12x miss), so the existing "<=4 keys: chain" guidance stands
- Long keys do **not** cause the predicted reversal: at 16 keys of 58-62 characters Roslyn never hashes, so the plain switch stays within 1.10-1.12x
- Code size scales with the key set (1,185 B at 16 -> 11,611 B at 128) while SampledNameTable stays flat at ~730-780 B
- **The win is against a *plain* if-chain.** Where the input must be converted before it can be matched (case folding for DB column names and the like), normalization costs more than the dispatch saves - 4.5-7.8x slower at 8 columns. See "Where the switch does not fit"

## What Roslyn actually generates

The strategy is chosen by **how many keys land in each length bucket**, not by the key count alone. Only the 128-key set crosses into hashing, and that is the only shape that reads every character.

| Key set | Generated strategy | Scans every character |
|---|---|---|
| 4 | Length + character tests - **identical to the hand-written Equals chain** | no |
| 16 | Length + character tests | no |
| 64 | Length jump table -> per-bucket character jump table | no |
| 128 | **FNV-1a loop over every character** (`mov edx,811C9DC5`, `xor` / `imul 1000193`) + a 191-node binary search over the hashes | **yes** |
| 16 x 58-62 chars | Length jump table (5 buckets) + first character + reference check + AVX-512 `vmovups zmm0` compare | no |

The hand-written sampling-hash switch it is measured against is a flat ~14 instructions with no loop (`shl edx,16`, first `<<8`, middle `<<4`, last xor), so its cost is independent of both key count and key length.

## How the probes are built

Keys are ordinary PascalCase identifier names (column / property / enum-member shaped); the smaller sets are prefixes of the same 128-name master list. The sampling hash collides naturally on them (64 keys -> 63 distinct hashes, 128 -> 125, longest chain 2); nothing is tuned to favour a strategy.

A miss probe keeps the length and replaces the character at index `length/3` - a position the sampling hash never reads, since it samples first / middle / last. So **every miss lands in the same bucket and has to be rejected by the full compare**: the pessimistic case for the sampling forms and the neutral case for the compiler's own dispatch. The 128-key conclusion survives that handicap.

Numbers below are **per probe** (`OperationsPerInvoke` = key count).

## 4 keys (lower bound)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Probe | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **EqualsChain**     | **Hit**   | **1.0790 ns** | **0.0179 ns** | **0.0263 ns** | **1.0862 ns** | **1.0228 ns** | **1.1208 ns** | **1.1074 ns** |  **1.00** |    **0.03** |     **255 B** |         **-** |          **NA** |
| SpanEqualsChain | Hit   | 0.9358 ns | 0.0200 ns | 0.0280 ns | 0.9439 ns | 0.8820 ns | 0.9709 ns | 0.9661 ns |  0.87 |    0.03 |     251 B |         - |          NA |
| StringSwitch    | Hit   | 1.0480 ns | 0.0141 ns | 0.0206 ns | 1.0439 ns | 1.0109 ns | 1.0836 ns | 1.0782 ns |  0.97 |    0.03 |     263 B |         - |          NA |
| SpanSwitch      | Hit   | 1.0920 ns | 0.0551 ns | 0.0825 ns | 1.0685 ns | 1.0043 ns | 1.2855 ns | 1.2469 ns |  1.01 |    0.08 |     252 B |         - |          NA |
|                 |       |           |           |           |           |           |           |           |       |         |           |           |             |
| **EqualsChain**     | **Miss**  | **1.5144 ns** | **0.0427 ns** | **0.0599 ns** | **1.5201 ns** | **1.4463 ns** | **1.6447 ns** | **1.5868 ns** |  **1.00** |    **0.05** |     **259 B** |         **-** |          **NA** |
| SpanEqualsChain | Miss  | 1.6789 ns | 0.1070 ns | 0.1569 ns | 1.8038 ns | 1.4847 ns | 1.8518 ns | 1.8417 ns |  1.11 |    0.11 |     259 B |         - |          NA |
| StringSwitch    | Miss  | 1.6569 ns | 0.0201 ns | 0.0300 ns | 1.6523 ns | 1.6145 ns | 1.7058 ns | 1.6992 ns |  1.10 |    0.05 |     259 B |         - |          NA |
| SpanSwitch      | Miss  | 1.8760 ns | 0.0478 ns | 0.0670 ns | 1.8607 ns | 1.7901 ns | 2.0959 ns | 1.9697 ns |  1.24 |    0.06 |     259 B |         - |          NA |

`EqualsChain` and `StringSwitch` compile to a byte-identical instruction stream here, and the sign of the difference flips between runs (an earlier run measured 1.009 vs 1.038 the other way). The span switch is the only form that is genuinely slower.

## 16 keys (the main win)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Probe | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **EqualsChain**      | **Hit**   | **3.466 ns** | **0.0332 ns** | **0.0476 ns** | **3.459 ns** | **3.379 ns** | **3.583 ns** | **3.537 ns** |  **1.00** |    **0.02** |   **1,146 B** |         **-** |          **NA** |
| SpanEqualsChain  | Hit   | 2.808 ns | 0.0677 ns | 0.0949 ns | 2.843 ns | 2.697 ns | 2.954 ns | 2.915 ns |  0.81 |    0.03 |   1,169 B |         - |          NA |
| StringSwitch     | Hit   | 1.776 ns | 0.0448 ns | 0.0657 ns | 1.746 ns | 1.702 ns | 1.928 ns | 1.905 ns |  0.51 |    0.02 |   1,170 B |         - |          NA |
| SpanSwitch       | Hit   | 2.008 ns | 0.0512 ns | 0.0751 ns | 2.033 ns | 1.915 ns | 2.115 ns | 2.097 ns |  0.58 |    0.02 |   1,185 B |         - |          NA |
| DictionaryLookup | Hit   | 4.776 ns | 0.0396 ns | 0.0569 ns | 4.783 ns | 4.692 ns | 4.879 ns | 4.838 ns |  1.38 |    0.02 |   1,142 B |         - |          NA |
| SampledTable     | Hit   | 3.788 ns | 0.0223 ns | 0.0298 ns | 3.777 ns | 3.741 ns | 3.845 ns | 3.827 ns |  1.09 |    0.02 |     766 B |         - |          NA |
|                  |       |          |           |           |          |          |          |          |       |         |           |           |             |
| **EqualsChain**      | **Miss**  | **6.080 ns** | **0.0865 ns** | **0.1240 ns** | **6.091 ns** | **5.854 ns** | **6.307 ns** | **6.224 ns** |  **1.00** |    **0.03** |   **1,146 B** |         **-** |          **NA** |
| SpanEqualsChain  | Miss  | 5.177 ns | 0.0457 ns | 0.0656 ns | 5.182 ns | 5.047 ns | 5.323 ns | 5.245 ns |  0.85 |    0.02 |   1,178 B |         - |          NA |
| StringSwitch     | Miss  | 1.993 ns | 0.0138 ns | 0.0202 ns | 1.999 ns | 1.953 ns | 2.027 ns | 2.017 ns |  0.33 |    0.01 |   1,166 B |         - |          NA |
| SpanSwitch       | Miss  | 2.016 ns | 0.0717 ns | 0.1005 ns | 1.956 ns | 1.897 ns | 2.146 ns | 2.138 ns |  0.33 |    0.02 |   1,179 B |         - |          NA |
| DictionaryLookup | Miss  | 3.152 ns | 0.0216 ns | 0.0316 ns | 3.158 ns | 3.091 ns | 3.201 ns | 3.188 ns |  0.52 |    0.01 |     509 B |         - |          NA |
| SampledTable     | Miss  | 4.908 ns | 0.0854 ns | 0.1252 ns | 4.869 ns | 4.740 ns | 5.316 ns | 5.065 ns |  0.81 |    0.03 |     753 B |         - |          NA |

## 64 keys

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Probe | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **Hit**   | **2.633 ns** | **0.0301 ns** | **0.0450 ns** | **2.633 ns** | **2.551 ns** | **2.723 ns** | **2.683 ns** |  **1.00** |    **0.02** |   **5,825 B** |         **-** |          **NA** |
| SpanSwitch    | Hit   | 2.792 ns | 0.0224 ns | 0.0328 ns | 2.791 ns | 2.736 ns | 2.862 ns | 2.833 ns |  1.06 |    0.02 |   4,897 B |         - |          NA |
| SampledTable  | Hit   | 3.607 ns | 0.0646 ns | 0.0926 ns | 3.550 ns | 3.499 ns | 3.761 ns | 3.721 ns |  1.37 |    0.04 |     755 B |         - |          NA |
|               |       |          |           |           |          |          |          |          |       |         |           |           |             |
| **SampledSwitch** | **Miss**  | **2.419 ns** | **0.0462 ns** | **0.0632 ns** | **2.402 ns** | **2.341 ns** | **2.585 ns** | **2.522 ns** |  **1.00** |    **0.04** |   **6,001 B** |         **-** |          **NA** |
| SpanSwitch    | Miss  | 2.408 ns | 0.0467 ns | 0.0685 ns | 2.387 ns | 2.291 ns | 2.510 ns | 2.492 ns |  1.00 |    0.04 |   5,019 B |         - |          NA |
| SampledTable  | Miss  | 4.074 ns | 0.0142 ns | 0.0209 ns | 4.075 ns | 4.039 ns | 4.120 ns | 4.101 ns |  1.69 |    0.04 |     733 B |         - |          NA |

Miss is level (2.419 +/- 0.046 vs 2.408 +/- 0.047, CIs overlap) but code size differs (6,001 vs 5,019 B), so it is recorded as a measurement tie rather than "no difference".

## 128 keys (the cliff)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Probe | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |------ |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **Hit**   |  **2.650 ns** | **0.0360 ns** | **0.0538 ns** |  **2.639 ns** |  **2.551 ns** |  **2.755 ns** |  **2.716 ns** |  **1.00** |    **0.03** |  **11,506 B** |         **-** |          **NA** |
| SpanSwitch    | Hit   |  4.580 ns | 0.3549 ns | 0.5090 ns |  4.514 ns |  3.807 ns |  5.543 ns |  5.217 ns |  1.73 |    0.19 |  11,611 B |         - |          NA |
| SampledTable  | Hit   |  3.722 ns | 0.0103 ns | 0.0154 ns |  3.722 ns |  3.692 ns |  3.754 ns |  3.741 ns |  1.41 |    0.03 |     776 B |         - |          NA |
|               |       |           |           |           |           |           |           |           |       |         |           |           |             |
| **SampledSwitch** | **Miss**  |  **2.687 ns** | **0.1545 ns** | **0.2215 ns** |  **2.542 ns** |  **2.414 ns** |  **3.091 ns** |  **2.962 ns** |  **1.01** |    **0.11** |  **11,517 B** |         **-** |          **NA** |
| SpanSwitch    | Miss  | 10.112 ns | 0.0234 ns | 0.0350 ns | 10.111 ns | 10.054 ns | 10.185 ns | 10.157 ns |  3.79 |    0.30 |  11,839 B |         - |          NA |
| SampledTable  | Miss  |  4.300 ns | 0.0138 ns | 0.0207 ns |  4.295 ns |  4.270 ns |  4.338 ns |  4.331 ns |  1.61 |    0.13 |     756 B |         - |          NA |

Miss costs far more than hit because the binary search has to descend to full depth before it can conclude "not present", while a hit can exit at an earlier arm.

## 16 keys x 58-62 characters (long keys)

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Probe | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **SampledSwitch** | **Hit**   | **1.856 ns** | **0.0098 ns** | **0.0146 ns** | **1.827 ns** | **1.878 ns** | **1.872 ns** |  **1.00** |    **0.01** |   **2,018 B** |         **-** |          **NA** |
| SpanSwitch    | Hit   | 2.051 ns | 0.0113 ns | 0.0165 ns | 2.027 ns | 2.090 ns | 2.073 ns |  1.10 |    0.01 |   1,651 B |         - |          NA |
| SampledTable  | Hit   | 3.525 ns | 0.0340 ns | 0.0488 ns | 3.477 ns | 3.655 ns | 3.602 ns |  1.90 |    0.03 |     730 B |         - |          NA |
|               |       |          |           |           |          |          |          |       |         |           |           |             |
| **SampledSwitch** | **Miss**  | **1.869 ns** | **0.0046 ns** | **0.0069 ns** | **1.857 ns** | **1.884 ns** | **1.877 ns** |  **1.00** |    **0.01** |   **2,039 B** |         **-** |          **NA** |
| SpanSwitch    | Miss  | 2.085 ns | 0.0320 ns | 0.0479 ns | 2.022 ns | 2.227 ns | 2.149 ns |  1.12 |    0.03 |   1,640 B |         - |          NA |
| SampledTable  | Miss  | 3.077 ns | 0.0165 ns | 0.0247 ns | 3.041 ns | 3.138 ns | 3.107 ns |  1.65 |    0.01 |     718 B |         - |          NA |

No reversal. The plain switch is only 1.10-1.12x behind, barely worse than at 64 short keys - because Roslyn does not hash this shape at all.

## Code size

| Key set | Plain switch | Sampling-hash switch | SampledNameTable |
|---|---:|---:|---:|
| 16 | 1,185 B | - | 766 B |
| 16 x 58-62 chars | 1,651 B | 2,018 B | 730 B |
| 64 | 4,897 B | 5,825 B | 755 B |
| 128 | 11,611 B | 11,506 B | **776 B** |

Both switch forms grow with the key set; the runtime table does not. Where binary size or I-cache pressure matters, SampledNameTable ([COL-04](COL-04-SampledNameTable.md)) can be the right answer even at sizes where the switch is 1.1-1.9x faster.

## Where the switch does not fit: matching that needs a conversion

The result above is a comparison against a **plain** `Equals` chain - one where the probe can be compared exactly as it arrives. As soon as the input has to be converted first, the conversion dominates and the switch stops being the right answer.

Measured on the DB column-name shape: resolve a reader's columns to ordinals, matching `OrdinalIgnoreCase` per SQL identifier rules. The current generated form is a guarded `Equals(OrdinalIgnoreCase)` chain at <=16 groups and a sampling-hash switch above that. The alternative is to normalize the probe first so a plain ordinal switch can be used.

| Approach | 8 cols Pascal | 8 cols snake | 24 cols Pascal | 24 cols snake |
|---|---:|---:|---:|---:|
| **Current (chain / hash)** | **15.4 ns** | **15.6 ns** | **129.2 ns** | **129.1 ns** |
| `ToUpperInvariant()` string + string switch | 120.6 (7.82) | 117.0 (7.50) | 387.2 (3.00) | 388.6 (3.01) |
| stackalloc normalize + span switch | 78.4 (5.09) | 75.2 (4.82) | 243.6 (1.89) | 242.6 (1.88) |
| `Ascii.ToUpper` (SIMD) + span switch | 72.3 (4.69) | 70.0 (4.49) | 227.6 (1.76) | 226.6 (1.76) |
| Plain switch, no case handling (reference) | 34.3 (2.23) | 35.4 (2.27) | 102.5 (0.79) | 108.1 (0.84) |

Per column:

| Approach | 8 cols | 24 cols |
|---|---:|---:|
| Current | 1.93 ns | 5.38 ns |
| Plain switch, no case handling | 4.29 ns | 4.27 ns |
| `Ascii.ToUpper` + span switch | 9.04 ns | 9.48 ns |
| stackalloc normalize + span switch | 9.80 ns | 10.15 ns |

- **Normalization adds about 4.8-5.2 ns per column, and the SIMD `Ascii.ToUpper` version is no cheaper.** The cost is structural - fold every character into a buffer, then read every character again in the switch - so no API choice avoids it. It exceeds the switch's own dispatch gain
- The plain switch's dispatch is a size-independent **4.3 ns per column**. That beats the current hash form at 24 columns (5.38) but loses to the chain at 8 (1.93), where arriving in declaration order costs roughly one `Equals` per column
- The 0.79x at 24 columns is **not** the switch being fast - it is case-insensitive matching being dropped, which changes the semantics
- Naming convention makes no difference (PascalCase vs snake_case within a few percent), because the generated literals already carry the applied convention
- Extrapolating the chain's 1.93 ns/column to 24 columns by N²/2 gives about 120 ns against the hash form's 129 ns, so **the existing switchover at 16 columns is consistent with measurement**

Where case folding, culture handling, or trimming has to happen first, use the sampling-hash form instead: only the 3 sampled characters are converted, and the confirming comparison stays `OrdinalIgnoreCase`. The switch is the default only when keys can be compared **as they arrive**.

## Guidance

- Key set fixed at compile time, comparable **as-is**, and **<= 64 entries: write the switch first**, before any hand-written dispatch
- **> 64 entries**: sampling-hash switch (see [generated-code-patterns](../../docs/generated-code-patterns.md) scenario 1)
- **<= 4 entries**: no reason to change - the chain and the switch are the same code, and on span input the chain wins
- Key length is not a criterion
- The switch is **ordinal**. Case-insensitive matching cannot use it; use the sampling-hash form with upper-cased sampling plus an `OrdinalIgnoreCase` confirm. Normalizing the input up front to reach a plain switch is measurably worse at every size
- Expect **around 0.5x against an Equals chain at 16 keys** - a real win, but not an order-of-magnitude one
