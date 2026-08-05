# BUF-03: BufferWriterSlim (stack-first sequential writer)

- Verdict: adopted (allocation win; time parity)
- 64 B payload (stack-only path): 42.6 ns / 0 B vs ArrayBufferWriter 43.4 ns / 312 B - same speed, zero allocation
- 4096 B (growth path): 2,651 ns / 0 B vs ArrayBufferWriter 2,395 ns / 8,056 B - time CIs overlap (recorded as measurement-noise; codegen differs, 1,016 vs 4,681 B), allocation 8 KB -> 0
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
