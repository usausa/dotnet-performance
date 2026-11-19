# CON-03: False sharing and cache line padding

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. The penalty holds on net11: the 64 B pad is 0.51x / 0.21x / 0.21x of adjacent at 2 / 4 / 8 workers, and padded Interlocked is 0.42x / 0.40x / 0.28x of adjacent Interlocked
- On net11, 64 B and 128 B pads are equal at every worker count (98.9 / 99.8, 108.9 / 108.9, 181.5 / 174.3 μs). On net10 in this run 128 B was ahead at 2 workers (57.1 vs 89.6 μs), the opposite of the earlier record; the pad-size ordering is within this benchmark's run-to-run noise
- Generation-to-generation movements (adjacent volatile at 8 workers 1,894 → 854 μs, padded Interlocked at 4 workers 240 → 808 μs) are not attributed to the runtime for the same reason

### FalseSharingBenchmark

net10 → net11 per row:

| Method | Workers | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| AdjacentVolatile | 2 | 145.55 μs | 203.09 μs | 1.40 | 1.84 KB → 1.84 KB | 9,074 B → 8,370 B |
| Padded64Volatile | 2 | 89.64 μs | 98.89 μs | 1.10 | 1.84 KB → 1.85 KB | 9,835 B → 9,568 B |
| Padded128Volatile | 2 | 57.08 μs | 99.83 μs | 1.75 | 1.84 KB → 1.84 KB | 9,855 B → 9,618 B |
| AdjacentInterlocked | 2 | 887.94 μs | 908.12 μs | 1.02 | 1.84 KB → 1.84 KB | 9,320 B → 8,667 B |
| Padded128Interlocked | 2 | 548.55 μs | 378.81 μs | 0.69 | 1.84 KB → 1.84 KB | 8,812 B → 8,697 B |
| AdjacentVolatile | 4 | 425.82 μs | 605.62 μs | 1.42 | 2.29 KB → 2.26 KB | 9,703 B → 8,733 B |
| Padded64Volatile | 4 | 95.13 μs | 108.89 μs | 1.14 | 2.26 KB → 2.27 KB | 9,856 B → 8,820 B |
| Padded128Volatile | 4 | 93.17 μs | 108.87 μs | 1.17 | 2.26 KB → 2.26 KB | 9,790 B → 8,718 B |
| AdjacentInterlocked | 4 | 1,704.72 μs | 2,011.58 μs | 1.18 | 2.28 KB → 2.27 KB | 9,001 B → 8,332 B |
| Padded128Interlocked | 4 | 240.12 μs | 808.41 μs | 3.37 | 2.28 KB → 2.25 KB | 9,512 B → 8,247 B |
| AdjacentVolatile | 8 | 1,894.34 μs | 854.13 μs | 0.45 | 3.1 KB → 3.17 KB | 9,462 B → 8,101 B |
| Padded64Volatile | 8 | 122.60 μs | 181.49 μs | 1.48 | 3.09 KB → 3.1 KB | 9,511 B → 8,580 B |
| Padded128Volatile | 8 | 118.25 μs | 174.33 μs | 1.47 | 3.08 KB → 3.12 KB | 9,501 B → 8,366 B |
| AdjacentInterlocked | 8 | 3,801.98 μs | 3,388.91 μs | 0.89 | 3.16 KB → 3.15 KB | 8,636 B → 7,924 B |
| Padded128Interlocked | 8 | 936.12 μs | 943.29 μs | 1.01 | 3.1 KB → 3.1 KB | 9,027 B → 8,323 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 68.52 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Workers | Mean        | Error      | StdDev     | Median      | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |-------- |------------:|-----------:|-----------:|------------:|------------:|------------:|------------:|------:|--------:|----------:|-------:|----------:|------------:|
| **AdjacentVolatile**     | **.NET 10.0**           | **.NET 10.0** | **2**       |   **145.55 μs** |   **5.784 μs** |   **8.108 μs** |   **144.27 μs** |   **125.21 μs** |   **164.22 μs** |   **158.20 μs** |  **1.00** |    **0.00** |   **9,074 B** | **0.1221** |   **1.84 KB** |        **1.00** |
| Padded64Volatile     | .NET 10.0           | .NET 10.0 | 2       |    89.64 μs |   3.181 μs |   4.761 μs |    89.13 μs |    81.54 μs |    98.29 μs |    95.88 μs |  0.62 |    0.05 |   9,835 B | 0.2136 |   1.84 KB |        1.00 |
| Padded128Volatile    | .NET 10.0           | .NET 10.0 | 2       |    57.08 μs |  19.326 μs |  27.716 μs |    35.18 μs |    29.97 μs |    88.70 μs |    87.35 μs |  0.39 |    0.19 |   9,855 B | 0.2136 |   1.84 KB |        1.00 |
| AdjacentInterlocked  | .NET 10.0           | .NET 10.0 | 2       |   887.94 μs |   9.493 μs |  13.915 μs |   882.48 μs |   873.67 μs |   925.00 μs |   905.91 μs |  6.12 |    0.35 |   9,320 B |      - |   1.84 KB |        1.00 |
| Padded128Interlocked | .NET 10.0           | .NET 10.0 | 2       |   548.55 μs |   1.378 μs |   2.062 μs |   548.61 μs |   543.42 μs |   552.46 μs |   550.63 μs |  3.78 |    0.21 |   8,812 B |      - |   1.84 KB |        1.00 |
|                      |                     |           |         |             |            |            |             |             |             |             |       |         |           |        |           |             |
| AdjacentVolatile     | MediumRun-.NET 11.0 | .NET 11.0 | 2       |   203.09 μs |  28.402 μs |  41.631 μs |   170.01 μs |   156.96 μs |   261.61 μs |   250.16 μs |  1.00 |    0.00 |   8,370 B | 0.1831 |   1.84 KB |        1.00 |
| Padded64Volatile     | MediumRun-.NET 11.0 | .NET 11.0 | 2       |    98.89 μs |   1.220 μs |   1.827 μs |    99.10 μs |    95.58 μs |   101.70 μs |   101.21 μs |  0.51 |    0.10 |   9,568 B | 0.1831 |   1.85 KB |        1.01 |
| Padded128Volatile    | MediumRun-.NET 11.0 | .NET 11.0 | 2       |    99.83 μs |   1.278 μs |   1.914 μs |   100.15 μs |    93.74 μs |   102.85 μs |   101.43 μs |  0.51 |    0.10 |   9,618 B | 0.1831 |   1.84 KB |        1.00 |
| AdjacentInterlocked  | MediumRun-.NET 11.0 | .NET 11.0 | 2       |   908.12 μs |  16.919 μs |  25.324 μs |   906.41 μs |   875.92 μs |   950.09 μs |   940.07 μs |  4.66 |    0.93 |   8,667 B |      - |   1.84 KB |        1.00 |
| Padded128Interlocked | MediumRun-.NET 11.0 | .NET 11.0 | 2       |   378.81 μs | 116.645 μs | 174.589 μs |   380.42 μs |   203.39 μs |   553.08 μs |   551.25 μs |  1.94 |    0.98 |   8,697 B |      - |   1.84 KB |        1.00 |
|                      |                     |           |         |             |            |            |             |             |             |             |       |         |           |        |           |             |
| **AdjacentVolatile**     | **.NET 10.0**           | **.NET 10.0** | **4**       |   **425.82 μs** |  **46.668 μs** |  **69.850 μs** |   **430.92 μs** |   **334.37 μs** |   **503.66 μs** |   **499.30 μs** |  **1.00** |    **0.00** |   **9,703 B** |      **-** |   **2.29 KB** |        **1.00** |
| Padded64Volatile     | .NET 10.0           | .NET 10.0 | 4       |    95.13 μs |   1.264 μs |   1.893 μs |    95.56 μs |    90.95 μs |    98.26 μs |    97.09 μs |  0.23 |    0.04 |   9,856 B | 0.2441 |   2.26 KB |        0.98 |
| Padded128Volatile    | .NET 10.0           | .NET 10.0 | 4       |    93.17 μs |   1.455 μs |   2.087 μs |    92.61 μs |    89.50 μs |    98.43 μs |    96.49 μs |  0.22 |    0.04 |   9,790 B | 0.2441 |   2.26 KB |        0.99 |
| AdjacentInterlocked  | .NET 10.0           | .NET 10.0 | 4       | 1,704.72 μs |  60.106 μs |  88.103 μs | 1,689.06 μs | 1,576.33 μs | 1,918.74 μs | 1,811.71 μs |  4.11 |    0.71 |   9,001 B |      - |   2.28 KB |        0.99 |
| Padded128Interlocked | .NET 10.0           | .NET 10.0 | 4       |   240.12 μs |   2.429 μs |   3.560 μs |   239.57 μs |   235.07 μs |   249.43 μs |   243.94 μs |  0.58 |    0.10 |   9,512 B | 0.2441 |   2.28 KB |        0.99 |
|                      |                     |           |         |             |            |            |             |             |             |             |       |         |           |        |           |             |
| AdjacentVolatile     | MediumRun-.NET 11.0 | .NET 11.0 | 4       |   605.62 μs | 150.554 μs | 225.343 μs |   550.87 μs |   374.38 μs |   875.95 μs |   860.81 μs |  1.00 |    0.00 |   8,733 B |      - |   2.26 KB |        1.00 |
| Padded64Volatile     | MediumRun-.NET 11.0 | .NET 11.0 | 4       |   108.89 μs |   1.058 μs |   1.448 μs |   108.74 μs |   106.30 μs |   112.18 μs |   110.58 μs |  0.21 |    0.07 |   8,820 B | 0.2441 |   2.27 KB |        1.00 |
| Padded128Volatile    | MediumRun-.NET 11.0 | .NET 11.0 | 4       |   108.87 μs |   3.118 μs |   4.667 μs |   108.70 μs |   102.30 μs |   116.80 μs |   114.81 μs |  0.21 |    0.07 |   8,718 B | 0.2441 |   2.26 KB |        1.00 |
| AdjacentInterlocked  | MediumRun-.NET 11.0 | .NET 11.0 | 4       | 2,011.58 μs | 256.165 μs | 383.416 μs | 2,041.16 μs | 1,565.91 μs | 2,409.95 μs | 2,399.46 μs |  3.82 |    1.57 |   8,332 B |      - |   2.27 KB |        1.00 |
| Padded128Interlocked | MediumRun-.NET 11.0 | .NET 11.0 | 4       |   808.41 μs |  10.106 μs |  14.167 μs |   812.40 μs |   760.69 μs |   819.69 μs |   817.26 μs |  1.54 |    0.55 |   8,247 B |      - |   2.25 KB |        0.99 |
|                      |                     |           |         |             |            |            |             |             |             |             |       |         |           |        |           |             |
| **AdjacentVolatile**     | **.NET 10.0**           | **.NET 10.0** | **8**       | **1,894.34 μs** |  **94.135 μs** | **140.896 μs** | **1,977.76 μs** | **1,695.17 μs** | **2,013.26 μs** | **2,007.20 μs** |  **1.00** |    **0.00** |   **9,462 B** |      **-** |    **3.1 KB** |        **1.00** |
| Padded64Volatile     | .NET 10.0           | .NET 10.0 | 8       |   122.60 μs |   1.082 μs |   1.551 μs |   122.94 μs |   119.18 μs |   124.82 μs |   124.35 μs |  0.07 |    0.01 |   9,511 B | 0.3662 |   3.09 KB |        1.00 |
| Padded128Volatile    | .NET 10.0           | .NET 10.0 | 8       |   118.25 μs |   1.218 μs |   1.785 μs |   118.55 μs |   113.83 μs |   121.34 μs |   120.32 μs |  0.06 |    0.00 |   9,501 B | 0.3662 |   3.08 KB |        1.00 |
| AdjacentInterlocked  | .NET 10.0           | .NET 10.0 | 8       | 3,801.98 μs | 299.488 μs | 448.260 μs | 3,772.70 μs | 3,271.03 μs | 4,379.96 μs | 4,326.19 μs |  2.02 |    0.28 |   8,636 B |      - |   3.16 KB |        1.02 |
| Padded128Interlocked | .NET 10.0           | .NET 10.0 | 8       |   936.12 μs |  11.291 μs |  16.193 μs |   935.42 μs |   899.59 μs |   963.57 μs |   956.24 μs |  0.50 |    0.04 |   9,027 B |      - |    3.1 KB |        1.00 |
|                      |                     |           |         |             |            |            |             |             |             |             |       |         |           |        |           |             |
| AdjacentVolatile     | MediumRun-.NET 11.0 | .NET 11.0 | 8       |   854.13 μs |  13.777 μs |  20.195 μs |   863.90 μs |   812.69 μs |   876.30 μs |   872.99 μs |  1.00 |    0.00 |   8,101 B |      - |   3.17 KB |        1.00 |
| Padded64Volatile     | MediumRun-.NET 11.0 | .NET 11.0 | 8       |   181.49 μs |   6.109 μs |   9.144 μs |   180.43 μs |   166.89 μs |   196.24 μs |   192.75 μs |  0.21 |    0.01 |   8,580 B | 0.3662 |    3.1 KB |        0.98 |
| Padded128Volatile    | MediumRun-.NET 11.0 | .NET 11.0 | 8       |   174.33 μs |   4.510 μs |   6.750 μs |   171.91 μs |   163.98 μs |   184.59 μs |   183.09 μs |  0.20 |    0.01 |   8,366 B | 0.3662 |   3.12 KB |        0.98 |
| AdjacentInterlocked  | MediumRun-.NET 11.0 | .NET 11.0 | 8       | 3,388.91 μs |  68.127 μs |  99.860 μs | 3,383.77 μs | 3,216.03 μs | 3,640.57 μs | 3,508.50 μs |  3.97 |    0.15 |   7,924 B |      - |   3.15 KB |        0.99 |
| Padded128Interlocked | MediumRun-.NET 11.0 | .NET 11.0 | 8       |   943.29 μs |  12.224 μs |  17.136 μs |   941.88 μs |   912.16 μs |   977.56 μs |   965.01 μs |  1.10 |    0.03 |   8,323 B |      - |    3.1 KB |        0.98 |

## Earlier record

- Verdict: adopted. **The penalty is universal; the pad size is not** - 128 B is required on x86-64-v3 (Zen 3)
  and 64 B is enough on x86-64-v4 (Zen 5), where the extra 64 B measured worse at low worker counts
- Measured twice on x86-64-v4 because this benchmark is genuinely noisy (AdjacentVolatile at 8 workers came
  out 756 +/- 23 us and 993 +/- 254 us in the two runs). Every claim below holds in both runs
- Time scales with the worker count, so ratios are only comparable inside one Workers value
- The ~1.8-3.2 KB allocation is Parallel.For internals and lands on every variant equally

## The penalty (x86-64-v4, both runs)

| Workers | Adjacent | Padded 64 | Penalty |
|---|---|---|---|
| 2 | 151.1 / 145.0 us | 33.8 / 35.9 us | **4.0-4.5x** |
| 4 | 495.0 / 426.2 us | 38.4 / 77.6 us | **5.5-12.9x** |
| 8 | 756.3 / 992.7 us | 138.9 / 135.4 us | **5.4-7.3x** |

Interlocked writes do not mask the penalty, they amplify it: adjacent interlocked is 4,354 / 4,433 us at
8 workers against 1,047 / 1,052 us for the padded form (**4.2x**), and the adjacent interlocked path is itself
5.8x the adjacent volatile baseline. Padding matters more under Interlocked, not less.

## The pad size is microarchitecture dependent

| Workers | | x86-64-v3 (Zen 3) | x86-64-v4 (Zen 5) run 1 / run 2 |
|---|---|---|---|
| 2 | 64 B / 128 B | 69.4 / 66.9 us (equal) | 33.8 / 46.0 us -- 35.9 / 83.6 us (**64 B ahead**) |
| 4 | 64 B / 128 B | 93.7 / 92.9 us (equal) | 38.4 / 104.9 us -- 77.6 / 100.2 us (**64 B ahead**) |
| 8 | 64 B / 128 B | **150.6 / 52.7 us (128 B 2.86x ahead)** | 138.9 / 139.1 us -- 135.4 / 136.2 us (equal) |

**Why it moves:** 128 B is the BCL's own choice (`PaddingHelpers` pads to 128 B on x64) because a prefetcher
that pulls **cache line pairs** makes two neighbouring 64 B lines behave as one unit - so a 64 B pad still puts
two writers in the same contended granule. Zen 3 shows exactly that at 8 workers. Zen 5 shows no pair effect at
any worker count: 64 B already isolates the writers, and the wider stride only adds footprint, which is what
the 2- and 4-worker rows charge for.

## What to weigh when adopting

1. **Padding itself is not the conditional part.** Every configuration measured, on both machines, is 4-13x
   better than adjacent slots. Never skip the pad because the size is uncertain
2. **Pick the size from the target's prefetch granularity, not from the line size.** A CPU whose prefetcher
   works in line pairs needs 128 B; one that does not is fully served by 64 B. If you cannot determine this,
   **128 B is the safer default**: its downside is footprint (and, at low contention on Zen 5, up to 2.3x on an
   already-fast padded path), while the downside of an insufficient pad is the full 2.86x false-sharing penalty
   returning at high contention
3. **Measure at your real worker count.** The two sizes converge as contention rises - equal at 8 workers on
   Zen 5 - so a low-worker benchmark is where over-padding looks worst and a high-worker one is where
   under-padding does. Neither alone answers the question
4. **Size the counter block, not just the field.** What matters is the distance between two slots written by
   different threads; padding a struct that is then packed into a dense array changes nothing
5. **Re-measure rather than trusting a single run here.** Thread scheduling puts this benchmark's noise well
   above the effects being compared on the padded paths (see the two AdjacentVolatile figures above)

## x86-64-v4 run 1 (full)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Workers | Mean        | Error      | StdDev     | Median      | Min         | Max         | P90         | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------- |------------:|-----------:|-----------:|------------:|------------:|------------:|------------:|------:|--------:|-------:|----------:|----------:|------------:|
| **AdjacentVolatile**     | **2**       |   **151.12 μs** |  **11.272 μs** |  **16.871 μs** |   **151.16 μs** |   **131.35 μs** |   **169.08 μs** |   **168.22 μs** |  **1.01** |    **0.16** |      **-** |   **8,240 B** |   **1.84 KB** |        **1.00** |
| Padded64Volatile     | 2       |    33.78 μs |   0.728 μs |   0.996 μs |    33.80 μs |    32.44 μs |    35.88 μs |    34.94 μs |  0.23 |    0.03 | 0.1831 |   8,388 B |   1.83 KB |        1.00 |
| Padded128Volatile    | 2       |    46.01 μs |  15.386 μs |  22.553 μs |    32.14 μs |    31.58 μs |    86.34 μs |    83.99 μs |  0.31 |    0.15 | 0.1831 |   8,701 B |   1.84 KB |        1.00 |
| AdjacentInterlocked  | 2       |   891.01 μs |   3.076 μs |   4.411 μs |   890.42 μs |   885.13 μs |   900.26 μs |   897.37 μs |  5.97 |    0.66 |      - |   8,448 B |   1.85 KB |        1.01 |
| Padded128Interlocked | 2       |   557.32 μs |   1.728 μs |   2.533 μs |   557.14 μs |   552.15 μs |   562.91 μs |   560.50 μs |  3.73 |    0.41 |      - |   8,141 B |   1.84 KB |        1.00 |
|                      |         |             |            |            |             |             |             |             |       |         |        |           |           |             |
| **AdjacentVolatile**     | **4**       |   **495.02 μs** |  **16.313 μs** |  **23.911 μs** |   **496.38 μs** |   **418.33 μs** |   **540.27 μs** |   **519.47 μs** |  **1.00** |    **0.07** |      **-** |   **8,142 B** |   **2.27 KB** |        **1.00** |
| Padded64Volatile     | 4       |    38.43 μs |   0.847 μs |   1.241 μs |    38.34 μs |    36.21 μs |    41.52 μs |    39.92 μs |  0.08 |    0.00 | 0.2441 |   8,354 B |   2.26 KB |        1.00 |
| Padded128Volatile    | 4       |   104.93 μs |   1.412 μs |   2.069 μs |   104.69 μs |   101.61 μs |   110.67 μs |   107.42 μs |  0.21 |    0.01 | 0.2441 |   8,337 B |   2.28 KB |        1.01 |
| AdjacentInterlocked  | 4       | 2,413.44 μs |  12.226 μs |  17.534 μs | 2,412.27 μs | 2,367.41 μs | 2,443.67 μs | 2,438.68 μs |  4.89 |    0.25 |      - |   8,359 B |    2.3 KB |        1.01 |
| Padded128Interlocked | 4       |   830.15 μs |  26.999 μs |  39.575 μs |   847.58 μs |   782.89 μs |   887.00 μs |   877.03 μs |  1.68 |    0.12 |      - |   8,054 B |   2.26 KB |        0.99 |
|                      |         |             |            |            |             |             |             |             |       |         |        |           |           |             |
| **AdjacentVolatile**     | **8**       |   **756.31 μs** |  **15.299 μs** |  **22.899 μs** |   **753.61 μs** |   **719.98 μs** |   **797.93 μs** |   **785.89 μs** |  **1.00** |    **0.04** |      **-** |   **8,153 B** |   **3.23 KB** |        **1.00** |
| Padded64Volatile     | 8       |   138.88 μs |   2.748 μs |   4.113 μs |   138.46 μs |   131.89 μs |   148.68 μs |   144.71 μs |  0.18 |    0.01 | 0.3662 |   8,203 B |   3.09 KB |        0.96 |
| Padded128Volatile    | 8       |   139.13 μs |   4.749 μs |   6.961 μs |   137.94 μs |   129.10 μs |   151.07 μs |   147.72 μs |  0.18 |    0.01 | 0.3662 |   7,262 B |   3.09 KB |        0.96 |
| AdjacentInterlocked  | 8       | 4,354.64 μs | 116.647 μs | 174.591 μs | 4,350.92 μs | 4,042.25 μs | 4,745.23 μs | 4,557.40 μs |  5.76 |    0.28 |      - |   8,353 B |   3.18 KB |        0.99 |
| Padded128Interlocked | 8       | 1,046.98 μs |  26.763 μs |  37.518 μs | 1,040.93 μs |   975.37 μs | 1,118.61 μs | 1,110.58 μs |  1.39 |    0.06 |      - |   7,257 B |    3.1 KB |        0.96 |
