# MEM-06: Array range operator vs AsSpan (grow loop and single slice)

- Verdict: **adopted**. Use `AsSpan(0, n)` / `Slice` wherever a view is wanted; keep `array[..n]` for the places
  that want an independent copy
- First measurement in this repository, on the reference environment (HX 370, x86-64-v4, .NET 11.0.0 JIT)
- Grow loop to 64 KB in 4 KB steps: 2.25-2.27 μs / 61,536 B → 1.03-1.12 μs / 0 B (0.46-0.50x)
- Grow loop to 1 MB: 74.1-75.2 μs / 1,045,4xx B → 23.0-23.3 μs / 0 B (0.31x); **Gen2 175.8-177.6 per
  1,000 operations → 0**
- One 1 KB slice of a 4 KB array: 25.64 ns / 1,048 B → 0.81 ns / 0 B
- The range form inlines `GetSubArray`: an allocation helper call and two `Memmove` calls (into the new array,
  then into the grown buffer). The AsSpan form has a single `Memmove`

## ArrayRangeCopyBenchmark, run 1

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.83 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | TotalBytes | Mean      | Error     | StdDev    | Median    | Min        | Max       | P90       | Ratio | RatioSD | Code Size | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|---------------------- |----------- |----------:|----------:|----------:|----------:|-----------:|----------:|----------:|------:|--------:|----------:|---------:|---------:|---------:|----------:|------------:|
| **GrowWithRangeOperator** | **65536**      |  **2.266 μs** | **0.0200 μs** | **0.0287 μs** |  **2.263 μs** |  **2.1989 μs** |  **2.335 μs** |  **2.301 μs** |  **1.00** |    **0.00** |   **1,205 B** |   **7.3509** |        **-** |        **-** |   **61536 B** |        **1.00** |
| GrowWithAsSpan        | 65536      |  1.032 μs | 0.0584 μs | 0.0838 μs |  1.104 μs |  0.9383 μs |  1.116 μs |  1.113 μs |  0.46 |    0.04 |     866 B |        - |        - |        - |         - |        0.00 |
|                       |            |           |           |           |           |            |           |           |       |         |           |          |          |          |           |             |
| **GrowWithRangeOperator** | **1048576**    | **74.101 μs** | **1.5587 μs** | **2.1851 μs** | **73.696 μs** | **71.0948 μs** | **80.071 μs** | **77.323 μs** |  **1.00** |    **0.00** |   **4,718 B** | **191.2842** | **175.7813** | **175.7813** | **1045414 B** |        **1.00** |
| GrowWithAsSpan        | 1048576    | 23.019 μs | 0.1802 μs | 0.2642 μs | 23.082 μs | 22.6777 μs | 23.554 μs | 23.346 μs |  0.31 |    0.01 |   4,362 B |        - |        - |        - |         - |        0.00 |

## ArrayRangeCopyBenchmark, run 2

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.61 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | TotalBytes | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|---------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|---------:|---------:|---------:|----------:|------------:|
| **GrowWithRangeOperator** | **65536**      |  **2.247 μs** | **0.0213 μs** | **0.0292 μs** |  **2.193 μs** |  **2.311 μs** |  **2.285 μs** |  **1.00** |   **4,705 B** |   **7.3509** |        **-** |        **-** |   **61536 B** |        **1.00** |
| GrowWithAsSpan        | 65536      |  1.118 μs | 0.0084 μs | 0.0117 μs |  1.098 μs |  1.152 μs |  1.131 μs |  0.50 |   4,366 B |        - |        - |        - |         - |        0.00 |
|                       |            |           |           |           |           |           |           |       |           |          |          |          |           |             |
| **GrowWithRangeOperator** | **1048576**    | **75.169 μs** | **2.2550 μs** | **3.0867 μs** | **72.615 μs** | **85.384 μs** | **78.966 μs** |  **1.00** |   **4,718 B** | **193.1152** | **177.6123** | **177.6123** | **1045381 B** |        **1.00** |
| GrowWithAsSpan        | 1048576    | 23.319 μs | 0.1597 μs | 0.2238 μs | 22.990 μs | 23.654 μs | 23.610 μs |  0.31 |     862 B |        - |        - |        - |         - |        0.00 |

## ArraySliceStyleBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.83 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|----------:|-------:|----------:|------------:|
| ArrayRangeOperator | 25.6449 ns | 0.3275 ns | 0.4801 ns | 24.8813 ns | 26.8694 ns | 26.1331 ns |  1.00 |   1,090 B | 0.1253 |    1048 B |        1.00 |
| SpanSlice          |  0.8116 ns | 0.0066 ns | 0.0095 ns |  0.8014 ns |  0.8418 ns |  0.8243 ns |  0.03 |      50 B |      - |         - |        0.00 |

## Basis for the verdict

1. **Time:** the confidence intervals are disjoint at both sizes in both runs, and the single slice is 32x
   faster. The gain grows with the buffer because every grow copies the written region twice on the range form
2. **Allocation and GC:** the range form allocates one throwaway array per grow. At 1 MB the arrays pass the
   LOH threshold and every run triggers gen2 collections (176-178 per 1,000 operations); the AsSpan form
   allocates nothing and triggers none
3. **Generated code:** the two forms differ by the allocation helper and the second `Memmove`. The `Code Size`
   column moves between launches (862-4,718 B) depending on whether the static helper was inlined into the
   benchmark method, so compare the instruction sequences rather than that column
4. **Stability:** run 1 at 64 KB was bimodal across its two launches on the AsSpan side (Min 0.938 μs, Median
   1.104 μs); run 2 settled at 1.118 ± 0.012 μs. The 1 MB rows agree within 1.5% between the runs, so the
   modulo-4096 placement effect did not show on this machine
5. **Against the Zen 3 reference:** same direction and same order of magnitude. The 1 MB ratio is 0.31x here
   against 0.18x there, and the 32 B left on the AsSpan side there belonged to the library's writer object,
   which this benchmark does not allocate
