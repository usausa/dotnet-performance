# COL-06: Collection conversion shape specialization

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - conditional. ToImmutableArray stays the fastest from a contiguous source; MoveToImmutable is 2.39x / 5.63x and ToImmutable 3.05x / 6.85x of it at 16 / 256 on net11 (2.40x / 5.92x and 3.04x / 6.92x on net10)
- List reuse: SetCount + span is 0.17x / 0.28x and Clear 0.52x / 0.82x of a new List on net11 (0.16x / 0.27x and 0.57x / 0.76x on net10)
- `new List<int>()` + Add compiles to 145 B on net11 instead of 1,723 B (Add is no longer inlined into the loop) with no time change

### ImmutableBuildBenchmark

net10 → net11 per row:

| Method | Count | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| ToImmutableArrayExtension | 16 | 4.963 ns | 4.993 ns | 1.01 | 88 B → 88 B | 66 B → 72 B |
| BuilderToImmutable | 16 | 15.075 ns | 15.247 ns | 1.01 | 176 B → 176 B | 2,909 B → 3,451 B |
| BuilderMoveToImmutable | 16 | 11.911 ns | 11.939 ns | 1.00 | 88 B → 88 B | 915 B → 1,106 B |
| ToImmutableArrayExtension | 256 | 25.447 ns | 26.346 ns | 1.04 | 1048 B → 1048 B | 66 B → 72 B |
| BuilderToImmutable | 256 | 176.173 ns | 180.371 ns | 1.02 | 2096 B → 2096 B | 2,884 B → 3,455 B |
| BuilderMoveToImmutable | 256 | 150.610 ns | 148.268 ns | 0.98 | 1048 B → 1048 B | 915 B → 1,105 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.09 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                    | Job                 | Runtime   | Count | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |-------------------- |---------- |------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| **ToImmutableArrayExtension** | **.NET 10.0**           | **.NET 10.0** | **16**    |   **4.963 ns** | **0.0816 ns** | **0.1196 ns** |   **4.777 ns** |   **5.103 ns** |   **5.087 ns** |  **1.00** |    **0.00** |      **66 B** | **0.0105** |      **-** |      **88 B** |        **1.00** |
| BuilderToImmutable        | .NET 10.0           | .NET 10.0 | 16    |  15.075 ns | 0.0559 ns | 0.0819 ns |  14.952 ns |  15.267 ns |  15.172 ns |  3.04 |    0.07 |   2,909 B | 0.0210 |      - |     176 B |        2.00 |
| BuilderMoveToImmutable    | .NET 10.0           | .NET 10.0 | 16    |  11.911 ns | 0.0534 ns | 0.0767 ns |  11.771 ns |  12.091 ns |  12.010 ns |  2.40 |    0.06 |     915 B | 0.0105 |      - |      88 B |        1.00 |
|                           |                     |           |       |            |           |           |            |            |            |       |         |           |        |        |           |             |
| ToImmutableArrayExtension | MediumRun-.NET 11.0 | .NET 11.0 | 16    |   4.993 ns | 0.0408 ns | 0.0598 ns |   4.924 ns |   5.166 ns |   5.059 ns |  1.00 |    0.00 |      72 B | 0.0105 |      - |      88 B |        1.00 |
| BuilderToImmutable        | MediumRun-.NET 11.0 | .NET 11.0 | 16    |  15.247 ns | 0.0942 ns | 0.1351 ns |  15.057 ns |  15.663 ns |  15.416 ns |  3.05 |    0.04 |   3,451 B | 0.0210 |      - |     176 B |        2.00 |
| BuilderMoveToImmutable    | MediumRun-.NET 11.0 | .NET 11.0 | 16    |  11.939 ns | 0.0550 ns | 0.0789 ns |  11.802 ns |  12.094 ns |  12.025 ns |  2.39 |    0.03 |   1,106 B | 0.0105 |      - |      88 B |        1.00 |
|                           |                     |           |       |            |           |           |            |            |            |       |         |           |        |        |           |             |
| **ToImmutableArrayExtension** | **.NET 10.0**           | **.NET 10.0** | **256**   |  **25.447 ns** | **0.2790 ns** | **0.4002 ns** |  **24.673 ns** |  **26.622 ns** |  **25.972 ns** |  **1.00** |    **0.00** |      **66 B** | **0.1253** |      **-** |    **1048 B** |        **1.00** |
| BuilderToImmutable        | .NET 10.0           | .NET 10.0 | 256   | 176.173 ns | 0.7822 ns | 1.1219 ns | 174.296 ns | 178.414 ns | 177.698 ns |  6.92 |    0.11 |   2,884 B | 0.2506 |      - |    2096 B |        2.00 |
| BuilderMoveToImmutable    | .NET 10.0           | .NET 10.0 | 256   | 150.610 ns | 2.0688 ns | 3.0965 ns | 147.507 ns | 158.660 ns | 154.685 ns |  5.92 |    0.15 |     915 B | 0.1252 |      - |    1048 B |        1.00 |
|                           |                     |           |       |            |           |           |            |            |            |       |         |           |        |        |           |             |
| ToImmutableArrayExtension | MediumRun-.NET 11.0 | .NET 11.0 | 256   |  26.346 ns | 0.3185 ns | 0.4768 ns |  25.732 ns |  27.810 ns |  27.215 ns |  1.00 |    0.00 |      72 B | 0.1253 |      - |    1048 B |        1.00 |
| BuilderToImmutable        | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 180.371 ns | 1.2400 ns | 1.8176 ns | 176.440 ns | 183.725 ns | 182.416 ns |  6.85 |    0.14 |   3,455 B | 0.2506 | 0.0005 |    2096 B |        2.00 |
| BuilderMoveToImmutable    | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 148.268 ns | 0.7975 ns | 1.0916 ns | 146.384 ns | 151.657 ns | 149.027 ns |  5.63 |    0.11 |   1,105 B | 0.1252 |      - |    1048 B |        1.00 |

### ListReuseBenchmark

net10 → net11 per row:

| Method | Count | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| NewListNoCapacity | 16 | 33.238 ns | 31.838 ns | 0.96 | 216 B → 216 B | 1,723 B → 145 B |
| NewListWithCapacity | 16 | 32.885 ns | 34.411 ns | 1.05 | 120 B → 120 B | 200 B → 212 B |
| ReuseWithClear | 16 | 18.892 ns | 16.488 ns | 0.87 | - → - | 1,773 B → 1,763 B |
| ReuseWithSetCountSpan | 16 | 5.357 ns | 5.348 ns | 1.00 | - → - | 1,738 B → 1,750 B |
| NewListNoCapacity | 256 | 264.272 ns | 258.820 ns | 0.98 | 2232 B → 2232 B | 1,723 B → 145 B |
| NewListWithCapacity | 256 | 211.251 ns | 199.039 ns | 0.94 | 1080 B → 1080 B | 200 B → 212 B |
| ReuseWithClear | 256 | 201.812 ns | 212.739 ns | 1.05 | - → - | 1,732 B → 1,763 B |
| ReuseWithSetCountSpan | 256 | 71.075 ns | 71.271 ns | 1.00 | - → - | 1,738 B → 1,750 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.1 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Count | Mean       | Error      | StdDev     | Median     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **NewListNoCapacity**     | **.NET 10.0**           | **.NET 10.0** | **16**    |  **33.238 ns** |  **1.2615 ns** |  **1.8491 ns** |  **33.884 ns** |  **30.755 ns** |  **35.926 ns** |  **35.460 ns** |  **1.00** |    **0.00** | **0.0258** |   **1,723 B** |      **-** |     **216 B** |        **1.00** |
| NewListWithCapacity   | .NET 10.0           | .NET 10.0 | 16    |  32.885 ns |  0.4189 ns |  0.6269 ns |  32.742 ns |  31.962 ns |  34.032 ns |  33.845 ns |  0.99 |    0.06 | 0.0143 |     200 B |      - |     120 B |        0.56 |
| ReuseWithClear        | .NET 10.0           | .NET 10.0 | 16    |  18.892 ns |  1.4368 ns |  2.1505 ns |  18.923 ns |  16.097 ns |  21.493 ns |  21.249 ns |  0.57 |    0.07 |      - |   1,773 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | .NET 10.0           | .NET 10.0 | 16    |   5.357 ns |  0.0364 ns |  0.0523 ns |   5.337 ns |   5.290 ns |   5.473 ns |   5.429 ns |  0.16 |    0.01 |      - |   1,738 B |      - |         - |        0.00 |
|                       |                     |           |       |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| NewListNoCapacity     | MediumRun-.NET 11.0 | .NET 11.0 | 16    |  31.838 ns |  1.5500 ns |  2.2719 ns |  30.363 ns |  29.319 ns |  35.650 ns |  34.518 ns |  1.00 |    0.00 | 0.0258 |     145 B |      - |     216 B |        1.00 |
| NewListWithCapacity   | MediumRun-.NET 11.0 | .NET 11.0 | 16    |  34.411 ns |  1.1709 ns |  1.7525 ns |  34.616 ns |  26.692 ns |  36.836 ns |  35.547 ns |  1.09 |    0.09 | 0.0143 |     212 B |      - |     120 B |        0.56 |
| ReuseWithClear        | MediumRun-.NET 11.0 | .NET 11.0 | 16    |  16.488 ns |  0.3206 ns |  0.4699 ns |  16.525 ns |  15.449 ns |  17.724 ns |  17.099 ns |  0.52 |    0.04 |      - |   1,763 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | MediumRun-.NET 11.0 | .NET 11.0 | 16    |   5.348 ns |  0.0209 ns |  0.0313 ns |   5.344 ns |   5.301 ns |   5.412 ns |   5.389 ns |  0.17 |    0.01 |      - |   1,750 B |      - |         - |        0.00 |
|                       |                     |           |       |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| **NewListNoCapacity**     | **.NET 10.0**           | **.NET 10.0** | **256**   | **264.272 ns** |  **2.3850 ns** |  **3.5698 ns** | **263.249 ns** | **258.863 ns** | **270.303 ns** | **269.117 ns** |  **1.00** |    **0.00** | **0.2666** |   **1,723 B** | **0.0010** |    **2232 B** |        **1.00** |
| NewListWithCapacity   | .NET 10.0           | .NET 10.0 | 256   | 211.251 ns | 11.1253 ns | 16.3073 ns | 196.998 ns | 194.523 ns | 230.233 ns | 228.919 ns |  0.80 |    0.06 | 0.1290 |     200 B | 0.0005 |    1080 B |        0.48 |
| ReuseWithClear        | .NET 10.0           | .NET 10.0 | 256   | 201.812 ns |  0.4360 ns |  0.6252 ns | 201.691 ns | 200.455 ns | 203.024 ns | 202.493 ns |  0.76 |    0.01 |      - |   1,732 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | .NET 10.0           | .NET 10.0 | 256   |  71.075 ns |  3.5662 ns |  4.9993 ns |  74.815 ns |  65.112 ns |  76.547 ns |  75.965 ns |  0.27 |    0.02 |      - |   1,738 B |      - |         - |        0.00 |
|                       |                     |           |       |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| NewListNoCapacity     | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 258.820 ns |  1.2714 ns |  1.8636 ns | 258.457 ns | 254.779 ns | 262.772 ns | 261.375 ns |  1.00 |    0.00 | 0.2666 |     145 B | 0.0010 |    2232 B |        1.00 |
| NewListWithCapacity   | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 199.039 ns |  2.9087 ns |  4.3536 ns | 197.456 ns | 195.496 ns | 211.922 ns | 205.664 ns |  0.77 |    0.02 | 0.1290 |     212 B | 0.0005 |    1080 B |        0.48 |
| ReuseWithClear        | MediumRun-.NET 11.0 | .NET 11.0 | 256   | 212.739 ns |  0.3720 ns |  0.5335 ns | 212.731 ns | 211.617 ns | 213.852 ns | 213.444 ns |  0.82 |    0.01 |      - |   1,763 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | MediumRun-.NET 11.0 | .NET 11.0 | 256   |  71.271 ns |  4.6098 ns |  6.8997 ns |  71.642 ns |  62.586 ns |  78.849 ns |  78.614 ns |  0.28 |    0.03 |      - |   1,750 B |      - |         - |        0.00 |

## Earlier record

## ImmutableArray construction

- Verdict: conditional
- Contiguous source (array/span): ToImmutableArray wins outright (4.0 ns / 88 B at 16; builder paths 2.9-3.6x slower) - bulk copy beats per-element Add
- Incremental build: MoveToImmutable vs ToImmutable = 11.3 ns/88 B vs 14.3 ns/176 B at 16, 171 vs 203 ns at 256 (non-overlapping CIs) - halves allocation and saves the final copy at every size
- Rule: know your source shape; only reach for Builder when elements arrive one by one, and size the builder exactly to use MoveToImmutable

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Count | Mean       | Error     | StdDev     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------ |-----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **ToImmutableArrayExtension** | **16**    |   **3.956 ns** | **0.0365 ns** |  **0.0500 ns** |   **3.842 ns** |   **4.054 ns** |   **4.016 ns** |  **1.00** |    **0.02** | **0.0105** |     **706 B** |      **-** |      **88 B** |        **1.00** |
| BuilderToImmutable        | 16    |  14.338 ns | 0.2332 ns |  0.3490 ns |  13.893 ns |  15.301 ns |  14.836 ns |  3.63 |    0.10 | 0.0210 |   2,033 B |      - |     176 B |        2.00 |
| BuilderMoveToImmutable    | 16    |  11.310 ns | 0.1517 ns |  0.2076 ns |  11.074 ns |  11.777 ns |  11.594 ns |  2.86 |    0.06 | 0.0105 |     891 B |      - |      88 B |        1.00 |
|                           |       |            |           |            |            |            |            |       |         |        |           |        |           |             |
| **ToImmutableArrayExtension** | **256**   |  **23.111 ns** | **0.3207 ns** |  **0.4599 ns** |  **22.484 ns** |  **24.695 ns** |  **23.613 ns** |  **1.00** |    **0.03** | **0.1253** |     **710 B** |      **-** |    **1048 B** |        **1.00** |
| BuilderToImmutable        | 256   | 202.878 ns | 8.9281 ns | 12.8044 ns | 185.504 ns | 224.524 ns | 220.133 ns |  8.78 |    0.57 | 0.2506 |   2,035 B | 0.0005 |    2096 B |        2.00 |
| BuilderMoveToImmutable    | 256   | 170.600 ns | 1.3788 ns |  1.9329 ns | 166.529 ns | 174.476 ns | 172.755 ns |  7.38 |    0.16 | 0.1252 |     891 B |      - |    1048 B |        1.00 |

## List reuse and fill strategy

- Verdict: adopted
- new List(capacity): 0.51x vs no-capacity at 16 elements; reuse with Clear+EnsureCapacity: zero allocation
- Reuse + SetCount + AsSpan fill (COL-01): 0.19x (16) / 0.26x (256) with zero allocation - the fastest fill path

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Count | Mean       | Error      | StdDev     | Median     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **NewListNoCapacity**     | **16**    |  **30.449 ns** |  **1.0377 ns** |  **1.4204 ns** |  **30.429 ns** |  **28.326 ns** |  **33.909 ns** |  **32.355 ns** |  **1.00** |    **0.06** | **0.0258** |   **1,901 B** |      **-** |     **216 B** |        **1.00** |
| NewListWithCapacity   | 16    |  15.578 ns |  0.1371 ns |  0.1830 ns |  15.604 ns |  15.019 ns |  15.946 ns |  15.696 ns |  0.51 |    0.02 | 0.0143 |     303 B |      - |     120 B |        0.56 |
| ReuseWithClear        | 16    |  19.111 ns |  0.2780 ns |  0.4161 ns |  19.123 ns |  17.610 ns |  19.929 ns |  19.583 ns |  0.63 |    0.03 |      - |     463 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | 16    |   5.681 ns |  0.0767 ns |  0.1024 ns |   5.654 ns |   5.602 ns |   6.076 ns |   5.721 ns |  0.19 |    0.01 |      - |     360 B |      - |         - |        0.00 |
|                       |       |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| **NewListNoCapacity**     | **256**   | **296.438 ns** | **23.9837 ns** | **34.3968 ns** | **269.638 ns** | **255.026 ns** | **337.229 ns** | **333.489 ns** |  **1.01** |    **0.16** | **0.2666** |   **1,891 B** | **0.0010** |    **2232 B** |        **1.00** |
| NewListWithCapacity   | 256   | 237.432 ns |  1.7959 ns |  2.3975 ns | 237.868 ns | 228.213 ns | 241.572 ns | 239.297 ns |  0.81 |    0.09 | 0.1290 |     303 B |      - |    1080 B |        0.48 |
| ReuseWithClear        | 256   | 204.001 ns |  2.1630 ns |  3.1021 ns | 203.247 ns | 200.688 ns | 212.034 ns | 208.527 ns |  0.70 |    0.08 |      - |     463 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | 256   |  76.383 ns |  0.8630 ns |  1.1812 ns |  76.190 ns |  74.876 ns |  79.829 ns |  77.677 ns |  0.26 |    0.03 |      - |     360 B |      - |         - |        0.00 |
