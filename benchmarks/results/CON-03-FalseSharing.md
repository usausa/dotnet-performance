# CON-03: False sharing and cache line padding

- Verdict: adopted
- Adjacent slots in a long[] cost 7.4x at 2 workers and 29.7x at 8 workers (1,565.20 vs 52.66 us)
- 64 bytes of padding is not enough. At 2 and 4 workers 64 B and 128 B are equal, but at 8 workers
  64 B is 150.59 us against 52.66 us for 128 B, a 2.86x gap. Pad to 128 bytes, which is what the BCL's own
  PaddingHelpers uses, because adjacent line prefetching pulls cache line pairs
- Interlocked writes do not mask the penalty, they amplify it: adjacent interlocked is 4.23x the adjacent
  volatile baseline at 8 workers, and 9.8x its padded counterpart (6,612 vs 673 us)
- Time scales with the worker count, so ratios are only comparable inside one Workers value
- The ~1.8-3.1 KB allocation is Parallel.For internals and lands on every variant equally (ratio 0.98-1.00)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Workers | Mean        | Error      | StdDev     | Min         | Max         | P90         | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------- |------------:|-----------:|-----------:|------------:|------------:|------------:|------:|--------:|-------:|----------:|----------:|------------:|
| **AdjacentVolatile**     | **2**       |   **516.51 μs** |  **51.314 μs** |  **76.805 μs** |   **344.59 μs** |   **636.47 μs** |   **612.75 μs** |  **1.02** |    **0.23** |      **-** |   **8,182 B** |   **1.86 KB** |        **1.00** |
| Padded64Volatile     | 2       |    69.43 μs |   1.245 μs |   1.864 μs |    66.39 μs |    73.47 μs |    71.37 μs |  0.14 |    0.02 |      - |   7,348 B |   1.84 KB |        0.99 |
| Padded128Volatile    | 2       |    66.86 μs |   0.809 μs |   1.211 μs |    64.24 μs |    69.36 μs |    68.20 μs |  0.13 |    0.02 |      - |   8,255 B |   1.84 KB |        0.99 |
| AdjacentInterlocked  | 2       |   646.38 μs |  20.661 μs |  30.924 μs |   593.57 μs |   703.99 μs |   691.78 μs |  1.28 |    0.22 |      - |   8,167 B |   1.86 KB |        1.00 |
| Padded128Interlocked | 2       |   415.88 μs |   8.384 μs |  12.289 μs |   394.02 μs |   439.85 μs |   430.86 μs |  0.82 |    0.14 |      - |   8,157 B |   1.84 KB |        0.99 |
|                      |         |             |            |            |             |             |             |       |         |        |           |           |             |
| **AdjacentVolatile**     | **4**       |   **853.96 μs** |   **3.876 μs** |   **5.802 μs** |   **843.82 μs** |   **866.11 μs** |   **860.80 μs** |  **1.00** |    **0.01** |      **-** |   **8,214 B** |   **2.29 KB** |        **1.00** |
| Padded64Volatile     | 4       |    93.73 μs |   2.866 μs |   4.110 μs |    86.92 μs |    99.78 μs |    98.40 μs |  0.11 |    0.00 | 0.1221 |   8,303 B |   2.26 KB |        0.99 |
| Padded128Volatile    | 4       |    92.94 μs |   2.686 μs |   4.020 μs |    86.64 μs |   101.14 μs |    98.17 μs |  0.11 |    0.00 | 0.1221 |   8,264 B |   2.26 KB |        0.99 |
| AdjacentInterlocked  | 4       | 1,680.32 μs |  14.875 μs |  21.803 μs | 1,640.44 μs | 1,735.36 μs | 1,711.23 μs |  1.97 |    0.03 |      - |   8,126 B |   2.29 KB |        1.00 |
| Padded128Interlocked | 4       |   444.70 μs |  17.814 μs |  25.549 μs |   403.88 μs |   479.90 μs |   474.49 μs |  0.52 |    0.03 |      - |   8,146 B |   2.26 KB |        0.99 |
|                      |         |             |            |            |             |             |             |       |         |        |           |           |             |
| **AdjacentVolatile**     | **8**       | **1,565.20 μs** |  **16.330 μs** |  **24.442 μs** | **1,540.09 μs** | **1,636.19 μs** | **1,606.52 μs** |  **1.00** |    **0.02** |      **-** |   **8,065 B** |   **3.15 KB** |        **1.00** |
| Padded64Volatile     | 8       |   150.59 μs |   1.636 μs |   2.449 μs |   145.23 μs |   156.26 μs |   153.02 μs |  0.10 |    0.00 |      - |   8,230 B |    3.1 KB |        0.98 |
| Padded128Volatile    | 8       |    52.66 μs |   0.826 μs |   1.236 μs |    50.64 μs |    54.98 μs |    54.39 μs |  0.03 |    0.00 | 0.1221 |   8,204 B |   2.84 KB |        0.90 |
| AdjacentInterlocked  | 8       | 6,612.19 μs | 100.006 μs | 149.685 μs | 6,288.13 μs | 6,886.52 μs | 6,762.34 μs |  4.23 |    0.11 |      - |   8,268 B |   3.15 KB |        1.00 |
| Padded128Interlocked | 8       |   673.50 μs |  13.180 μs |  19.319 μs |   618.57 μs |   709.61 μs |   692.95 μs |  0.43 |    0.01 |      - |   8,166 B |    3.1 KB |        0.98 |

