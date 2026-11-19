# BUF-05: TemporaryBuffer

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted. The pool path is still 0.12x of `new byte[4096]` on net11 (6.83 vs 58.1 ns)
- **`ArrayPool<byte>.Shared` Rent/Return is 1.26-1.31x slower on net11** (5.25 → 6.87 ns at 64 B, 5.53 → 6.95 ns at 4 KB, disjoint intervals; code 4.0 → 5.1 KB). TemporaryBuffer's pool path follows it (5.22 → 6.83 ns); its 64 B stack path does not (2.64 ns)

### TemporaryBufferBenchmark

net10 → net11 per row:

| Method | Size | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| AllocateArray | 64 | 3.004 ns | 3.175 ns | 1.06 | 88 B → 88 B | 67 B → 67 B |
| TemporaryBuffer | 64 | 2.667 ns | 2.640 ns | 0.99 | - → - | 320 B → 320 B |
| ArrayPoolRent | 64 | 5.253 ns | 6.870 ns | 1.31 | - → - | 4,043 B → 5,086 B |
| AllocateArray | 4096 | 53.517 ns | 58.113 ns | 1.09 | 4120 B → 4120 B | 67 B → 67 B |
| TemporaryBuffer | 4096 | 5.223 ns | 6.830 ns | 1.31 | - → - | 4,447 B → 5,424 B |
| ArrayPoolRent | 4096 | 5.526 ns | 6.952 ns | 1.26 | - → - | 4,218 B → 5,263 B |

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
| Method          | Job                 | Runtime   | Size | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **AllocateArray**   | **.NET 10.0**           | **.NET 10.0** | **64**   |  **3.004 ns** | **0.0521 ns** | **0.0747 ns** |  **2.985 ns** |  **2.918 ns** |  **3.173 ns** |  **3.092 ns** |  **1.00** |    **0.00** |      **67 B** | **0.0105** |      **88 B** |        **1.00** |
| TemporaryBuffer | .NET 10.0           | .NET 10.0 | 64   |  2.667 ns | 0.0197 ns | 0.0276 ns |  2.655 ns |  2.629 ns |  2.709 ns |  2.701 ns |  0.89 |    0.02 |     320 B |      - |         - |        0.00 |
| ArrayPoolRent   | .NET 10.0           | .NET 10.0 | 64   |  5.253 ns | 0.0630 ns | 0.0863 ns |  5.254 ns |  5.142 ns |  5.418 ns |  5.342 ns |  1.75 |    0.05 |   4,043 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |           |           |           |       |         |           |        |           |             |
| AllocateArray   | MediumRun-.NET 11.0 | .NET 11.0 | 64   |  3.175 ns | 0.0629 ns | 0.0902 ns |  3.107 ns |  3.074 ns |  3.296 ns |  3.274 ns |  1.00 |    0.00 |      67 B | 0.0105 |      88 B |        1.00 |
| TemporaryBuffer | MediumRun-.NET 11.0 | .NET 11.0 | 64   |  2.640 ns | 0.0077 ns | 0.0115 ns |  2.641 ns |  2.624 ns |  2.671 ns |  2.653 ns |  0.83 |    0.02 |     320 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 11.0 | .NET 11.0 | 64   |  6.870 ns | 0.0162 ns | 0.0232 ns |  6.871 ns |  6.810 ns |  6.910 ns |  6.894 ns |  2.17 |    0.06 |   5,086 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |           |           |           |       |         |           |        |           |             |
| **AllocateArray**   | **.NET 10.0**           | **.NET 10.0** | **4096** | **53.517 ns** | **1.7569 ns** | **2.5198 ns** | **54.778 ns** | **50.573 ns** | **56.843 ns** | **56.323 ns** |  **1.00** |    **0.00** |      **67 B** | **0.4928** |    **4120 B** |        **1.00** |
| TemporaryBuffer | .NET 10.0           | .NET 10.0 | 4096 |  5.223 ns | 0.1562 ns | 0.2189 ns |  5.301 ns |  4.926 ns |  5.609 ns |  5.459 ns |  0.10 |    0.01 |   4,447 B |      - |         - |        0.00 |
| ArrayPoolRent   | .NET 10.0           | .NET 10.0 | 4096 |  5.526 ns | 0.1647 ns | 0.2254 ns |  5.366 ns |  5.267 ns |  5.846 ns |  5.771 ns |  0.10 |    0.01 |   4,218 B |      - |         - |        0.00 |
|                 |                     |           |      |           |           |           |           |           |           |           |       |         |           |        |           |             |
| AllocateArray   | MediumRun-.NET 11.0 | .NET 11.0 | 4096 | 58.113 ns | 0.5180 ns | 0.7429 ns | 58.214 ns | 55.849 ns | 59.334 ns | 58.880 ns |  1.00 |    0.00 |      67 B | 0.4928 |    4120 B |        1.00 |
| TemporaryBuffer | MediumRun-.NET 11.0 | .NET 11.0 | 4096 |  6.830 ns | 0.0289 ns | 0.0414 ns |  6.834 ns |  6.774 ns |  6.937 ns |  6.874 ns |  0.12 |    0.00 |   5,424 B |      - |         - |        0.00 |
| ArrayPoolRent   | MediumRun-.NET 11.0 | .NET 11.0 | 4096 |  6.952 ns | 0.0708 ns | 0.1060 ns |  6.951 ns |  6.799 ns |  7.088 ns |  7.074 ns |  0.12 |    0.00 |   5,263 B |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted (implemented)
- 0.09x at 4096 B (pool path, 50.0 -> 4.3 ns), 0 B allocated
- 64 B stack path slightly slower than new byte[] (2.3 vs 2.0 ns; the value is zero GC pressure, not latency)
- Faster than direct ArrayPool at small sizes (stackalloc path skips the pool: 2.3 vs 4.2 ns)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Size | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| **AllocateArray**   | **64**   |  **2.034 ns** | **0.0377 ns** | **0.0529 ns** |  **2.056 ns** |  **1.962 ns** |  **2.120 ns** |  **2.094 ns** |  **1.00** |    **0.04** | **0.0105** |      **67 B** |      **88 B** |        **1.00** |
| TemporaryBuffer | 64   |  2.344 ns | 0.1226 ns | 0.1678 ns |  2.217 ns |  2.185 ns |  2.763 ns |  2.523 ns |  1.15 |    0.09 |      - |     474 B |         - |        0.00 |
| ArrayPoolRent   | 64   |  4.234 ns | 0.0166 ns | 0.0243 ns |  4.227 ns |  4.203 ns |  4.285 ns |  4.268 ns |  2.08 |    0.05 |      - |   2,493 B |         - |        0.00 |
|                 |      |           |           |           |           |           |           |           |       |         |        |           |           |             |
| **AllocateArray**   | **4096** | **50.009 ns** | **1.3431 ns** | **2.0103 ns** | **50.597 ns** | **44.312 ns** | **52.960 ns** | **52.394 ns** |  **1.00** |    **0.06** | **0.4923** |      **67 B** |    **4120 B** |        **1.00** |
| TemporaryBuffer | 4096 |  4.257 ns | 0.0149 ns | 0.0208 ns |  4.258 ns |  4.218 ns |  4.307 ns |  4.273 ns |  0.09 |    0.00 |      - |   2,695 B |         - |        0.00 |
| ArrayPoolRent   | 4096 |  4.321 ns | 0.0475 ns | 0.0697 ns |  4.351 ns |  4.207 ns |  4.428 ns |  4.393 ns |  0.09 |    0.00 |      - |   2,686 B |         - |        0.00 |
