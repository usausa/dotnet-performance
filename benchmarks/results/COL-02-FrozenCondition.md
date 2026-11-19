# COL-02: FrozenDictionary adoption conditions (string keys)

## net11 / net10 re-verification of the build side (2026-11-18, HX 370)

- Verdict: unchanged. Building a `FrozenDictionary` is not cheaper on net11
- Build ratio against `Dictionary`: 9.4 / 7.6 / 5.4x on net10 and 10.7 / 8.3 / 5.5x on net11 (16 / 256 / 1024 entries);
  allocation is identical on both (2,584 / 35,416 / 122,344 B)
- The net11 pre-sizing of the temporary dictionary (#128300) applies to one source shape only: a count-known
  sequence of `KeyValuePair` passed without selectors (803 → 495 KB and 217 → 191 μs at 4,096 entries). The selector
  overload used by `FrozenBuildBenchmark` already allocated that little on net10, and a sequence of unknown count
  (an iterator) is unchanged. See "Build by source shape" below
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

### Build by source shape (FrozenBuildSourceBenchmark)

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
| Method                | Job                 | Runtime   | Count | Mean       | Error     | StdDev     | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0    | Gen1    | Gen2    | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |------ |-----------:|----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|--------:|--------:|--------:|----------:|------------:|
| **ArrayWithSelectors**    | **.NET 10.0**           | **.NET 10.0** | **256**   |  **10.318 μs** | **0.2884 μs** |  **0.4227 μs** |  **10.068 μs** |   **9.770 μs** |  **10.946 μs** |  **10.801 μs** |  **1.00** |    **0.00** |  **14,451 B** |  **4.2267** |  **0.2441** |       **-** |  **34.59 KB** |        **1.00** |
| ArrayOfPairs          | .NET 10.0           | .NET 10.0 | 256   |  10.801 μs | 0.0773 μs |  0.1084 μs |  10.812 μs |  10.643 μs |  11.001 μs |  10.938 μs |  1.05 |    0.04 |   5,687 B |  5.9052 |  0.3357 |       - |  48.23 KB |        1.39 |
| IteratorWithSelectors | .NET 10.0           | .NET 10.0 | 256   |  13.723 μs | 0.2035 μs |  0.2853 μs |  13.685 μs |  13.333 μs |  14.641 μs |  13.954 μs |  1.33 |    0.06 |  10,155 B |  5.9052 |  0.3815 |       - |   48.3 KB |        1.40 |
| IteratorOfPairs       | .NET 10.0           | .NET 10.0 | 256   |  10.981 μs | 0.1291 μs |  0.1892 μs |  10.903 μs |  10.717 μs |  11.281 μs |  11.259 μs |  1.07 |    0.05 |   5,901 B |  5.9052 |  0.3357 |       - |  48.23 KB |        1.39 |
| FromDictionary        | .NET 10.0           | .NET 10.0 | 256   |   8.970 μs | 0.1267 μs |  0.1858 μs |   8.998 μs |   8.734 μs |   9.411 μs |   9.237 μs |  0.87 |    0.04 |   4,030 B |  3.2196 |  0.1221 |       - |   26.4 KB |        0.76 |
|                       |                     |           |       |            |           |            |            |            |            |            |       |         |           |         |         |         |           |             |
| ArrayWithSelectors    | MediumRun-.NET 11.0 | .NET 11.0 | 256   |  10.243 μs | 0.0486 μs |  0.0665 μs |  10.248 μs |  10.124 μs |  10.339 μs |  10.325 μs |  1.00 |    0.00 |   5,730 B |  4.2267 |       - |       - |  34.59 KB |        1.00 |
| ArrayOfPairs          | MediumRun-.NET 11.0 | .NET 11.0 | 256   |  10.775 μs | 0.2074 μs |  0.2908 μs |  10.957 μs |  10.380 μs |  11.129 μs |  11.086 μs |  1.05 |    0.03 |   5,166 B |  4.2267 |  0.2441 |       - |  34.59 KB |        1.00 |
| IteratorWithSelectors | MediumRun-.NET 11.0 | .NET 11.0 | 256   |  13.158 μs | 0.0605 μs |  0.0906 μs |  13.159 μs |  13.008 μs |  13.317 μs |  13.274 μs |  1.28 |    0.01 |   9,627 B |  5.9052 |       - |       - |   48.3 KB |        1.40 |
| IteratorOfPairs       | MediumRun-.NET 11.0 | .NET 11.0 | 256   |  11.412 μs | 0.0756 μs |  0.1108 μs |  11.420 μs |  11.183 μs |  11.630 μs |  11.531 μs |  1.11 |    0.01 |   5,698 B |  5.9052 |       - |       - |   48.3 KB |        1.40 |
| FromDictionary        | MediumRun-.NET 11.0 | .NET 11.0 | 256   |   9.259 μs | 0.1832 μs |  0.2686 μs |   9.438 μs |   8.875 μs |   9.607 μs |   9.545 μs |  0.90 |    0.03 |   1,181 B |  3.2349 |       - |       - |   26.4 KB |        0.76 |
|                       |                     |           |       |            |           |            |            |            |            |            |       |         |           |         |         |         |           |             |
| **ArrayWithSelectors**    | **.NET 10.0**           | **.NET 10.0** | **4096**  | **185.782 μs** | **2.1131 μs** |  **2.9623 μs** | **186.108 μs** | **180.826 μs** | **191.101 μs** | **189.833 μs** |  **1.00** |    **0.00** |  **13,624 B** | **73.7305** | **36.8652** | **36.8652** | **495.46 KB** |        **1.00** |
| ArrayOfPairs          | .NET 10.0           | .NET 10.0 | 4096  | 217.128 μs | 3.6190 μs |  5.3047 μs | 215.405 μs | 209.488 μs | 225.692 μs | 223.514 μs |  1.17 |    0.03 |   4,654 B | 90.8203 | 90.8203 | 90.8203 | 803.34 KB |        1.62 |
| IteratorWithSelectors | .NET 10.0           | .NET 10.0 | 4096  | 268.968 μs | 2.8363 μs |  4.0678 μs | 267.355 μs | 263.951 μs | 281.492 μs | 273.387 μs |  1.45 |    0.03 |   9,889 B | 90.8203 | 90.8203 | 90.8203 | 803.41 KB |        1.62 |
| IteratorOfPairs       | .NET 10.0           | .NET 10.0 | 4096  | 224.484 μs | 1.1060 μs |  1.5505 μs | 224.280 μs | 221.681 μs | 227.868 μs | 226.199 μs |  1.21 |    0.02 |   4,895 B | 90.8203 | 90.8203 | 90.8203 | 803.34 KB |        1.62 |
| FromDictionary        | .NET 10.0           | .NET 10.0 | 4096  | 115.730 μs | 0.8124 μs |  1.1651 μs | 115.507 μs | 114.028 μs | 119.588 μs | 116.821 μs |  0.62 |    0.01 |   3,671 B | 44.1895 | 11.7188 |       - |  362.3 KB |        0.73 |
|                       |                     |           |       |            |           |            |            |            |            |            |       |         |           |         |         |         |           |             |
| ArrayWithSelectors    | MediumRun-.NET 11.0 | .NET 11.0 | 4096  | 184.222 μs | 1.4743 μs |  2.1144 μs | 183.811 μs | 181.524 μs | 189.291 μs | 187.373 μs |  1.00 |    0.00 |   7,419 B | 73.7305 | 36.8652 | 36.8652 | 495.46 KB |        1.00 |
| ArrayOfPairs          | MediumRun-.NET 11.0 | .NET 11.0 | 4096  | 191.164 μs | 1.1369 μs |  1.6665 μs | 190.970 μs | 188.505 μs | 195.349 μs | 193.200 μs |  1.04 |    0.01 |   5,147 B | 73.7305 | 36.8652 | 36.8652 | 495.46 KB |        1.00 |
| IteratorWithSelectors | MediumRun-.NET 11.0 | .NET 11.0 | 4096  | 268.605 μs | 1.1125 μs |  1.5596 μs | 268.140 μs | 266.647 μs | 272.897 μs | 270.631 μs |  1.46 |    0.02 |  10,369 B | 90.8203 | 90.8203 | 90.8203 | 803.41 KB |        1.62 |
| IteratorOfPairs       | MediumRun-.NET 11.0 | .NET 11.0 | 4096  | 254.541 μs | 7.3656 μs | 10.5635 μs | 253.483 μs | 239.579 μs | 268.598 μs | 267.077 μs |  1.38 |    0.06 |   6,143 B | 90.8203 | 90.8203 | 90.8203 | 803.41 KB |        1.62 |
| FromDictionary        | MediumRun-.NET 11.0 | .NET 11.0 | 4096  | 126.972 μs | 2.1018 μs |  3.0143 μs | 127.850 μs | 122.804 μs | 132.339 μs | 131.137 μs |  0.69 |    0.02 |   4,366 B | 44.1895 | 11.7188 |       - |  362.3 KB |        0.73 |

### Lookup side (FrozenLookupBenchmark)

- `Dictionary` got 5-8% faster on net11 and `FrozenDictionary` did not, so Frozen is 1.06 / 1.05 / **1.22x** of Dictionary at 16 / 256 / 1024 entries (1.03 / 0.99 / 1.12x on net10). The rejection reason gets stronger

#### FrozenLookupBenchmark

net10 → net11 per row:

| Method | Count | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| LookupDictionary | 16 | 72.75 ns | 68.70 ns | 0.94 | - → - | 1,200 B → 1,159 B |
| LookupFrozen | 16 | 74.85 ns | 72.90 ns | 0.97 | - → - | 956 B → 905 B |
| LookupDictionary | 256 | 1,282.11 ns | 1,219.11 ns | 0.95 | - → - | 1,203 B → 1,181 B |
| LookupFrozen | 256 | 1,273.76 ns | 1,281.95 ns | 1.01 | - → - | 945 B → 905 B |
| LookupDictionary | 1024 | 5,118.06 ns | 4,709.55 ns | 0.92 | - → - | 1,186 B → 1,154 B |
| LookupFrozen | 1024 | 5,720.62 ns | 5,729.87 ns | 1.00 | - → - | 948 B → 908 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.81 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Count | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| **LookupDictionary** | **.NET 10.0**           | **.NET 10.0** | **16**    |    **72.75 ns** |  **0.516 ns** |  **0.772 ns** |    **71.16 ns** |    **74.38 ns** |    **73.77 ns** |  **1.00** |    **0.00** |   **1,200 B** |         **-** |          **NA** |
| LookupFrozen     | .NET 10.0           | .NET 10.0 | 16    |    74.85 ns |  0.840 ns |  1.149 ns |    72.93 ns |    76.63 ns |    76.07 ns |  1.03 |    0.02 |     956 B |         - |          NA |
|                  |                     |           |       |             |           |           |             |             |             |       |         |           |           |             |
| LookupDictionary | MediumRun-.NET 11.0 | .NET 11.0 | 16    |    68.70 ns |  0.288 ns |  0.414 ns |    67.90 ns |    69.70 ns |    69.14 ns |  1.00 |    0.00 |   1,159 B |         - |          NA |
| LookupFrozen     | MediumRun-.NET 11.0 | .NET 11.0 | 16    |    72.90 ns |  0.451 ns |  0.661 ns |    72.07 ns |    74.92 ns |    73.66 ns |  1.06 |    0.01 |     905 B |         - |          NA |
|                  |                     |           |       |             |           |           |             |             |             |       |         |           |           |             |
| **LookupDictionary** | **.NET 10.0**           | **.NET 10.0** | **256**   | **1,282.11 ns** | **16.669 ns** | **24.949 ns** | **1,249.76 ns** | **1,335.75 ns** | **1,316.59 ns** |  **1.00** |    **0.00** |   **1,203 B** |         **-** |          **NA** |
| LookupFrozen     | .NET 10.0           | .NET 10.0 | 256   | 1,273.76 ns |  6.719 ns | 10.056 ns | 1,260.21 ns | 1,305.20 ns | 1,285.58 ns |  0.99 |    0.02 |     945 B |         - |          NA |
|                  |                     |           |       |             |           |           |             |             |             |       |         |           |           |             |
| LookupDictionary | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 1,219.11 ns |  4.012 ns |  5.355 ns | 1,206.35 ns | 1,226.31 ns | 1,224.19 ns |  1.00 |    0.00 |   1,181 B |         - |          NA |
| LookupFrozen     | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 1,281.95 ns |  7.866 ns | 11.774 ns | 1,265.65 ns | 1,304.10 ns | 1,299.98 ns |  1.05 |    0.01 |     905 B |         - |          NA |
|                  |                     |           |       |             |           |           |             |             |             |       |         |           |           |             |
| **LookupDictionary** | **.NET 10.0**           | **.NET 10.0** | **1024**  | **5,118.06 ns** | **30.208 ns** | **44.278 ns** | **5,040.66 ns** | **5,218.52 ns** | **5,197.27 ns** |  **1.00** |    **0.00** |   **1,186 B** |         **-** |          **NA** |
| LookupFrozen     | .NET 10.0           | .NET 10.0 | 1024  | 5,720.62 ns | 29.095 ns | 42.647 ns | 5,649.06 ns | 5,795.08 ns | 5,775.08 ns |  1.12 |    0.01 |     948 B |         - |          NA |
|                  |                     |           |       |             |           |           |             |             |             |       |         |           |           |             |
| LookupDictionary | MediumRun-.NET 11.0 | .NET 11.0 | 1024  | 4,709.55 ns | 25.533 ns | 37.426 ns | 4,661.26 ns | 4,822.71 ns | 4,746.97 ns |  1.00 |    0.00 |   1,154 B |         - |          NA |
| LookupFrozen     | MediumRun-.NET 11.0 | .NET 11.0 | 1024  | 5,729.87 ns | 24.720 ns | 36.234 ns | 5,660.10 ns | 5,798.92 ns | 5,765.44 ns |  1.22 |    0.01 |     908 B |         - |          NA |

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
