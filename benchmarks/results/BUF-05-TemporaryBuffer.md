# BUF-05: TemporaryBuffer

- Verdict: adopted (implemented)
- 0.11-0.32x at 4096 B (pool path), 0 B allocated
- 64 B stack path slightly slower than new byte[] (value is zero GC pressure, not latency)
- Faster than direct ArrayPool at small sizes (stackalloc path skips the pool)

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
