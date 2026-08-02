# BIT-05: XxHash3 vs string.GetHashCode vs FNV-1a

- Verdict: adopted
- XxHash3: 0.34x (8 chars) / 0.25x (64) / 0.11x (512) vs string.GetHashCode
- MemoryMarshal.AsBytes cast path == fixed pointer path (zero-cost reinterpretation confirmed)
- Hand-rolled FNV-1a is SLOWER than string.GetHashCode from 64 chars (1.25x) - do not hand-roll
- Sampling hash (BIT-02) stays ~0.9 ns at any length (0.003x at 512) but only works for known key sets

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Length | Mean        | Error     | StdDev    | Median      | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------- |------------:|----------:|----------:|------------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| **StringGetHashCode** | **8**      |   **4.4546 ns** | **0.0793 ns** | **0.1188 ns** |   **4.4229 ns** |   **4.2611 ns** |   **4.7579 ns** |   **4.6182 ns** |  **1.00** |    **0.04** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 8      |   1.4930 ns | 0.0262 ns | 0.0384 ns |   1.4864 ns |   1.4328 ns |   1.6039 ns |   1.5319 ns |  0.34 |    0.01 |     471 B |         - |          NA |
| XxHash3Fixed      | 8      |   1.6694 ns | 0.0747 ns | 0.1118 ns |   1.6663 ns |   1.5243 ns |   1.8189 ns |   1.7946 ns |  0.38 |    0.03 |     489 B |         - |          NA |
| Fnv1a             | 8      |   2.8433 ns | 0.0550 ns | 0.0823 ns |   2.8290 ns |   2.7280 ns |   3.0600 ns |   2.9427 ns |  0.64 |    0.02 |      54 B |         - |          NA |
| SamplingHash      | 8      |   0.8380 ns | 0.0133 ns | 0.0194 ns |   0.8424 ns |   0.7941 ns |   0.8772 ns |   0.8607 ns |  0.19 |    0.01 |      79 B |         - |          NA |
|                   |        |             |           |           |             |             |             |             |       |         |           |           |             |
| **StringGetHashCode** | **64**     |  **33.2315 ns** | **0.2544 ns** | **0.3808 ns** |  **33.2618 ns** |  **32.5389 ns** |  **34.2586 ns** |  **33.5626 ns** |  **1.00** |    **0.02** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 64     |   8.1936 ns | 0.1362 ns | 0.1953 ns |   8.1336 ns |   7.9729 ns |   8.7136 ns |   8.4994 ns |  0.25 |    0.01 |     779 B |         - |          NA |
| XxHash3Fixed      | 64     |   8.0964 ns | 0.0688 ns | 0.1030 ns |   8.0733 ns |   7.9400 ns |   8.3395 ns |   8.2469 ns |  0.24 |    0.00 |     801 B |         - |          NA |
| Fnv1a             | 64     |  41.3898 ns | 0.2566 ns | 0.3762 ns |  41.2828 ns |  40.8356 ns |  42.1159 ns |  41.9025 ns |  1.25 |    0.02 |      54 B |         - |          NA |
| SamplingHash      | 64     |   0.8480 ns | 0.0251 ns | 0.0353 ns |   0.8366 ns |   0.7904 ns |   0.9280 ns |   0.8966 ns |  0.03 |    0.00 |      79 B |         - |          NA |
|                   |        |             |           |           |             |             |             |             |       |         |           |           |             |
| **StringGetHashCode** | **512**    | **281.6577 ns** | **2.1156 ns** | **3.1665 ns** | **282.2704 ns** | **276.7966 ns** | **287.3484 ns** | **284.9277 ns** | **1.000** |    **0.02** |     **321 B** |         **-** |          **NA** |
| XxHash3Cast       | 512    |  30.0255 ns | 0.3695 ns | 0.5180 ns |  29.9602 ns |  29.2049 ns |  30.7271 ns |  30.6106 ns | 0.107 |    0.00 |   1,041 B |         - |          NA |
| XxHash3Fixed      | 512    |  32.4974 ns | 0.4021 ns | 0.5894 ns |  32.4899 ns |  30.9995 ns |  33.3290 ns |  33.2285 ns | 0.115 |    0.00 |   1,063 B |         - |          NA |
| Fnv1a             | 512    | 436.3501 ns | 3.3120 ns | 4.9572 ns | 437.4261 ns | 428.7645 ns | 444.7632 ns | 442.4641 ns | 1.549 |    0.02 |      54 B |         - |          NA |
| SamplingHash      | 512    |   0.9119 ns | 0.0903 ns | 0.1324 ns |   0.8296 ns |   0.7592 ns |   1.0808 ns |   1.0530 ns | 0.003 |    0.00 |      79 B |         - |          NA |
