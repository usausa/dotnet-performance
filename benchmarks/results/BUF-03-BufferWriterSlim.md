# BUF-03: BufferWriterSlim (stack-first sequential writer)

- Verdict: adopted (allocation win; time parity)
- 64 B payload (stack-only path): 42.6 ns / 0 B vs ArrayBufferWriter 43.4 ns / 312 B - same speed, zero allocation
- 4096 B (growth path): 2,651 ns / 0 B vs ArrayBufferWriter 2,395 ns / 8,056 B - time CIs overlap (recorded as measurement-noise; codegen differs, 1,016 vs 4,681 B), allocation 8 KB -> 0
- PooledBufferWriter (BUF-02, class) allocates only its own 32 B instance; choose Slim for sync scopes, Pooled when the writer must be stored or passed as IBufferWriter

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | TotalBytes | Mean        | Error      | StdDev     | Median      | Min         | Max         | P90         | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------ |----------- |------------:|-----------:|-----------:|------------:|------------:|------------:|------------:|------:|--------:|-------:|----------:|----------:|------------:|
| **ArrayBufferWriter** | **64**         |    **43.35 ns** |   **1.591 ns** |   **2.282 ns** |    **42.92 ns** |    **40.31 ns** |    **49.23 ns** |    **46.68 ns** |  **1.00** |    **0.07** | **0.0186** |   **1,165 B** |     **312 B** |        **1.00** |
| PooledWriter      | 64         |    47.56 ns |   5.619 ns |   8.410 ns |    45.91 ns |    38.84 ns |    62.50 ns |    57.37 ns |  1.10 |    0.20 | 0.0019 |   3,059 B |      32 B |        0.10 |
| WriterSlim        | 64         |    42.57 ns |   1.681 ns |   2.464 ns |    43.20 ns |    37.44 ns |    46.91 ns |    45.33 ns |  0.98 |    0.07 |      - |   1,166 B |         - |        0.00 |
|                   |            |             |            |            |             |             |             |             |       |         |        |           |           |             |
| **ArrayBufferWriter** | **4096**       | **2,395.16 ns** | **323.277 ns** | **463.634 ns** | **2,099.02 ns** | **1,848.19 ns** | **3,257.00 ns** | **2,929.80 ns** |  **1.04** |    **0.28** | **0.4807** |   **1,016 B** |    **8056 B** |       **1.000** |
| PooledWriter      | 4096       | 2,634.60 ns | 103.411 ns | 154.780 ns | 2,632.69 ns | 2,345.43 ns | 2,945.58 ns | 2,870.30 ns |  1.14 |    0.22 |      - |   5,157 B |      32 B |       0.004 |
| WriterSlim        | 4096       | 2,650.85 ns |  80.854 ns | 121.018 ns | 2,669.08 ns | 2,373.81 ns | 2,818.22 ns | 2,786.01 ns |  1.15 |    0.22 |      - |   4,681 B |         - |       0.000 |
