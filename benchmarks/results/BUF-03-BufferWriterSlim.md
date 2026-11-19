# BUF-03: BufferWriterSlim (stack-first sequential writer)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - WriterSlim stays the fastest at 0 B: 0.78x (64 B) and 0.91x (4 KB) of ArrayBufferWriter on net11 (0.75x / 0.92x on net10)

### BufferWriterSlimBenchmark

net10 → net11 per row:

| Method | TotalBytes | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| ArrayBufferWriter | 64 | 25.60 ns | 25.49 ns | 1.00 | 312 B → 312 B | 1,163 B → 1,207 B |
| PooledWriter | 64 | 24.67 ns | 26.31 ns | 1.07 | 32 B → 32 B | 4,670 B → 5,531 B |
| WriterSlim | 64 | 19.17 ns | 19.98 ns | 1.04 | - → - | 1,001 B → 1,159 B |
| ArrayBufferWriter | 4096 | 1,387.49 ns | 1,429.39 ns | 1.03 | 8056 B → 8056 B | 1,007 B → 1,165 B |
| PooledWriter | 4096 | 1,335.11 ns | 1,336.03 ns | 1.00 | 32 B → 32 B | 7,438 B → 5,881 B |
| WriterSlim | 4096 | 1,274.97 ns | 1,301.31 ns | 1.02 | - → - | 5,897 B → 5,878 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.42 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | TotalBytes | Mean        | Error     | StdDev    | Median      | Min         | Max         | P90         | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |----------- |------------:|----------:|----------:|------------:|------------:|------------:|------------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **ArrayBufferWriter** | **.NET 10.0**           | **.NET 10.0** | **64**         |    **25.60 ns** |  **0.183 ns** |  **0.268 ns** |    **25.60 ns** |    **25.11 ns** |    **26.17 ns** |    **25.95 ns** |  **1.00** |    **0.00** | **0.0373** |   **1,163 B** | **0.0000** |     **312 B** |        **1.00** |
| PooledWriter      | .NET 10.0           | .NET 10.0 | 64         |    24.67 ns |  0.118 ns |  0.174 ns |    24.68 ns |    24.38 ns |    25.03 ns |    24.94 ns |  0.96 |    0.01 | 0.0038 |   4,670 B |      - |      32 B |        0.10 |
| WriterSlim        | .NET 10.0           | .NET 10.0 | 64         |    19.17 ns |  0.146 ns |  0.214 ns |    19.14 ns |    18.77 ns |    19.68 ns |    19.43 ns |  0.75 |    0.01 |      - |   1,001 B |      - |         - |        0.00 |
|                   |                     |           |            |             |           |           |             |             |             |             |       |         |        |           |        |           |             |
| ArrayBufferWriter | MediumRun-.NET 11.0 | .NET 11.0 | 64         |    25.49 ns |  0.116 ns |  0.171 ns |    25.43 ns |    25.23 ns |    25.97 ns |    25.68 ns |  1.00 |    0.00 | 0.0373 |   1,207 B | 0.0000 |     312 B |        1.00 |
| PooledWriter      | MediumRun-.NET 11.0 | .NET 11.0 | 64         |    26.31 ns |  0.089 ns |  0.131 ns |    26.30 ns |    26.10 ns |    26.58 ns |    26.50 ns |  1.03 |    0.01 | 0.0038 |   5,531 B |      - |      32 B |        0.10 |
| WriterSlim        | MediumRun-.NET 11.0 | .NET 11.0 | 64         |    19.98 ns |  0.605 ns |  0.828 ns |    19.35 ns |    19.13 ns |    21.16 ns |    21.00 ns |  0.78 |    0.03 |      - |   1,159 B |      - |         - |        0.00 |
|                   |                     |           |            |             |           |           |             |             |             |             |       |         |        |           |        |           |             |
| **ArrayBufferWriter** | **.NET 10.0**           | **.NET 10.0** | **4096**       | **1,387.49 ns** |  **7.056 ns** | **10.562 ns** | **1,388.19 ns** | **1,370.02 ns** | **1,408.46 ns** | **1,403.99 ns** |  **1.00** |    **0.00** | **0.9632** |   **1,007 B** | **0.0038** |    **8056 B** |       **1.000** |
| PooledWriter      | .NET 10.0           | .NET 10.0 | 4096       | 1,335.11 ns |  7.081 ns |  9.693 ns | 1,330.26 ns | 1,324.56 ns | 1,355.14 ns | 1,350.54 ns |  0.96 |    0.01 | 0.0038 |   7,438 B |      - |      32 B |       0.004 |
| WriterSlim        | .NET 10.0           | .NET 10.0 | 4096       | 1,274.97 ns |  2.602 ns |  3.473 ns | 1,274.72 ns | 1,269.88 ns | 1,285.49 ns | 1,278.39 ns |  0.92 |    0.01 |      - |   5,897 B |      - |         - |       0.000 |
|                   |                     |           |            |             |           |           |             |             |             |             |       |         |        |           |        |           |             |
| ArrayBufferWriter | MediumRun-.NET 11.0 | .NET 11.0 | 4096       | 1,429.39 ns |  6.674 ns |  9.572 ns | 1,428.98 ns | 1,415.78 ns | 1,448.63 ns | 1,441.33 ns |  1.00 |    0.00 | 0.9632 |   1,165 B |      - |    8056 B |       1.000 |
| PooledWriter      | MediumRun-.NET 11.0 | .NET 11.0 | 4096       | 1,336.03 ns |  2.375 ns |  3.481 ns | 1,336.15 ns | 1,329.29 ns | 1,342.30 ns | 1,340.17 ns |  0.93 |    0.01 | 0.0038 |   5,881 B |      - |      32 B |       0.004 |
| WriterSlim        | MediumRun-.NET 11.0 | .NET 11.0 | 4096       | 1,301.31 ns | 10.149 ns | 15.190 ns | 1,297.42 ns | 1,280.22 ns | 1,338.94 ns | 1,319.34 ns |  0.91 |    0.01 |      - |   5,878 B |      - |         - |       0.000 |

## Earlier record

- Verdict: adopted (zero allocation and now also the fastest)
- 64 B payload (stack-only path): 19.0 ns / 0 B vs ArrayBufferWriter 25.2 ns / 312 B (0.76x)
- 4096 B (growth path): 1,283 ns / 0 B vs ArrayBufferWriter 1,427 ns / 8,056 B (0.90x, non-overlapping CIs)
- PooledBufferWriter (BUF-02, class) allocates only its own 32 B instance; choose Slim for sync scopes, Pooled when the writer must be stored or passed as IBufferWriter

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | TotalBytes | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |----------- |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| **ArrayBufferWriter** | **64**         |    **25.21 ns** |  **1.149 ns** |  **1.573 ns** |    **23.73 ns** |    **29.97 ns** |    **27.29 ns** |  **1.00** |    **0.08** |   **1,149 B** | **0.0373** |      **-** |     **312 B** |        **1.00** |
| PooledWriter      | 64         |    24.27 ns |  0.599 ns |  0.859 ns |    23.52 ns |    26.71 ns |    25.83 ns |  0.97 |    0.06 |   3,043 B | 0.0038 |      - |      32 B |        0.10 |
| WriterSlim        | 64         |    19.00 ns |  0.181 ns |  0.254 ns |    18.71 ns |    19.77 ns |    19.33 ns |  0.76 |    0.04 |   1,126 B |      - |      - |         - |        0.00 |
|                   |            |             |           |           |             |             |             |       |         |           |        |        |           |             |
| **ArrayBufferWriter** | **4096**       | **1,426.81 ns** | **37.702 ns** | **52.853 ns** | **1,361.65 ns** | **1,592.93 ns** | **1,482.28 ns** |  **1.00** |    **0.05** |     **997 B** | **0.9613** | **0.0019** |    **8056 B** |       **1.000** |
| PooledWriter      | 4096       | 1,327.72 ns | 13.496 ns | 18.919 ns | 1,309.94 ns | 1,388.15 ns | 1,345.92 ns |  0.93 |    0.03 |   5,141 B | 0.0038 |      - |      32 B |       0.004 |
| WriterSlim        | 4096       | 1,283.13 ns |  4.992 ns |  6.998 ns | 1,272.03 ns | 1,300.69 ns | 1,291.38 ns |  0.90 |    0.03 |   4,638 B |      - |      - |         - |       0.000 |
