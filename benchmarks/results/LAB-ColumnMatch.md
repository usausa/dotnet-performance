# LAB-ColumnMatch: normalizing the probe vs matching case-insensitively (TXT-10 counter-case)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - converting the probe still loses in all 12 conditions on net11; the generated chain / hash is the fastest at 8 columns and the plain span switch at 24
- `ToUpperInvariant` + string switch got 0.78-0.80x faster on net11 when the probe is already upper case (34.4 → 26.9 ns at 8, 107.5 → 84.0 ns at 24). At 24 columns that leaves it 1.06-1.08x of the generated hash instead of 1.36x, still a loss
- The string-switch forms carry 1.3-2.5x more code on net11 (up to 9.9 KB) with no time change where the probe is not already upper case

### ColumnMatchPascal8Benchmark

net10 → net11 per row:

| Method | Casing | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| GeneratedChain | AsDeclared | 8.740 ns | 9.195 ns | 1.05 | - → - | 1,012 B → 986 B |
| UpperStringSwitch | AsDeclared | 69.107 ns | 64.982 ns | 0.94 | 304 B → 304 B | 2,765 B → 4,942 B |
| UpperSpanSwitch | AsDeclared | 48.710 ns | 48.504 ns | 1.00 | - → - | 1,865 B → 1,743 B |
| AsciiUpperSpanSwitch | AsDeclared | 45.668 ns | 45.250 ns | 0.99 | - → - | 1,648 B → 1,559 B |
| PlainSpanSwitch | AsDeclared | 18.023 ns | 18.566 ns | 1.03 | - → - | 1,134 B → 1,030 B |
| ChainViaMatcher | AsDeclared | 18.469 ns | 19.202 ns | 1.04 | - → - | 1,804 B → 1,786 B |
| GeneratedChain | AllUpper | 8.569 ns | 9.230 ns | 1.08 | - → - | 1,012 B → 986 B |
| UpperStringSwitch | AllUpper | 34.402 ns | 26.870 ns | 0.78 | - → - | 2,833 B → 3,777 B |
| UpperSpanSwitch | AllUpper | 48.799 ns | 48.431 ns | 0.99 | - → - | 1,865 B → 1,743 B |
| AsciiUpperSpanSwitch | AllUpper | 45.805 ns | 45.211 ns | 0.99 | - → - | 1,648 B → 1,559 B |
| PlainSpanSwitch | AllUpper | 15.749 ns | 15.849 ns | 1.01 | - → - | 1,141 B → 1,111 B |
| ChainViaMatcher | AllUpper | 18.521 ns | 19.014 ns | 1.03 | - → - | 1,804 B → 1,786 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.64 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Casing     | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedChain**       | **.NET 10.0**           | **.NET 10.0** | **AsDeclared** |  **8.740 ns** | **0.0936 ns** | **0.1372 ns** |  **8.731 ns** |  **8.446 ns** |  **9.028 ns** |  **8.891 ns** |  **1.00** |    **0.00** |   **1,012 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AsDeclared | 69.107 ns | 0.4519 ns | 0.6482 ns | 69.028 ns | 68.074 ns | 71.110 ns | 69.843 ns |  7.91 |    0.14 |   2,765 B | 0.0362 |     304 B |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared | 48.710 ns | 0.2171 ns | 0.3182 ns | 48.676 ns | 48.228 ns | 49.410 ns | 49.215 ns |  5.57 |    0.09 |   1,865 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AsDeclared | 45.668 ns | 0.1118 ns | 0.1567 ns | 45.644 ns | 45.470 ns | 46.057 ns | 45.952 ns |  5.23 |    0.08 |   1,648 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared | 18.023 ns | 0.2109 ns | 0.3025 ns | 17.999 ns | 17.496 ns | 18.571 ns | 18.366 ns |  2.06 |    0.05 |   1,134 B |      - |         - |          NA |
| ChainViaMatcher      | .NET 10.0           | .NET 10.0 | AsDeclared | 18.469 ns | 0.0813 ns | 0.1191 ns | 18.462 ns | 18.287 ns | 18.815 ns | 18.621 ns |  2.11 |    0.04 |   1,804 B |      - |         - |          NA |
|                      |                     |           |            |           |           |           |           |           |           |           |       |         |           |        |           |             |
| GeneratedChain       | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared |  9.195 ns | 0.3834 ns | 0.5619 ns |  8.825 ns |  8.562 ns |  9.840 ns |  9.801 ns |  1.00 |    0.00 |     986 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 64.982 ns | 0.4197 ns | 0.6152 ns | 64.761 ns | 64.033 ns | 66.275 ns | 65.839 ns |  7.09 |    0.43 |   4,942 B | 0.0362 |     304 B |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 48.504 ns | 0.1722 ns | 0.2357 ns | 48.511 ns | 48.100 ns | 48.956 ns | 48.748 ns |  5.29 |    0.32 |   1,743 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 45.250 ns | 0.1351 ns | 0.2022 ns | 45.226 ns | 44.915 ns | 45.668 ns | 45.532 ns |  4.94 |    0.30 |   1,559 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 18.566 ns | 0.2879 ns | 0.4309 ns | 18.504 ns | 17.933 ns | 19.316 ns | 19.157 ns |  2.03 |    0.13 |   1,030 B |      - |         - |          NA |
| ChainViaMatcher      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 19.202 ns | 0.1423 ns | 0.2086 ns | 19.279 ns | 18.824 ns | 19.528 ns | 19.424 ns |  2.10 |    0.13 |   1,786 B |      - |         - |          NA |
|                      |                     |           |            |           |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **.NET 10.0**           | **.NET 10.0** | **AllUpper**   |  **8.569 ns** | **0.0975 ns** | **0.1459 ns** |  **8.559 ns** |  **8.332 ns** |  **8.954 ns** |  **8.754 ns** |  **1.00** |    **0.00** |   **1,012 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AllUpper   | 34.402 ns | 0.8837 ns | 1.3227 ns | 34.473 ns | 32.711 ns | 36.260 ns | 35.877 ns |  4.02 |    0.17 |   2,833 B |      - |         - |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   | 48.799 ns | 0.2329 ns | 0.3414 ns | 48.702 ns | 48.299 ns | 49.729 ns | 49.366 ns |  5.70 |    0.10 |   1,865 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AllUpper   | 45.805 ns | 0.1732 ns | 0.2539 ns | 45.748 ns | 45.479 ns | 46.391 ns | 46.133 ns |  5.35 |    0.09 |   1,648 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   | 15.749 ns | 0.0983 ns | 0.1440 ns | 15.742 ns | 15.380 ns | 16.094 ns | 15.915 ns |  1.84 |    0.03 |   1,141 B |      - |         - |          NA |
| ChainViaMatcher      | .NET 10.0           | .NET 10.0 | AllUpper   | 18.521 ns | 0.1154 ns | 0.1727 ns | 18.510 ns | 18.259 ns | 18.920 ns | 18.745 ns |  2.16 |    0.04 |   1,804 B |      - |         - |          NA |
|                      |                     |           |            |           |           |           |           |           |           |           |       |         |           |        |           |             |
| GeneratedChain       | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  9.230 ns | 0.3450 ns | 0.5164 ns |  9.255 ns |  8.644 ns |  9.880 ns |  9.784 ns |  1.00 |    0.00 |     986 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 26.870 ns | 0.1371 ns | 0.1966 ns | 26.830 ns | 26.565 ns | 27.372 ns | 27.177 ns |  2.92 |    0.16 |   3,777 B |      - |         - |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 48.431 ns | 0.1480 ns | 0.2215 ns | 48.405 ns | 48.077 ns | 49.021 ns | 48.664 ns |  5.26 |    0.29 |   1,743 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 45.211 ns | 0.1099 ns | 0.1504 ns | 45.206 ns | 44.899 ns | 45.478 ns | 45.395 ns |  4.91 |    0.27 |   1,559 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 15.849 ns | 0.1056 ns | 0.1514 ns | 15.871 ns | 15.481 ns | 16.143 ns | 16.020 ns |  1.72 |    0.10 |   1,111 B |      - |         - |          NA |
| ChainViaMatcher      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 19.014 ns | 0.0768 ns | 0.1076 ns | 19.010 ns | 18.873 ns | 19.255 ns | 19.155 ns |  2.07 |    0.11 |   1,786 B |      - |         - |          NA |

### ColumnMatchSnake8Benchmark

net10 → net11 per row:

| Method | Casing | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| GeneratedChain | AsDeclared | 8.460 ns | 8.523 ns | 1.01 | - → - | 1,034 B → 1,035 B |
| UpperStringSwitch | AsDeclared | 72.315 ns | 68.850 ns | 0.95 | 328 B → 328 B | 2,763 B → 4,940 B |
| UpperSpanSwitch | AsDeclared | 52.512 ns | 51.892 ns | 0.99 | - → - | 1,855 B → 1,741 B |
| AsciiUpperSpanSwitch | AsDeclared | 49.232 ns | 49.141 ns | 1.00 | - → - | 1,638 B → 1,557 B |
| PlainSpanSwitch | AsDeclared | 17.673 ns | 18.212 ns | 1.03 | - → - | 1,133 B → 1,033 B |
| ChainViaMatcher | AsDeclared | 18.260 ns | 18.345 ns | 1.00 | - → - | 1,847 B → 1,873 B |
| GeneratedChain | AllUpper | 8.418 ns | 8.486 ns | 1.01 | - → - | 1,034 B → 1,035 B |
| UpperStringSwitch | AllUpper | 33.989 ns | 27.298 ns | 0.80 | - → - | 2,833 B → 3,783 B |
| UpperSpanSwitch | AllUpper | 52.517 ns | 52.779 ns | 1.00 | - → - | 1,855 B → 1,741 B |
| AsciiUpperSpanSwitch | AllUpper | 49.452 ns | 49.719 ns | 1.01 | - → - | 1,638 B → 1,557 B |
| PlainSpanSwitch | AllUpper | 15.415 ns | 15.588 ns | 1.01 | - → - | 1,140 B → 1,114 B |
| ChainViaMatcher | AllUpper | 18.364 ns | 18.478 ns | 1.01 | - → - | 1,847 B → 1,873 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.62 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Casing     | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedChain**       | **.NET 10.0**           | **.NET 10.0** | **AsDeclared** |  **8.460 ns** | **0.0426 ns** | **0.0625 ns** |  **8.336 ns** |  **8.601 ns** |  **8.546 ns** |  **1.00** |    **0.00** |   **1,034 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AsDeclared | 72.315 ns | 0.3967 ns | 0.5815 ns | 71.266 ns | 73.687 ns | 73.114 ns |  8.55 |    0.09 |   2,763 B | 0.0391 |     328 B |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared | 52.512 ns | 0.1176 ns | 0.1724 ns | 52.108 ns | 52.802 ns | 52.697 ns |  6.21 |    0.05 |   1,855 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AsDeclared | 49.232 ns | 0.1290 ns | 0.1849 ns | 48.930 ns | 49.657 ns | 49.479 ns |  5.82 |    0.05 |   1,638 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared | 17.673 ns | 0.2874 ns | 0.4212 ns | 17.028 ns | 18.344 ns | 18.175 ns |  2.09 |    0.05 |   1,133 B |      - |         - |          NA |
| ChainViaMatcher      | .NET 10.0           | .NET 10.0 | AsDeclared | 18.260 ns | 0.0560 ns | 0.0767 ns | 18.120 ns | 18.383 ns | 18.355 ns |  2.16 |    0.02 |   1,847 B |      - |         - |          NA |
|                      |                     |           |            |           |           |           |           |           |           |       |         |           |        |           |             |
| GeneratedChain       | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared |  8.523 ns | 0.0637 ns | 0.0934 ns |  8.379 ns |  8.668 ns |  8.662 ns |  1.00 |    0.00 |   1,035 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 68.850 ns | 0.7683 ns | 1.1262 ns | 67.064 ns | 70.793 ns | 70.350 ns |  8.08 |    0.16 |   4,940 B | 0.0391 |     328 B |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 51.892 ns | 0.1110 ns | 0.1627 ns | 51.590 ns | 52.184 ns | 52.076 ns |  6.09 |    0.07 |   1,741 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 49.141 ns | 0.1371 ns | 0.2009 ns | 48.761 ns | 49.571 ns | 49.353 ns |  5.77 |    0.07 |   1,557 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 18.212 ns | 0.3728 ns | 0.5347 ns | 17.359 ns | 19.015 ns | 18.824 ns |  2.14 |    0.07 |   1,033 B |      - |         - |          NA |
| ChainViaMatcher      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 18.345 ns | 0.0830 ns | 0.1217 ns | 18.115 ns | 18.642 ns | 18.490 ns |  2.15 |    0.03 |   1,873 B |      - |         - |          NA |
|                      |                     |           |            |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **.NET 10.0**           | **.NET 10.0** | **AllUpper**   |  **8.418 ns** | **0.0424 ns** | **0.0621 ns** |  **8.297 ns** |  **8.552 ns** |  **8.493 ns** |  **1.00** |    **0.00** |   **1,034 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AllUpper   | 33.989 ns | 0.3835 ns | 0.5621 ns | 33.109 ns | 35.214 ns | 34.770 ns |  4.04 |    0.07 |   2,833 B |      - |         - |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   | 52.517 ns | 0.1775 ns | 0.2602 ns | 52.031 ns | 53.053 ns | 52.808 ns |  6.24 |    0.05 |   1,855 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AllUpper   | 49.452 ns | 0.1343 ns | 0.1883 ns | 49.129 ns | 49.877 ns | 49.757 ns |  5.87 |    0.05 |   1,638 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   | 15.415 ns | 0.1501 ns | 0.2246 ns | 15.082 ns | 15.946 ns | 15.770 ns |  1.83 |    0.03 |   1,140 B |      - |         - |          NA |
| ChainViaMatcher      | .NET 10.0           | .NET 10.0 | AllUpper   | 18.364 ns | 0.0766 ns | 0.1099 ns | 18.204 ns | 18.626 ns | 18.522 ns |  2.18 |    0.02 |   1,847 B |      - |         - |          NA |
|                      |                     |           |            |           |           |           |           |           |           |       |         |           |        |           |             |
| GeneratedChain       | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  8.486 ns | 0.0476 ns | 0.0713 ns |  8.356 ns |  8.599 ns |  8.564 ns |  1.00 |    0.00 |   1,035 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 27.298 ns | 0.1171 ns | 0.1753 ns | 27.031 ns | 27.753 ns | 27.505 ns |  3.22 |    0.03 |   3,783 B |      - |         - |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 52.779 ns | 0.8132 ns | 1.2171 ns | 51.575 ns | 55.910 ns | 54.716 ns |  6.22 |    0.15 |   1,741 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 49.719 ns | 0.2816 ns | 0.4128 ns | 49.190 ns | 50.852 ns | 50.252 ns |  5.86 |    0.07 |   1,557 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 15.588 ns | 0.0906 ns | 0.1300 ns | 15.376 ns | 15.960 ns | 15.753 ns |  1.84 |    0.02 |   1,114 B |      - |         - |          NA |
| ChainViaMatcher      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 18.478 ns | 0.1094 ns | 0.1497 ns | 18.241 ns | 18.822 ns | 18.646 ns |  2.18 |    0.03 |   1,873 B |      - |         - |          NA |

### ColumnMatchPascal24Benchmark

net10 → net11 per row:

| Method | Casing | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| GeneratedHash | AsDeclared | 79.59 ns | 79.39 ns | 1.00 | - → - | 3,367 B → 3,422 B |
| UpperStringSwitch | AsDeclared | 227.23 ns | 229.04 ns | 1.01 | 976 B → 976 B | 3,939 B → 9,852 B |
| UpperSpanSwitch | AsDeclared | 161.30 ns | 160.87 ns | 1.00 | - → - | 3,117 B → 2,747 B |
| AsciiUpperSpanSwitch | AsDeclared | 152.61 ns | 157.18 ns | 1.03 | - → - | 2,900 B → 2,563 B |
| PlainSpanSwitch | AsDeclared | 56.05 ns | 56.41 ns | 1.01 | - → - | 2,395 B → 2,034 B |
| GeneratedHash | AllUpper | 79.00 ns | 79.11 ns | 1.00 | - → - | 3,367 B → 3,422 B |
| UpperStringSwitch | AllUpper | 107.54 ns | 83.99 ns | 0.78 | - → - | 3,612 B → 4,633 B |
| UpperSpanSwitch | AllUpper | 161.48 ns | 161.48 ns | 1.00 | - → - | 3,119 B → 2,749 B |
| AsciiUpperSpanSwitch | AllUpper | 158.93 ns | 151.81 ns | 0.96 | - → - | 2,902 B → 2,565 B |
| PlainSpanSwitch | AllUpper | 51.10 ns | 50.86 ns | 1.00 | - → - | 2,386 B → 2,198 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.55 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Casing     | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |----------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedHash**        | **.NET 10.0**           | **.NET 10.0** | **AsDeclared** |  **79.59 ns** | **0.389 ns** | **0.558 ns** |  **78.68 ns** |  **80.91 ns** |  **80.26 ns** |  **1.00** |    **0.00** |   **3,367 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AsDeclared | 227.23 ns | 1.033 ns | 1.546 ns | 224.97 ns | 230.99 ns | 229.42 ns |  2.86 |    0.03 |   3,939 B | 0.1166 |     976 B |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared | 161.30 ns | 0.488 ns | 0.715 ns | 160.14 ns | 162.69 ns | 162.19 ns |  2.03 |    0.02 |   3,117 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AsDeclared | 152.61 ns | 0.726 ns | 1.064 ns | 151.57 ns | 155.00 ns | 154.10 ns |  1.92 |    0.02 |   2,900 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared |  56.05 ns | 0.650 ns | 0.973 ns |  54.45 ns |  58.09 ns |  57.28 ns |  0.70 |    0.01 |   2,395 B |      - |         - |          NA |
|                      |                     |           |            |           |          |          |           |           |           |       |         |           |        |           |             |
| GeneratedHash        | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared |  79.39 ns | 0.279 ns | 0.400 ns |  78.88 ns |  80.63 ns |  79.90 ns |  1.00 |    0.00 |   3,422 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 229.04 ns | 2.956 ns | 4.240 ns | 222.51 ns | 235.04 ns | 234.07 ns |  2.89 |    0.05 |   9,852 B | 0.1166 |     976 B |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 160.87 ns | 0.633 ns | 0.928 ns | 159.12 ns | 163.26 ns | 161.79 ns |  2.03 |    0.02 |   2,747 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 157.18 ns | 3.448 ns | 5.161 ns | 150.81 ns | 163.56 ns | 162.81 ns |  1.98 |    0.06 |   2,563 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared |  56.41 ns | 0.252 ns | 0.377 ns |  55.74 ns |  57.21 ns |  56.81 ns |  0.71 |    0.01 |   2,034 B |      - |         - |          NA |
|                      |                     |           |            |           |          |          |           |           |           |       |         |           |        |           |             |
| **GeneratedHash**        | **.NET 10.0**           | **.NET 10.0** | **AllUpper**   |  **79.00 ns** | **0.330 ns** | **0.473 ns** |  **78.05 ns** |  **80.02 ns** |  **79.54 ns** |  **1.00** |    **0.00** |   **3,367 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AllUpper   | 107.54 ns | 4.089 ns | 5.993 ns | 102.95 ns | 124.79 ns | 118.75 ns |  1.36 |    0.08 |   3,612 B |      - |         - |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   | 161.48 ns | 0.736 ns | 1.079 ns | 159.56 ns | 164.55 ns | 162.57 ns |  2.04 |    0.02 |   3,119 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AllUpper   | 158.93 ns | 4.841 ns | 6.943 ns | 151.18 ns | 170.15 ns | 166.64 ns |  2.01 |    0.09 |   2,902 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   |  51.10 ns | 0.701 ns | 1.005 ns |  49.75 ns |  52.57 ns |  52.25 ns |  0.65 |    0.01 |   2,386 B |      - |         - |          NA |
|                      |                     |           |            |           |          |          |           |           |           |       |         |           |        |           |             |
| GeneratedHash        | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  79.11 ns | 0.255 ns | 0.357 ns |  78.49 ns |  79.89 ns |  79.58 ns |  1.00 |    0.00 |   3,422 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  83.99 ns | 0.216 ns | 0.303 ns |  83.47 ns |  84.43 ns |  84.32 ns |  1.06 |    0.01 |   4,633 B |      - |         - |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 161.48 ns | 0.625 ns | 0.936 ns | 160.18 ns | 163.53 ns | 162.85 ns |  2.04 |    0.01 |   2,749 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 151.81 ns | 0.895 ns | 1.284 ns | 150.23 ns | 154.62 ns | 153.75 ns |  1.92 |    0.02 |   2,565 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  50.86 ns | 0.240 ns | 0.351 ns |  50.29 ns |  51.65 ns |  51.27 ns |  0.64 |    0.01 |   2,198 B |      - |         - |          NA |

### ColumnMatchSnake24Benchmark

net10 → net11 per row:

| Method | Casing | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| GeneratedHash | AsDeclared | 79.97 ns | 79.78 ns | 1.00 | - → - | 3,357 B → 3,502 B |
| UpperStringSwitch | AsDeclared | 229.35 ns | 227.88 ns | 0.99 | 1008 B → 1008 B | 3,962 B → 9,878 B |
| UpperSpanSwitch | AsDeclared | 162.52 ns | 161.61 ns | 0.99 | - → - | 3,138 B → 2,783 B |
| AsciiUpperSpanSwitch | AsDeclared | 154.17 ns | 153.11 ns | 0.99 | - → - | 2,921 B → 2,599 B |
| PlainSpanSwitch | AsDeclared | 55.07 ns | 56.32 ns | 1.02 | - → - | 2,410 B → 2,064 B |
| GeneratedHash | AllUpper | 79.25 ns | 79.68 ns | 1.01 | - → - | 3,357 B → 3,502 B |
| UpperStringSwitch | AllUpper | 107.62 ns | 85.88 ns | 0.80 | - → - | 3,639 B → 4,668 B |
| UpperSpanSwitch | AllUpper | 162.39 ns | 161.26 ns | 0.99 | - → - | 3,140 B → 2,785 B |
| AsciiUpperSpanSwitch | AllUpper | 154.20 ns | 153.08 ns | 0.99 | - → - | 2,923 B → 2,601 B |
| PlainSpanSwitch | AllUpper | 43.75 ns | 44.07 ns | 1.01 | - → - | 2,453 B → 2,407 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.32 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Casing     | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |----------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedHash**        | **.NET 10.0**           | **.NET 10.0** | **AsDeclared** |  **79.97 ns** | **0.621 ns** | **0.871 ns** |  **78.70 ns** |  **81.57 ns** |  **80.91 ns** |  **1.00** |    **0.00** |   **3,357 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AsDeclared | 229.35 ns | 1.417 ns | 2.078 ns | 225.18 ns | 233.38 ns | 231.66 ns |  2.87 |    0.04 |   3,962 B | 0.1204 |    1008 B |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared | 162.52 ns | 0.400 ns | 0.586 ns | 161.24 ns | 163.51 ns | 163.19 ns |  2.03 |    0.02 |   3,138 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AsDeclared | 154.17 ns | 0.598 ns | 0.895 ns | 152.61 ns | 156.08 ns | 155.44 ns |  1.93 |    0.02 |   2,921 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AsDeclared |  55.07 ns | 0.337 ns | 0.493 ns |  54.18 ns |  56.40 ns |  55.66 ns |  0.69 |    0.01 |   2,410 B |      - |         - |          NA |
|                      |                     |           |            |           |          |          |           |           |           |       |         |           |        |           |             |
| GeneratedHash        | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared |  79.78 ns | 0.452 ns | 0.634 ns |  78.59 ns |  80.96 ns |  80.45 ns |  1.00 |    0.00 |   3,502 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 227.88 ns | 0.919 ns | 1.258 ns | 225.72 ns | 230.65 ns | 229.11 ns |  2.86 |    0.03 |   9,878 B | 0.1204 |    1008 B |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 161.61 ns | 0.400 ns | 0.574 ns | 160.88 ns | 163.14 ns | 162.21 ns |  2.03 |    0.02 |   2,783 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared | 153.11 ns | 0.362 ns | 0.507 ns | 152.18 ns | 154.09 ns | 153.82 ns |  1.92 |    0.02 |   2,599 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AsDeclared |  56.32 ns | 0.375 ns | 0.537 ns |  55.46 ns |  57.76 ns |  56.95 ns |  0.71 |    0.01 |   2,064 B |      - |         - |          NA |
|                      |                     |           |            |           |          |          |           |           |           |       |         |           |        |           |             |
| **GeneratedHash**        | **.NET 10.0**           | **.NET 10.0** | **AllUpper**   |  **79.25 ns** | **0.502 ns** | **0.751 ns** |  **78.19 ns** |  **80.65 ns** |  **80.34 ns** |  **1.00** |    **0.00** |   **3,357 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | .NET 10.0           | .NET 10.0 | AllUpper   | 107.62 ns | 1.002 ns | 1.469 ns | 104.86 ns | 109.81 ns | 109.33 ns |  1.36 |    0.02 |   3,639 B |      - |         - |          NA |
| UpperSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   | 162.39 ns | 0.524 ns | 0.768 ns | 161.13 ns | 164.14 ns | 163.38 ns |  2.05 |    0.02 |   3,140 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | .NET 10.0           | .NET 10.0 | AllUpper   | 154.20 ns | 0.632 ns | 0.906 ns | 153.04 ns | 156.49 ns | 155.57 ns |  1.95 |    0.02 |   2,923 B |      - |         - |          NA |
| PlainSpanSwitch      | .NET 10.0           | .NET 10.0 | AllUpper   |  43.75 ns | 0.178 ns | 0.260 ns |  43.42 ns |  44.49 ns |  44.09 ns |  0.55 |    0.01 |   2,453 B |      - |         - |          NA |
|                      |                     |           |            |           |          |          |           |           |           |       |         |           |        |           |             |
| GeneratedHash        | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  79.68 ns | 0.531 ns | 0.744 ns |  78.50 ns |  81.16 ns |  80.58 ns |  1.00 |    0.00 |   3,502 B |      - |         - |          NA |
| UpperStringSwitch    | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  85.88 ns | 0.452 ns | 0.662 ns |  84.90 ns |  87.49 ns |  86.77 ns |  1.08 |    0.01 |   4,668 B |      - |         - |          NA |
| UpperSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 161.26 ns | 0.330 ns | 0.451 ns | 160.19 ns | 162.12 ns | 161.74 ns |  2.02 |    0.02 |   2,785 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   | 153.08 ns | 0.388 ns | 0.580 ns | 151.73 ns | 154.39 ns | 153.68 ns |  1.92 |    0.02 |   2,601 B |      - |         - |          NA |
| PlainSpanSwitch      | MediumRun-.NET 11.0 | .NET 11.0 | AllUpper   |  44.07 ns | 0.275 ns | 0.403 ns |  43.41 ns |  44.88 ns |  44.64 ns |  0.55 |    0.01 |   2,407 B |      - |         - |          NA |

## Earlier record

- Verdict: **converting the probe never pays - it loses in all 12 conditions measured.** For the match itself
  the crossover is at column count: the `Equals(OrdinalIgnoreCase)` chain wins at 8 columns (0.84x) and the
  plain ordinal switch wins at 24 (0.91x), both measured like for like
- Shape: resolve a DB reader's column names to ordinals under SQL identifier rules (OrdinalIgnoreCase). The
  generated form is a guarded `Equals(OrdinalIgnoreCase)` chain at 8 columns and a sampling-hash switch at 24;
  the alternative is to upper-case the probe first so a plain ordinal switch can be used
- Measured on x86-64-v4 (Zen 5, .NET 10.0.11). First pass: 4 classes x 5 variants x 2 casings = 40 cases. The
  24-column snake_case rows were re-measured after an unstable first run (100.93 ns +/- 11.37 -> 84.42 +/- 0.82),
  and the 8-column classes were re-run with a sixth variant after the harness fix below
- **The headline is not "IgnoreCase is expensive".** `string.Equals(name, "Id", OrdinalIgnoreCase)` is the
  cheapest thing here. What costs is the *conversion* introduced to avoid needing it

## The harness fix, and why the first reading of the 8-column rows was wrong

The 24-column classes always ran their baseline through the same harness as the variants
(`GeneratedHash() => ResolveByMatcher(names, static x => MatchHash(x))`), so those rows were like for like from
the start. **The 8-column classes did not:** `GeneratedChain() => ResolveChain(names)` is a direct call that
inlines into a 210-instruction body, while every variant went through `ResolveByMatcher` - a `Func<string,int>`
indirect call per column plus a non-inlined call into the matcher.

`ChainViaMatcher` was added to the two 8-column classes: the same chain, reached through the harness.

| 8 columns, AsDeclared | Chain, direct (inlined) | Chain, via the harness | Harness cost |
|---|---:|---:|---:|
| PascalCase | 8.427 ns | 25.895 ns | +17.5 ns (**2.18 ns/column**) |
| snake_case | 9.365 ns | 24.625 ns | +15.3 ns (**1.91 ns/column**) |

**The harness costs more than the difference it was hiding**, which is why the first pass read the plain switch
as 3.0-3.6x slower than the chain when the real figure is 1.17-1.19x. Any comparison where only some variants
pay a delegate call is worthless at this scale.

## 1. The match itself, like for like (all variants through the harness)

| Per column | 8 cols, Pascal | 8 cols, snake | 24 cols, Pascal | 24 cols, snake |
|---|---:|---:|---:|---:|
| `Equals(OrdinalIgnoreCase)` chain | **3.24 ns** | **3.08 ns** | - | - |
| Sampling-hash switch (generated at 24) | - | - | 3.55 ns | 3.61 ns |
| Plain ordinal switch, no case handling | 3.86 ns | 3.61 ns | **3.24 ns** | **3.52 ns** |

| Chain / hash vs plain ordinal switch | Ratio | CIs |
|---|---:|---|
| 8 cols, PascalCase | switch is **1.19x** slower | disjoint (25.43-26.36 vs 30.50-31.22 ns) |
| 8 cols, snake_case | switch is **1.17x** slower | disjoint (24.31-24.94 vs 28.61-29.10 ns) |
| 24 cols, PascalCase | switch is **0.91x** | disjoint |
| 24 cols, snake_case | switch is **0.98x** | overlap - a tie |

**The crossover is the chain's shape:** a chain checks the probe against the literals in declaration order, so
its cost per probe grows with the column count (about 4.5 comparisons on average at 8 columns, 12.5 at 24),
while a switch is flat - 3.24-3.86 ns per column at both sizes. That is exactly the boundary the catalog already
draws between a chain and a sampling-hash switch, now measured on both sides of it.

## 2. What the conversion costs (same harness, `AsDeclared`)

| Columns / naming | Plain switch (no conversion) | `Ascii.ToUpper` + span switch | `ToUpperInvariant` + span switch | `string.ToUpperInvariant()` + string switch |
|---|---:|---:|---:|---:|
| 8 / PascalCase | 30.86 ns | 52.59 (**1.70x**) | 58.07 (1.88x) | 77.38 (2.51x) + 304 B |
| 8 / snake_case | 28.86 ns | 54.09 (**1.87x**) | 59.62 (2.07x) | 88.07 (3.05x) + 328 B |
| 24 / PascalCase | 77.85 ns | 154.40 (**1.98x**) | 165.88 (2.13x) | 247.99 (3.19x) + 976 B |
| 24 / snake_case | 84.42 ns | 169.95 (**2.01x**) | 171.18 (2.03x) | 244.44 (2.90x) + 1,008 B |

- **The conversion costs 1.70-2.13x of the match itself** over a span, and 2.51-3.19x plus an allocation through
  `string.ToUpperInvariant()`. Against the generated form it is 2.0-3.0x at 8 columns and 1.35-2.91x at 24 -
  **12 of 12 conditions lose**
- The ASCII-specialised conversion is the cheapest of the three in every condition, consistent with TXT-06, but
  it only shaves 6-11% off the span-based one - not enough to change any decision
- `string.ToUpperInvariant()` allocates only when the input is not already upper case: 304-1,008 B under
  `AsDeclared`, **zero** under `AllUpper`, where the BCL returns the same instance. That is also why its
  `AllUpper` timings are so much better than its `AsDeclared` ones
- **`PlainSpanSwitch` resolves nothing under `AllUpper`** - it has no case handling, and Verify only checks it
  under `AsDeclared`. Those rows look fast because they bail out early and are excluded from every comparison

## What to weigh

1. **Keep the comparison case-insensitive. Never normalise the probe to make a case-sensitive switch usable.**
   It costs 1.70-2.13x of the match and loses to the generated form in every condition measured.
   `Equals(..., OrdinalIgnoreCase)` is the cheapest primitive in the whole table
2. **Pick the match shape by column count, which is what the catalog already says:** the chain wins at 8 columns
   (0.84x against a plain switch), the switch wins at 24 (0.91x against the sampling hash). The reason is
   structural - a chain is O(columns) per probe, a switch is flat - so the boundary moves with the count, not
   with the naming convention (PascalCase and snake_case agree within a few percent everywhere)
3. **Where case sensitivity is acceptable at 24+ columns, a plain ordinal switch is the better generated shape**:
   0.91x and a third less code (2,212 vs 3,261 B) against the sampling-hash form
4. **When a conversion is genuinely unavoidable, convert as few characters as possible** - upper-cased sampling
   plus an `OrdinalIgnoreCase` confirm touches 3 characters instead of all of them. Full-probe normalisation is
   structural cost: fold every character into a buffer, then read every character again in the switch
5. **Methodology: make every variant pay the same call shape before reading any ratio.** The harness asymmetry
   here was worth 1.9-2.2 ns per column and inverted the 8-column conclusion. Verify proved the variants
   returned equal results, which is not the same as proving they are equally *shaped*

## x86-64-v4 - 8 columns, PascalCase (with ChainViaMatcher)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedChain**       | **AsDeclared** |  **8.427 ns** | **0.2998 ns** | **0.4300 ns** |  **7.717 ns** |  **9.473 ns** |  **9.056 ns** |  **1.00** |    **0.07** |   **1,002 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 77.380 ns | 2.4477 ns | 3.4314 ns | 73.014 ns | 83.784 ns | 81.515 ns |  9.20 |    0.60 |   2,541 B | 0.0362 |     304 B |          NA |
| UpperSpanSwitch      | AsDeclared | 58.067 ns | 0.7419 ns | 1.1105 ns | 56.317 ns | 60.837 ns | 59.313 ns |  6.91 |    0.36 |   1,809 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 52.588 ns | 0.5470 ns | 0.8187 ns | 51.162 ns | 54.341 ns | 53.591 ns |  6.26 |    0.32 |   1,545 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 30.859 ns | 0.3600 ns | 0.5388 ns | 29.775 ns | 31.899 ns | 31.446 ns |  3.67 |    0.19 |     941 B |      - |         - |          NA |
| ChainViaMatcher      | AsDeclared | 25.895 ns | 0.4667 ns | 0.6986 ns | 24.754 ns | 27.334 ns | 26.686 ns |  3.08 |    0.17 |   1,053 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **AllUpper**   |  **9.169 ns** | **0.1516 ns** | **0.2270 ns** |  **8.784 ns** |  **9.672 ns** |  **9.401 ns** |  **1.00** |    **0.03** |     **999 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 42.874 ns | 0.7669 ns | 1.1478 ns | 40.637 ns | 45.321 ns | 44.239 ns |  4.68 |    0.17 |   2,199 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 58.331 ns | 0.6765 ns | 1.0126 ns | 56.625 ns | 60.252 ns | 59.539 ns |  6.37 |    0.19 |   1,796 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 52.334 ns | 0.7158 ns | 1.0714 ns | 50.766 ns | 54.993 ns | 53.566 ns |  5.71 |    0.18 |   1,592 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   | 27.849 ns | 0.3665 ns | 0.5486 ns | 26.953 ns | 29.408 ns | 28.424 ns |  3.04 |    0.09 |     921 B |      - |         - |          NA |
| ChainViaMatcher      | AllUpper   | 23.464 ns | 0.2681 ns | 0.3845 ns | 22.902 ns | 24.509 ns | 23.923 ns |  2.56 |    0.07 |   1,053 B |      - |         - |          NA |

## x86-64-v4 - 8 columns, snake_case (with ChainViaMatcher)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedChain**       | **AsDeclared** |  **9.365 ns** | **0.1327 ns** | **0.1904 ns** |  **9.388 ns** |  **8.989 ns** |  **9.618 ns** |  **9.575 ns** |  **1.00** |    **0.03** |   **1,024 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 88.072 ns | 1.2621 ns | 1.8891 ns | 87.892 ns | 83.206 ns | 91.934 ns | 90.735 ns |  9.41 |    0.27 |   2,516 B | 0.0391 |     328 B |          NA |
| UpperSpanSwitch      | AsDeclared | 59.621 ns | 0.7160 ns | 1.0716 ns | 59.413 ns | 58.034 ns | 62.388 ns | 60.892 ns |  6.37 |    0.17 |   1,799 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 54.087 ns | 0.8540 ns | 1.2248 ns | 53.627 ns | 52.369 ns | 56.403 ns | 55.772 ns |  5.78 |    0.17 |   1,563 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 28.858 ns | 0.2439 ns | 0.3575 ns | 28.742 ns | 28.289 ns | 29.620 ns | 29.354 ns |  3.08 |    0.07 |     959 B |      - |         - |          NA |
| ChainViaMatcher      | AsDeclared | 24.625 ns | 0.3184 ns | 0.4766 ns | 24.676 ns | 23.632 ns | 25.520 ns | 25.220 ns |  2.63 |    0.07 |   1,074 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **AllUpper**   |  **9.206 ns** | **0.2881 ns** | **0.4313 ns** |  **9.106 ns** |  **8.539 ns** |  **9.951 ns** |  **9.757 ns** |  **1.00** |    **0.07** |   **1,021 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 42.556 ns | 0.5993 ns | 0.8970 ns | 42.512 ns | 40.725 ns | 44.327 ns | 43.558 ns |  4.63 |    0.23 |   2,186 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 61.150 ns | 0.6623 ns | 0.9285 ns | 61.200 ns | 59.571 ns | 64.065 ns | 62.057 ns |  6.66 |    0.32 |   1,799 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 55.751 ns | 0.5540 ns | 0.8292 ns | 55.587 ns | 53.722 ns | 57.920 ns | 56.628 ns |  6.07 |    0.29 |   1,544 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   | 25.698 ns | 1.5517 ns | 2.3225 ns | 24.415 ns | 23.899 ns | 31.432 ns | 29.579 ns |  2.80 |    0.28 |     920 B |      - |         - |          NA |
| ChainViaMatcher      | AllUpper   | 21.374 ns | 0.3161 ns | 0.4533 ns | 21.247 ns | 20.695 ns | 22.630 ns | 21.950 ns |  2.33 |    0.12 |   1,074 B |      - |         - |          NA |

## x86-64-v4 - 24 columns, PascalCase

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedHash**        | **AsDeclared** |  **85.18 ns** | **1.021 ns** | **1.464 ns** |  **83.59 ns** |  **89.98 ns** |  **87.48 ns** |  **1.00** |    **0.02** |   **3,261 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 247.99 ns | 3.077 ns | 4.606 ns | 241.34 ns | 259.40 ns | 255.39 ns |  2.91 |    0.07 |   3,794 B | 0.1166 |     976 B |          NA |
| UpperSpanSwitch      | AsDeclared | 165.88 ns | 0.842 ns | 1.261 ns | 163.72 ns | 168.22 ns | 167.38 ns |  1.95 |    0.04 |   3,026 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 154.40 ns | 1.574 ns | 2.257 ns | 150.94 ns | 157.63 ns | 157.06 ns |  1.81 |    0.04 |   2,812 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared |  77.85 ns | 0.307 ns | 0.441 ns |  77.00 ns |  79.11 ns |  78.27 ns |  0.91 |    0.02 |   2,212 B |      - |         - |          NA |
|                      |            |           |          |          |           |           |           |       |         |           |        |           |             |
| **GeneratedHash**        | **AllUpper**   |  **87.41 ns** | **1.274 ns** | **1.867 ns** |  **84.04 ns** |  **89.69 ns** |  **89.49 ns** |  **1.00** |    **0.03** |   **3,314 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 118.21 ns | 1.026 ns | 1.439 ns | 115.12 ns | 120.43 ns | 119.72 ns |  1.35 |    0.03 |   3,462 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 168.52 ns | 1.124 ns | 1.647 ns | 165.05 ns | 172.45 ns | 170.59 ns |  1.93 |    0.04 |   3,019 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 154.08 ns | 1.427 ns | 2.092 ns | 150.03 ns | 160.08 ns | 156.39 ns |  1.76 |    0.04 |   2,819 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   |  81.97 ns | 0.706 ns | 1.035 ns |  80.37 ns |  84.17 ns |  83.46 ns |  0.94 |    0.02 |   2,174 B |      - |         - |          NA |

## x86-64-v4 - 24 columns, snake_case (run 1, the unstable PlainSpanSwitch row)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedHash**        | **AsDeclared** |  **85.23 ns** |  **2.862 ns** |  **4.011 ns** |  **83.07 ns** |  **81.37 ns** |  **93.30 ns** |  **90.78 ns** |  **1.00** |    **0.06** |   **3,294 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 244.44 ns |  2.463 ns |  3.687 ns | 243.16 ns | 239.53 ns | 253.45 ns | 249.87 ns |  2.87 |    0.14 |   3,814 B | 0.1202 |    1008 B |          NA |
| UpperSpanSwitch      | AsDeclared | 171.18 ns |  3.203 ns |  4.593 ns | 169.88 ns | 164.95 ns | 178.73 ns | 176.93 ns |  2.01 |    0.10 |   3,028 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 169.95 ns |  5.320 ns |  7.102 ns | 166.58 ns | 160.95 ns | 184.68 ns | 181.42 ns |  2.00 |    0.12 |   2,825 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 100.93 ns | 11.372 ns | 17.022 ns |  90.28 ns |  84.61 ns | 129.06 ns | 124.65 ns |  1.19 |    0.20 |   2,218 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedHash**        | **AllUpper**   |  **87.44 ns** |  **1.152 ns** |  **1.652 ns** |  **87.53 ns** |  **84.79 ns** |  **90.55 ns** |  **89.69 ns** |  **1.00** |    **0.03** |   **3,254 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 124.78 ns |  2.714 ns |  3.979 ns | 124.00 ns | 120.32 ns | 136.20 ns | 128.22 ns |  1.43 |    0.05 |   3,486 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 175.63 ns |  2.046 ns |  2.869 ns | 175.06 ns | 172.44 ns | 185.30 ns | 179.24 ns |  2.01 |    0.05 |   3,045 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 165.24 ns |  2.645 ns |  3.877 ns | 164.06 ns | 161.12 ns | 178.61 ns | 170.08 ns |  1.89 |    0.06 |   2,825 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   |  84.21 ns |  1.047 ns |  1.535 ns |  83.66 ns |  82.42 ns |  87.80 ns |  86.31 ns |  0.96 |    0.02 |   2,241 B |      - |         - |          NA |

## x86-64-v4 - 24 columns, snake_case (run 2, GeneratedHash / PlainSpanSwitch only)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Casing     | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **GeneratedHash**   | **AsDeclared** | **86.59 ns** | **2.146 ns** | **3.078 ns** | **82.70 ns** | **92.11 ns** | **90.20 ns** |  **1.00** |    **0.05** |   **3,265 B** |         **-** |          **NA** |
| PlainSpanSwitch | AsDeclared | 84.42 ns | 0.818 ns | 1.224 ns | 82.37 ns | 87.22 ns | 85.76 ns |  0.98 |    0.04 |   2,221 B |         - |          NA |
|                 |            |          |          |          |          |          |          |       |         |           |           |             |
| **GeneratedHash**   | **AllUpper**   | **86.37 ns** | **0.901 ns** | **1.263 ns** | **83.78 ns** | **88.73 ns** | **87.53 ns** |  **1.00** |    **0.02** |   **3,324 B** |         **-** |          **NA** |
| PlainSpanSwitch | AllUpper   | 78.06 ns | 1.100 ns | 1.542 ns | 75.91 ns | 82.37 ns | 79.40 ns |  0.90 |    0.02 |   2,248 B |         - |          NA |
