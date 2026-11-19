# LAB: Unsafe.CopyBlockUnaligned (rejected, R-14)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - rejected. Variable length: CopyBlockUnaligned is 0.91-1.00x of Span.CopyTo on net11
- Constant 64 B: the two forms swap places between the generations (net10 2.19 / 3.34 ns, net11 3.34 / 2.19 ns) with unchanged code sizes (102 / 64 B) - a placement effect (pitfall 10), not a reason to prefer either

### CopyVariableBenchmark

net10 → net11 per row:

| Method | Size | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| SpanCopyTo | 16 | 1.862 ns | 1.845 ns | 0.99 | - → - | 671 B → 838 B |
| ArrayCopy | 16 | 2.429 ns | 2.373 ns | 0.98 | - → - | 2,355 B → 3,075 B |
| CopyBlockUnaligned | 16 | 1.842 ns | 1.840 ns | 1.00 | - → - | 633 B → 800 B |
| SpanCopyTo | 512 | 4.414 ns | 4.544 ns | 1.03 | - → - | 671 B → 838 B |
| ArrayCopy | 512 | 5.869 ns | 5.125 ns | 0.87 | - → - | 2,355 B → 3,075 B |
| CopyBlockUnaligned | 512 | 4.086 ns | 4.117 ns | 1.01 | - → - | 633 B → 800 B |
| SpanCopyTo | 4096 | 27.102 ns | 27.303 ns | 1.01 | - → - | 605 B → 605 B |
| ArrayCopy | 4096 | 28.237 ns | 27.713 ns | 0.98 | - → - | 2,397 B → 3,114 B |
| CopyBlockUnaligned | 4096 | 26.571 ns | 26.910 ns | 1.01 | - → - | 567 B → 567 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.58 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Size | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **SpanCopyTo**         | **.NET 10.0**           | **.NET 10.0** | **16**   |  **1.862 ns** | **0.0095 ns** | **0.0136 ns** |  **1.863 ns** |  **1.838 ns** |  **1.892 ns** |  **1.878 ns** |  **1.00** |    **0.00** |     **671 B** |         **-** |          **NA** |
| ArrayCopy          | .NET 10.0           | .NET 10.0 | 16   |  2.429 ns | 0.0365 ns | 0.0534 ns |  2.410 ns |  2.358 ns |  2.515 ns |  2.490 ns |  1.30 |    0.03 |   2,355 B |         - |          NA |
| CopyBlockUnaligned | .NET 10.0           | .NET 10.0 | 16   |  1.842 ns | 0.0071 ns | 0.0102 ns |  1.840 ns |  1.824 ns |  1.865 ns |  1.855 ns |  0.99 |    0.01 |     633 B |         - |          NA |
|                    |                     |           |      |           |           |           |           |           |           |           |       |         |           |           |             |
| SpanCopyTo         | MediumRun-.NET 11.0 | .NET 11.0 | 16   |  1.845 ns | 0.0079 ns | 0.0108 ns |  1.844 ns |  1.826 ns |  1.870 ns |  1.859 ns |  1.00 |    0.00 |     838 B |         - |          NA |
| ArrayCopy          | MediumRun-.NET 11.0 | .NET 11.0 | 16   |  2.373 ns | 0.0096 ns | 0.0144 ns |  2.373 ns |  2.354 ns |  2.406 ns |  2.394 ns |  1.29 |    0.01 |   3,075 B |         - |          NA |
| CopyBlockUnaligned | MediumRun-.NET 11.0 | .NET 11.0 | 16   |  1.840 ns | 0.0127 ns | 0.0182 ns |  1.835 ns |  1.821 ns |  1.895 ns |  1.861 ns |  1.00 |    0.01 |     800 B |         - |          NA |
|                    |                     |           |      |           |           |           |           |           |           |           |       |         |           |           |             |
| **SpanCopyTo**         | **.NET 10.0**           | **.NET 10.0** | **512**  |  **4.414 ns** | **0.0407 ns** | **0.0584 ns** |  **4.421 ns** |  **4.316 ns** |  **4.507 ns** |  **4.486 ns** |  **1.00** |    **0.00** |     **671 B** |         **-** |          **NA** |
| ArrayCopy          | .NET 10.0           | .NET 10.0 | 512  |  5.869 ns | 0.4427 ns | 0.6490 ns |  5.308 ns |  5.222 ns |  6.577 ns |  6.557 ns |  1.33 |    0.15 |   2,355 B |         - |          NA |
| CopyBlockUnaligned | .NET 10.0           | .NET 10.0 | 512  |  4.086 ns | 0.0936 ns | 0.1373 ns |  4.003 ns |  3.911 ns |  4.289 ns |  4.241 ns |  0.93 |    0.03 |     633 B |         - |          NA |
|                    |                     |           |      |           |           |           |           |           |           |           |       |         |           |           |             |
| SpanCopyTo         | MediumRun-.NET 11.0 | .NET 11.0 | 512  |  4.544 ns | 0.1559 ns | 0.2334 ns |  4.552 ns |  4.284 ns |  4.817 ns |  4.799 ns |  1.00 |    0.00 |     838 B |         - |          NA |
| ArrayCopy          | MediumRun-.NET 11.0 | .NET 11.0 | 512  |  5.125 ns | 0.0921 ns | 0.1351 ns |  5.213 ns |  4.929 ns |  5.298 ns |  5.285 ns |  1.13 |    0.06 |   3,075 B |         - |          NA |
| CopyBlockUnaligned | MediumRun-.NET 11.0 | .NET 11.0 | 512  |  4.117 ns | 0.0833 ns | 0.1168 ns |  4.193 ns |  3.967 ns |  4.287 ns |  4.243 ns |  0.91 |    0.05 |     800 B |         - |          NA |
|                    |                     |           |      |           |           |           |           |           |           |           |       |         |           |           |             |
| **SpanCopyTo**         | **.NET 10.0**           | **.NET 10.0** | **4096** | **27.102 ns** | **0.1072 ns** | **0.1571 ns** | **27.069 ns** | **26.849 ns** | **27.478 ns** | **27.297 ns** |  **1.00** |    **0.00** |     **605 B** |         **-** |          **NA** |
| ArrayCopy          | .NET 10.0           | .NET 10.0 | 4096 | 28.237 ns | 0.2694 ns | 0.3864 ns | 28.299 ns | 27.744 ns | 28.770 ns | 28.712 ns |  1.04 |    0.02 |   2,397 B |         - |          NA |
| CopyBlockUnaligned | .NET 10.0           | .NET 10.0 | 4096 | 26.571 ns | 0.6460 ns | 0.9056 ns | 27.247 ns | 25.470 ns | 27.568 ns | 27.523 ns |  0.98 |    0.03 |     567 B |         - |          NA |
|                    |                     |           |      |           |           |           |           |           |           |           |       |         |           |           |             |
| SpanCopyTo         | MediumRun-.NET 11.0 | .NET 11.0 | 4096 | 27.303 ns | 0.1032 ns | 0.1545 ns | 27.297 ns | 27.010 ns | 27.597 ns | 27.493 ns |  1.00 |    0.00 |     605 B |         - |          NA |
| ArrayCopy          | MediumRun-.NET 11.0 | .NET 11.0 | 4096 | 27.713 ns | 0.1212 ns | 0.1699 ns | 27.714 ns | 27.356 ns | 28.008 ns | 27.942 ns |  1.02 |    0.01 |   3,114 B |         - |          NA |
| CopyBlockUnaligned | MediumRun-.NET 11.0 | .NET 11.0 | 4096 | 26.910 ns | 0.0722 ns | 0.1081 ns | 26.910 ns | 26.701 ns | 27.143 ns | 27.032 ns |  0.99 |    0.01 |     567 B |         - |          NA |

### CopyConstantBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| SpanCopyTo8 | 0.8578 ns | 0.8557 ns | 1.00 | - → - | 96 B → 96 B |
| CopyBlockUnaligned8 | 0.8534 ns | 0.8564 ns | 1.00 | - → - | 52 B → 52 B |
| SpanCopyTo16 | 0.9277 ns | 0.8569 ns | 0.92 | - → - | 99 B → 99 B |
| CopyBlockUnaligned16 | 0.8598 ns | 0.8530 ns | 0.99 | - → - | 54 B → 54 B |
| SpanCopyTo64 | 2.1901 ns | 3.3433 ns | 1.53 | - → - | 102 B → 102 B |
| CopyBlockUnaligned64 | 3.3391 ns | 2.1927 ns | 0.66 | - → - | 64 B → 64 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.58 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| SpanCopyTo8          | .NET 10.0           | .NET 10.0 | 0.8578 ns | 0.0096 ns | 0.0141 ns | 0.8601 ns | 0.8382 ns | 0.8873 ns | 0.8755 ns |  1.00 |    0.00 |      96 B |         - |          NA |
| CopyBlockUnaligned8  | .NET 10.0           | .NET 10.0 | 0.8534 ns | 0.0042 ns | 0.0062 ns | 0.8530 ns | 0.8425 ns | 0.8706 ns | 0.8612 ns |  1.00 |    0.02 |      52 B |         - |          NA |
| SpanCopyTo16         | .NET 10.0           | .NET 10.0 | 0.9277 ns | 0.0565 ns | 0.0828 ns | 0.9915 ns | 0.8389 ns | 1.0244 ns | 1.0155 ns |  1.08 |    0.10 |      99 B |         - |          NA |
| CopyBlockUnaligned16 | .NET 10.0           | .NET 10.0 | 0.8598 ns | 0.0050 ns | 0.0073 ns | 0.8615 ns | 0.8494 ns | 0.8745 ns | 0.8680 ns |  1.00 |    0.02 |      54 B |         - |          NA |
| SpanCopyTo64         | .NET 10.0           | .NET 10.0 | 2.1901 ns | 0.8205 ns | 1.1768 ns | 2.1867 ns | 1.0282 ns | 3.3676 ns | 3.3559 ns |  2.55 |    1.35 |     102 B |         - |          NA |
| CopyBlockUnaligned64 | .NET 10.0           | .NET 10.0 | 3.3391 ns | 0.0063 ns | 0.0090 ns | 3.3377 ns | 3.3230 ns | 3.3619 ns | 3.3523 ns |  3.89 |    0.06 |      64 B |         - |          NA |
|                      |                     |           |           |           |           |           |           |           |           |       |         |           |           |             |
| SpanCopyTo8          | MediumRun-.NET 11.0 | .NET 11.0 | 0.8557 ns | 0.0079 ns | 0.0113 ns | 0.8619 ns | 0.8391 ns | 0.8690 ns | 0.8675 ns |  1.00 |    0.00 |      96 B |         - |          NA |
| CopyBlockUnaligned8  | MediumRun-.NET 11.0 | .NET 11.0 | 0.8564 ns | 0.0107 ns | 0.0150 ns | 0.8515 ns | 0.8434 ns | 0.9026 ns | 0.8827 ns |  1.00 |    0.02 |      52 B |         - |          NA |
| SpanCopyTo16         | MediumRun-.NET 11.0 | .NET 11.0 | 0.8569 ns | 0.0073 ns | 0.0110 ns | 0.8575 ns | 0.8372 ns | 0.8732 ns | 0.8711 ns |  1.00 |    0.02 |      99 B |         - |          NA |
| CopyBlockUnaligned16 | MediumRun-.NET 11.0 | .NET 11.0 | 0.8530 ns | 0.0021 ns | 0.0030 ns | 0.8525 ns | 0.8479 ns | 0.8616 ns | 0.8561 ns |  1.00 |    0.01 |      54 B |         - |          NA |
| SpanCopyTo64         | MediumRun-.NET 11.0 | .NET 11.0 | 3.3433 ns | 0.0041 ns | 0.0060 ns | 3.3434 ns | 3.3289 ns | 3.3562 ns | 3.3494 ns |  3.91 |    0.05 |     102 B |         - |          NA |
| CopyBlockUnaligned64 | MediumRun-.NET 11.0 | .NET 11.0 | 2.1927 ns | 0.7470 ns | 1.0472 ns | 1.2835 ns | 1.2538 ns | 3.3507 ns | 3.3486 ns |  2.56 |    1.20 |      64 B |         - |          NA |

## Earlier record

- Verdict: rejected
- Variable length: 0.81-1.01x vs Span.CopyTo (both reach the same Memmove; the sub-ns gap at 16 B is call-shape overhead)
- Constant sizes: 8 B 0.89x and 16 B 0.94x, but the CIs overlap - the sub-0.05 ns gap is at the limit of resolution; **at 64 B CopyBlockUnaligned is 1.07x SLOWER** (2.52 vs 2.36 ns, non-overlapping CIs)
- The only advantage that holds is code size (52-64 B vs 96-102 B); an unresolvable win at small constants that reverses by 64 B does not justify losing the type/bounds safety
- Array.Copy is slowest (1.22-1.59x) with 1.7 KB code

## CopyVariableBenchmark

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Size | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |----- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| **SpanCopyTo**         | **16**   |  **1.0310 ns** | **0.0052 ns** | **0.0075 ns** |  **1.0326 ns** |  **1.0168 ns** |  **1.0428 ns** |  **1.0408 ns** |  **1.00** |    **0.01** |     **657 B** |         **-** |          **NA** |
| ArrayCopy          | 16   |  1.6386 ns | 0.0231 ns | 0.0332 ns |  1.6308 ns |  1.6092 ns |  1.7736 ns |  1.6533 ns |  1.59 |    0.03 |   1,694 B |         - |          NA |
| CopyBlockUnaligned | 16   |  0.8392 ns | 0.0093 ns | 0.0133 ns |  0.8380 ns |  0.8149 ns |  0.8643 ns |  0.8575 ns |  0.81 |    0.01 |     619 B |         - |          NA |
|                    |      |            |           |           |            |            |            |            |       |         |           |           |             |
| **SpanCopyTo**         | **512**  |  **4.7445 ns** | **0.0397 ns** | **0.0557 ns** |  **4.7286 ns** |  **4.6763 ns** |  **4.9087 ns** |  **4.7951 ns** |  **1.00** |    **0.02** |     **652 B** |         **-** |          **NA** |
| ArrayCopy          | 512  |  5.7657 ns | 0.0162 ns | 0.0243 ns |  5.7649 ns |  5.7246 ns |  5.8215 ns |  5.7944 ns |  1.22 |    0.01 |   1,671 B |         - |          NA |
| CopyBlockUnaligned | 512  |  4.3506 ns | 0.0186 ns | 0.0267 ns |  4.3497 ns |  4.3019 ns |  4.4156 ns |  4.3789 ns |  0.92 |    0.01 |     614 B |         - |          NA |
|                    |      |            |           |           |            |            |            |            |       |         |           |           |             |
| **SpanCopyTo**         | **4096** | **25.8064 ns** | **0.2123 ns** | **0.3178 ns** | **25.8598 ns** | **25.3305 ns** | **26.3525 ns** | **26.1954 ns** |  **1.00** |    **0.02** |     **619 B** |         **-** |          **NA** |
| ArrayCopy          | 4096 | 26.9068 ns | 0.2651 ns | 0.3628 ns | 26.8280 ns | 26.6048 ns | 28.0352 ns | 27.2880 ns |  1.04 |    0.02 |   1,744 B |         - |          NA |
| CopyBlockUnaligned | 4096 | 26.0984 ns | 0.3311 ns | 0.4748 ns | 26.3741 ns | 25.5297 ns | 26.6703 ns | 26.6365 ns |  1.01 |    0.02 |     581 B |         - |          NA |

## CopyConstantBenchmark (constant 8 / 16 / 64 B)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| SpanCopyTo8          | 0.1746 ns | 0.0080 ns | 0.0119 ns | 0.1545 ns | 0.2003 ns | 0.1917 ns |  1.00 |    0.09 |      96 B |         - |          NA |
| CopyBlockUnaligned8  | 0.1555 ns | 0.0179 ns | 0.0262 ns | 0.1008 ns | 0.2325 ns | 0.1823 ns |  0.89 |    0.16 |      52 B |         - |          NA |
| SpanCopyTo16         | 0.2044 ns | 0.0053 ns | 0.0080 ns | 0.1925 ns | 0.2223 ns | 0.2155 ns |  1.18 |    0.09 |      99 B |         - |          NA |
| CopyBlockUnaligned16 | 0.1921 ns | 0.0020 ns | 0.0029 ns | 0.1831 ns | 0.1976 ns | 0.1963 ns |  1.11 |    0.07 |      54 B |         - |          NA |
| SpanCopyTo64         | 2.3566 ns | 0.0044 ns | 0.0066 ns | 2.3458 ns | 2.3693 ns | 2.3650 ns | 13.56 |    0.89 |     102 B |         - |          NA |
| CopyBlockUnaligned64 | 2.5183 ns | 0.0064 ns | 0.0096 ns | 2.5040 ns | 2.5338 ns | 2.5291 ns | 14.49 |    0.95 |      64 B |         - |          NA |

