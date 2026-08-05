# BIT-04: XxHash3 vs string.GetHashCode vs FNV-1a

- Verdict: adopted
- XxHash3: 0.26x (8 chars) / 0.21x (64) / 0.09x (512) vs string.GetHashCode
- MemoryMarshal.AsBytes cast path is equivalent to fixed (no pinning required, so prefer the cast) - zero-cost reinterpretation confirmed
- Hand-rolled FNV-1a is SLOWER than string.GetHashCode from 64 chars (1.05x at 64, 1.53x at 512) - do not hand-roll
- Sampling hash (BIT-01) stays ~0.23 ns at any length (0.001x at 512) but only works for known key sets

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Length | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------- |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| **StringGetHashCode** | **8**      |   **3.8378 ns** | **0.0317 ns** | **0.0444 ns** |   **3.7963 ns** |   **3.9533 ns** |   **3.9083 ns** |  **1.00** |    **0.02** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 8      |   0.9858 ns | 0.0109 ns | 0.0149 ns |   0.9707 ns |   1.0328 ns |   1.0007 ns |  0.26 |    0.00 |     471 B |         - |          NA |
| XxHash3Fixed      | 8      |   0.9473 ns | 0.0192 ns | 0.0263 ns |   0.9159 ns |   1.0192 ns |   0.9893 ns |  0.25 |    0.01 |     489 B |         - |          NA |
| Fnv1a             | 8      |   1.7350 ns | 0.0867 ns | 0.1215 ns |   1.6488 ns |   2.0624 ns |   1.9510 ns |  0.45 |    0.03 |      54 B |         - |          NA |
| SamplingHash      | 8      |   0.2246 ns | 0.0043 ns | 0.0062 ns |   0.2157 ns |   0.2380 ns |   0.2328 ns |  0.06 |    0.00 |      79 B |         - |          NA |
|                   |        |             |           |           |             |             |             |       |         |           |           |             |
| **StringGetHashCode** | **64**     |  **28.9965 ns** | **0.1830 ns** | **0.2443 ns** |  **28.7768 ns** |  **29.7969 ns** |  **29.1044 ns** | **1.000** |    **0.01** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 64     |   6.0301 ns | 0.0816 ns | 0.1090 ns |   5.9386 ns |   6.4458 ns |   6.1038 ns | 0.208 |    0.00 |     779 B |         - |          NA |
| XxHash3Fixed      | 64     |   5.9982 ns | 0.0337 ns | 0.0461 ns |   5.9248 ns |   6.1522 ns |   6.0466 ns | 0.207 |    0.00 |     801 B |         - |          NA |
| Fnv1a             | 64     |  30.3434 ns | 0.2400 ns | 0.3365 ns |  29.9548 ns |  31.4205 ns |  30.8390 ns | 1.047 |    0.01 |      54 B |         - |          NA |
| SamplingHash      | 64     |   0.2265 ns | 0.0055 ns | 0.0075 ns |   0.2157 ns |   0.2422 ns |   0.2377 ns | 0.008 |    0.00 |      79 B |         - |          NA |
|                   |        |             |           |           |             |             |             |       |         |           |           |             |
| **StringGetHashCode** | **512**    | **249.5169 ns** | **1.2440 ns** | **1.7439 ns** | **247.6708 ns** | **254.9917 ns** | **251.5221 ns** | **1.000** |    **0.01** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 512    |  21.1107 ns | 2.7649 ns | 3.7847 ns |  17.1888 ns |  25.9780 ns |  24.7836 ns | 0.085 |    0.01 |     961 B |         - |          NA |
| XxHash3Fixed      | 512    |  16.9231 ns | 0.0711 ns | 0.0973 ns |  16.7555 ns |  17.1793 ns |  17.0264 ns | 0.068 |    0.00 |     983 B |         - |          NA |
| Fnv1a             | 512    | 381.3360 ns | 1.5705 ns | 2.2017 ns | 379.5605 ns | 387.5817 ns | 384.4652 ns | 1.528 |    0.01 |      54 B |         - |          NA |
| SamplingHash      | 512    |   0.2319 ns | 0.0133 ns | 0.0177 ns |   0.2154 ns |   0.2823 ns |   0.2554 ns | 0.001 |    0.00 |      79 B |         - |          NA |
