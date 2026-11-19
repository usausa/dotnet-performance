# BIT-04: XxHash3 vs string.GetHashCode vs FNV-1a

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted
- XxHash3 got faster on net11 (0.81-0.95x of net10, code 3-17% smaller): 0.37x / 0.20x / 0.07-0.08x of `string.GetHashCode` at 8 / 64 / 512 chars
- The sampling hash is 1.10-1.13x slower on net11 (1.01 → 1.13 ns) with smaller code (79 → 73 B); it is still the fastest by far

### HashCompareBenchmark

net10 → net11 per row:

| Method | Length | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| StringGetHashCode | 8 | 4.170 ns | 4.418 ns | 1.06 | - → - | 321 B → 315 B |
| XxHash3Cast | 8 | 1.730 ns | 1.652 ns | 0.95 | - → - | 482 B → 466 B |
| XxHash3Fixed | 8 | 1.617 ns | 1.623 ns | 1.00 | - → - | 500 B → 485 B |
| Fnv1a | 8 | 2.219 ns | 2.221 ns | 1.00 | - → - | 54 B → 54 B |
| SamplingHash | 8 | 1.014 ns | 1.145 ns | 1.13 | - → - | 79 B → 73 B |
| StringGetHashCode | 64 | 29.254 ns | 29.374 ns | 1.00 | - → - | 321 B → 317 B |
| XxHash3Cast | 64 | 6.619 ns | 5.814 ns | 0.88 | - → - | 785 B → 654 B |
| XxHash3Fixed | 64 | 6.677 ns | 6.042 ns | 0.90 | - → - | 807 B → 676 B |
| Fnv1a | 64 | 30.799 ns | 30.776 ns | 1.00 | - → - | 54 B → 54 B |
| SamplingHash | 64 | 1.026 ns | 1.130 ns | 1.10 | - → - | 79 B → 73 B |
| StringGetHashCode | 512 | 248.540 ns | 248.277 ns | 1.00 | - → - | 321 B → 317 B |
| XxHash3Cast | 512 | 22.457 ns | 20.420 ns | 0.91 | - → - | 972 B → 921 B |
| XxHash3Fixed | 512 | 21.932 ns | 17.840 ns | 0.81 | - → - | 994 B → 943 B |
| Fnv1a | 512 | 380.113 ns | 379.739 ns | 1.00 | - → - | 54 B → 54 B |
| SamplingHash | 512 | 1.014 ns | 1.134 ns | 1.12 | - → - | 79 B → 73 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.51 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Length | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| **StringGetHashCode** | **.NET 10.0**           | **.NET 10.0** | **8**      |   **4.170 ns** | **0.0086 ns** | **0.0123 ns** |   **4.169 ns** |   **4.148 ns** |   **4.190 ns** |   **4.187 ns** |  **1.00** |    **0.00** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | .NET 10.0           | .NET 10.0 | 8      |   1.730 ns | 0.0161 ns | 0.0220 ns |   1.727 ns |   1.701 ns |   1.772 ns |   1.753 ns |  0.41 |    0.01 |     482 B |         - |          NA |
| XxHash3Fixed      | .NET 10.0           | .NET 10.0 | 8      |   1.617 ns | 0.0041 ns | 0.0059 ns |   1.616 ns |   1.607 ns |   1.629 ns |   1.626 ns |  0.39 |    0.00 |     500 B |         - |          NA |
| Fnv1a             | .NET 10.0           | .NET 10.0 | 8      |   2.219 ns | 0.0063 ns | 0.0090 ns |   2.219 ns |   2.206 ns |   2.245 ns |   2.231 ns |  0.53 |    0.00 |      54 B |         - |          NA |
| SamplingHash      | .NET 10.0           | .NET 10.0 | 8      |   1.014 ns | 0.0040 ns | 0.0058 ns |   1.013 ns |   1.006 ns |   1.027 ns |   1.022 ns |  0.24 |    0.00 |      79 B |         - |          NA |
|                   |                     |           |        |            |           |           |            |            |            |            |       |         |           |           |             |
| StringGetHashCode | MediumRun-.NET 11.0 | .NET 11.0 | 8      |   4.418 ns | 0.0096 ns | 0.0137 ns |   4.414 ns |   4.398 ns |   4.443 ns |   4.436 ns |  1.00 |    0.00 |     315 B |         - |          NA |
| XxHash3Cast       | MediumRun-.NET 11.0 | .NET 11.0 | 8      |   1.652 ns | 0.0257 ns | 0.0352 ns |   1.652 ns |   1.609 ns |   1.701 ns |   1.692 ns |  0.37 |    0.01 |     466 B |         - |          NA |
| XxHash3Fixed      | MediumRun-.NET 11.0 | .NET 11.0 | 8      |   1.623 ns | 0.0216 ns | 0.0303 ns |   1.599 ns |   1.588 ns |   1.664 ns |   1.657 ns |  0.37 |    0.01 |     485 B |         - |          NA |
| Fnv1a             | MediumRun-.NET 11.0 | .NET 11.0 | 8      |   2.221 ns | 0.0116 ns | 0.0162 ns |   2.217 ns |   2.199 ns |   2.271 ns |   2.237 ns |  0.50 |    0.00 |      54 B |         - |          NA |
| SamplingHash      | MediumRun-.NET 11.0 | .NET 11.0 | 8      |   1.145 ns | 0.0813 ns | 0.1085 ns |   1.232 ns |   1.018 ns |   1.271 ns |   1.255 ns |  0.26 |    0.02 |      73 B |         - |          NA |
|                   |                     |           |        |            |           |           |            |            |            |            |       |         |           |           |             |
| **StringGetHashCode** | **.NET 10.0**           | **.NET 10.0** | **64**     |  **29.254 ns** | **0.0974 ns** | **0.1428 ns** |  **29.251 ns** |  **28.984 ns** |  **29.504 ns** |  **29.441 ns** |  **1.00** |    **0.00** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | .NET 10.0           | .NET 10.0 | 64     |   6.619 ns | 0.0429 ns | 0.0587 ns |   6.603 ns |   6.556 ns |   6.794 ns |   6.687 ns |  0.23 |    0.00 |     785 B |         - |          NA |
| XxHash3Fixed      | .NET 10.0           | .NET 10.0 | 64     |   6.677 ns | 0.0120 ns | 0.0173 ns |   6.675 ns |   6.636 ns |   6.708 ns |   6.699 ns |  0.23 |    0.00 |     807 B |         - |          NA |
| Fnv1a             | .NET 10.0           | .NET 10.0 | 64     |  30.799 ns | 0.0943 ns | 0.1259 ns |  30.781 ns |  30.659 ns |  31.174 ns |  30.967 ns |  1.05 |    0.01 |      54 B |         - |          NA |
| SamplingHash      | .NET 10.0           | .NET 10.0 | 64     |   1.026 ns | 0.0118 ns | 0.0173 ns |   1.022 ns |   1.010 ns |   1.080 ns |   1.042 ns |  0.04 |    0.00 |      79 B |         - |          NA |
|                   |                     |           |        |            |           |           |            |            |            |            |       |         |           |           |             |
| StringGetHashCode | MediumRun-.NET 11.0 | .NET 11.0 | 64     |  29.374 ns | 0.0462 ns | 0.0662 ns |  29.371 ns |  29.275 ns |  29.515 ns |  29.464 ns |  1.00 |    0.00 |     317 B |         - |          NA |
| XxHash3Cast       | MediumRun-.NET 11.0 | .NET 11.0 | 64     |   5.814 ns | 0.0095 ns | 0.0136 ns |   5.816 ns |   5.785 ns |   5.843 ns |   5.831 ns |  0.20 |    0.00 |     654 B |         - |          NA |
| XxHash3Fixed      | MediumRun-.NET 11.0 | .NET 11.0 | 64     |   6.042 ns | 0.0584 ns | 0.0837 ns |   5.990 ns |   5.956 ns |   6.215 ns |   6.152 ns |  0.21 |    0.00 |     676 B |         - |          NA |
| Fnv1a             | MediumRun-.NET 11.0 | .NET 11.0 | 64     |  30.776 ns | 0.0832 ns | 0.1139 ns |  30.751 ns |  30.621 ns |  31.028 ns |  30.947 ns |  1.05 |    0.00 |      54 B |         - |          NA |
| SamplingHash      | MediumRun-.NET 11.0 | .NET 11.0 | 64     |   1.130 ns | 0.0698 ns | 0.1001 ns |   1.140 ns |   1.012 ns |   1.238 ns |   1.232 ns |  0.04 |    0.00 |      73 B |         - |          NA |
|                   |                     |           |        |            |           |           |            |            |            |            |       |         |           |           |             |
| **StringGetHashCode** | **.NET 10.0**           | **.NET 10.0** | **512**    | **248.540 ns** | **0.4734 ns** | **0.6155 ns** | **248.390 ns** | **247.686 ns** | **249.932 ns** | **249.316 ns** | **1.000** |    **0.00** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | .NET 10.0           | .NET 10.0 | 512    |  22.457 ns | 3.2210 ns | 4.8211 ns |  22.480 ns |  17.598 ns |  27.314 ns |  27.264 ns | 0.090 |    0.02 |     972 B |         - |          NA |
| XxHash3Fixed      | .NET 10.0           | .NET 10.0 | 512    |  21.932 ns | 2.4831 ns | 3.7165 ns |  21.966 ns |  18.151 ns |  25.681 ns |  25.632 ns | 0.088 |    0.01 |     994 B |         - |          NA |
| Fnv1a             | .NET 10.0           | .NET 10.0 | 512    | 380.113 ns | 0.4504 ns | 0.6741 ns | 380.009 ns | 379.030 ns | 382.198 ns | 380.885 ns | 1.529 |    0.00 |      54 B |         - |          NA |
| SamplingHash      | .NET 10.0           | .NET 10.0 | 512    |   1.014 ns | 0.0031 ns | 0.0046 ns |   1.015 ns |   1.006 ns |   1.023 ns |   1.021 ns | 0.004 |    0.00 |      79 B |         - |          NA |
|                   |                     |           |        |            |           |           |            |            |            |            |       |         |           |           |             |
| StringGetHashCode | MediumRun-.NET 11.0 | .NET 11.0 | 512    | 248.277 ns | 0.3288 ns | 0.4715 ns | 248.198 ns | 247.754 ns | 249.664 ns | 248.890 ns | 1.000 |    0.00 |     317 B |         - |          NA |
| XxHash3Cast       | MediumRun-.NET 11.0 | .NET 11.0 | 512    |  20.420 ns | 1.5443 ns | 2.2149 ns |  22.341 ns |  17.935 ns |  22.616 ns |  22.505 ns | 0.082 |    0.01 |     921 B |         - |          NA |
| XxHash3Fixed      | MediumRun-.NET 11.0 | .NET 11.0 | 512    |  17.840 ns | 0.1015 ns | 0.1389 ns |  17.821 ns |  17.587 ns |  18.104 ns |  18.000 ns | 0.072 |    0.00 |     943 B |         - |          NA |
| Fnv1a             | MediumRun-.NET 11.0 | .NET 11.0 | 512    | 379.739 ns | 0.4110 ns | 0.5761 ns | 379.676 ns | 379.007 ns | 381.286 ns | 380.438 ns | 1.530 |    0.00 |      54 B |         - |          NA |
| SamplingHash      | MediumRun-.NET 11.0 | .NET 11.0 | 512    |   1.134 ns | 0.0797 ns | 0.1144 ns |   1.232 ns |   1.007 ns |   1.251 ns |   1.244 ns | 0.005 |    0.00 |      73 B |         - |          NA |

## Earlier record

- Verdict: adopted
- XxHash3: 0.26x (8 chars) / 0.21x (64) / 0.09x (512) vs string.GetHashCode
- MemoryMarshal.AsBytes cast path is equivalent to fixed (no pinning required, so prefer the cast) - zero-cost reinterpretation confirmed
- Hand-rolled FNV-1a is SLOWER than string.GetHashCode from 64 chars (1.05x at 64, 1.53x at 512) - do not hand-roll
- Sampling hash (BIT-01) stays ~0.23 ns at any length (0.001x at 512) but only works for known key sets

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Length | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------- |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| **StringGetHashCode** | **8**      |   **3.8378 ns** | **0.0317 ns** | **0.0444 ns** |   **3.7963 ns** |   **3.9533 ns** |   **3.9083 ns** |  **1.00** |    **0.02** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 8      |   0.9858 ns | 0.0109 ns | 0.0149 ns |   0.9707 ns |   1.0328 ns |   1.0007 ns |  0.26 |    0.00 |     471 B |         - |          NA |
| XxHash3Fixed      | 8      |   0.9473 ns | 0.0192 ns | 0.0263 ns |   0.9159 ns |   1.0192 ns |   0.9893 ns |  0.25 |    0.01 |     489 B |         - |          NA |
| Fnv1a             | 8      |   1.7350 ns | 0.0867 ns | 0.1215 ns |   1.6488 ns |   2.0624 ns |   1.9510 ns |  0.45 |    0.03 |      54 B |         - |          NA |
| SamplingHash      | 8      |   0.2246 ns | 0.0043 ns | 0.0062 ns |   0.2157 ns |   0.2380 ns |   0.2328 ns |  0.06 |    0.00 |      79 B |         - |          NA |
|                   |        |             |           |           |             |             |             |       |         |           |           |             |
| **StringGetHashCode** | **64**     |  **28.9965 ns** | **0.1830 ns** | **0.2443 ns** |  **28.7768 ns** |  **29.7969 ns** |  **29.1044 ns** | **1.000** |    **0.01** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 64     |   6.0301 ns | 0.0816 ns | 0.1090 ns |   5.9386 ns |   6.4458 ns |   6.1038 ns | 0.208 |    0.00 |     779 B |         - |          NA |
| XxHash3Fixed      | 64     |   5.9982 ns | 0.0337 ns | 0.0461 ns |   5.9248 ns |   6.1522 ns |   6.0466 ns | 0.207 |    0.00 |     801 B |         - |          NA |
| Fnv1a             | 64     |  30.3434 ns | 0.2400 ns | 0.3365 ns |  29.9548 ns |  31.4205 ns |  30.8390 ns | 1.047 |    0.01 |      54 B |         - |          NA |
| SamplingHash      | 64     |   0.2265 ns | 0.0055 ns | 0.0075 ns |   0.2157 ns |   0.2422 ns |   0.2377 ns | 0.008 |    0.00 |      79 B |         - |          NA |
|                   |        |             |           |           |             |             |             |       |         |           |           |             |
| **StringGetHashCode** | **512**    | **249.5169 ns** | **1.2440 ns** | **1.7439 ns** | **247.6708 ns** | **254.9917 ns** | **251.5221 ns** | **1.000** |    **0.01** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 512    |  21.1107 ns | 2.7649 ns | 3.7847 ns |  17.1888 ns |  25.9780 ns |  24.7836 ns | 0.085 |    0.01 |     961 B |         - |          NA |
| XxHash3Fixed      | 512    |  16.9231 ns | 0.0711 ns | 0.0973 ns |  16.7555 ns |  17.1793 ns |  17.0264 ns | 0.068 |    0.00 |     983 B |         - |          NA |
| Fnv1a             | 512    | 381.3360 ns | 1.5705 ns | 2.2017 ns | 379.5605 ns | 387.5817 ns | 384.4652 ns | 1.528 |    0.01 |      54 B |         - |          NA |
| SamplingHash      | 512    |   0.2319 ns | 0.0133 ns | 0.0177 ns |   0.2154 ns |   0.2823 ns |   0.2554 ns | 0.001 |    0.00 |      79 B |         - |          NA |
