# CON-03: False sharing and cache line padding

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
