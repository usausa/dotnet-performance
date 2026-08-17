# BIT-05: Order-preserving 8-byte digest probes in a binary search

- Use case: **storage-engine index blocks** - binary search over a sorted run of variable-length keys, where the structure has to stay ordered because range scans, prefix scans and successor lookups go through it too. If ordering is not required, a hash table beats binary search outright and this pattern has no place
- Verdict: conditional (adopt only when keys are known to diverge inside the first 8 bytes - the worst case is a real 1.2-1.4x regression, not a wash)
- Random keys: hit 0.74x (64) / 0.68x (256) / **0.65x** (1024), miss 0.64x / 0.57x / **0.54x**. The gain grows with table size because a deeper search means more probes that never touch key bytes
- Shared 8-byte prefix (every digest ties): hit **1.31x / 1.27x / 1.30x**, miss **1.36x / 1.26x / 1.22x**. The digest is computed, compared, and then thrown away on every probe, so it is pure added work
- Misses benefit most in the good case (0.54x at 1024) - a miss walks the full depth of the search and never needs the key bytes at all
- Code size roughly doubles (672-696 B -> 1,397-1,441 B) because the digest packing inlines into both probe sites
- The deciding question is the key population, not the table size: identifier-like keys with a common namespace prefix land in the regression case. Measure the actual prefix distribution before adopting

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | KeyCount | Shape        | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |--------- |------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **HitFullCompare**  | **64**       | **Random**       | **11.078 ns** | **0.1910 ns** | **0.2800 ns** | **11.033 ns** | **10.667 ns** | **11.945 ns** | **11.355 ns** |  **1.00** |    **0.03** |     **690 B** |         **-** |          **NA** |
| HitDigest       | 64       | Random       |  8.167 ns | 0.1409 ns | 0.2109 ns |  8.096 ns |  7.946 ns |  8.921 ns |  8.444 ns |  0.74 |    0.03 |   1,397 B |         - |          NA |
| MissFullCompare | 64       | Random       | 10.538 ns | 0.0718 ns | 0.1030 ns | 10.512 ns | 10.400 ns | 10.753 ns | 10.695 ns |  0.95 |    0.02 |     693 B |         - |          NA |
| MissDigest      | 64       | Random       |  6.722 ns | 0.0427 ns | 0.0571 ns |  6.724 ns |  6.618 ns |  6.847 ns |  6.786 ns |  0.61 |    0.02 |   1,119 B |         - |          NA |
|                 |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **64**       | **SharedPrefix** | **10.604 ns** | **0.0804 ns** | **0.1153 ns** | **10.639 ns** | **10.354 ns** | **10.761 ns** | **10.717 ns** |  **1.00** |    **0.02** |     **685 B** |         **-** |          **NA** |
| HitDigest       | 64       | SharedPrefix | 13.872 ns | 0.2759 ns | 0.3957 ns | 13.752 ns | 13.453 ns | 14.921 ns | 14.365 ns |  1.31 |    0.04 |   1,427 B |         - |          NA |
| MissFullCompare | 64       | SharedPrefix | 11.545 ns | 0.0799 ns | 0.1093 ns | 11.560 ns | 11.330 ns | 11.775 ns | 11.714 ns |  1.09 |    0.02 |     690 B |         - |          NA |
| MissDigest      | 64       | SharedPrefix | 15.742 ns | 0.4105 ns | 0.6017 ns | 15.535 ns | 15.157 ns | 17.296 ns | 16.972 ns |  1.48 |    0.06 |   1,441 B |         - |          NA |
|                 |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **256**      | **Random**       | **14.222 ns** | **0.3549 ns** | **0.4976 ns** | **14.187 ns** | **13.566 ns** | **15.483 ns** | **14.841 ns** |  **1.00** |    **0.05** |     **672 B** |         **-** |          **NA** |
| HitDigest       | 256      | Random       |  9.643 ns | 0.0634 ns | 0.0910 ns |  9.654 ns |  9.457 ns |  9.771 ns |  9.741 ns |  0.68 |    0.02 |   1,411 B |         - |          NA |
| MissFullCompare | 256      | Random       | 14.226 ns | 0.1469 ns | 0.2059 ns | 14.180 ns | 13.979 ns | 14.878 ns | 14.475 ns |  1.00 |    0.04 |     696 B |         - |          NA |
| MissDigest      | 256      | Random       |  8.136 ns | 0.0687 ns | 0.0940 ns |  8.120 ns |  8.030 ns |  8.469 ns |  8.192 ns |  0.57 |    0.02 |   1,119 B |         - |          NA |
|                 |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **256**      | **SharedPrefix** | **14.256 ns** | **0.1781 ns** | **0.2497 ns** | **14.274 ns** | **13.967 ns** | **14.788 ns** | **14.629 ns** |  **1.00** |    **0.02** |     **685 B** |         **-** |          **NA** |
| HitDigest       | 256      | SharedPrefix | 18.084 ns | 0.1053 ns | 0.1476 ns | 18.093 ns | 17.896 ns | 18.508 ns | 18.239 ns |  1.27 |    0.02 |   1,427 B |         - |          NA |
| MissFullCompare | 256      | SharedPrefix | 15.880 ns | 0.1416 ns | 0.2031 ns | 15.852 ns | 15.585 ns | 16.262 ns | 16.142 ns |  1.11 |    0.02 |     689 B |         - |          NA |
| MissDigest      | 256      | SharedPrefix | 19.988 ns | 0.1408 ns | 0.2019 ns | 19.975 ns | 19.674 ns | 20.519 ns | 20.193 ns |  1.40 |    0.03 |   1,428 B |         - |          NA |
|                 |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **1024**     | **Random**       | **17.784 ns** | **0.2837 ns** | **0.4069 ns** | **17.857 ns** | **17.174 ns** | **18.412 ns** | **18.357 ns** |  **1.00** |    **0.03** |     **690 B** |         **-** |          **NA** |
| HitDigest       | 1024     | Random       | 11.502 ns | 0.4095 ns | 0.5741 ns | 11.895 ns | 10.797 ns | 12.331 ns | 12.138 ns |  0.65 |    0.03 |   1,397 B |         - |          NA |
| MissFullCompare | 1024     | Random       | 17.909 ns | 0.1429 ns | 0.2050 ns | 17.855 ns | 17.602 ns | 18.314 ns | 18.206 ns |  1.01 |    0.03 |     693 B |         - |          NA |
| MissDigest      | 1024     | Random       |  9.759 ns | 0.3241 ns | 0.4649 ns | 10.069 ns |  9.212 ns | 10.429 ns | 10.266 ns |  0.55 |    0.03 |   1,128 B |         - |          NA |
|                 |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **1024**     | **SharedPrefix** | **17.737 ns** | **0.3164 ns** | **0.4735 ns** | **17.631 ns** | **17.117 ns** | **18.950 ns** | **18.409 ns** |  **1.00** |    **0.04** |     **686 B** |         **-** |          **NA** |
| HitDigest       | 1024     | SharedPrefix | 23.108 ns | 0.1404 ns | 0.2058 ns | 23.110 ns | 22.667 ns | 23.538 ns | 23.337 ns |  1.30 |    0.04 |   1,434 B |         - |          NA |
| MissFullCompare | 1024     | SharedPrefix | 20.392 ns | 0.3222 ns | 0.4621 ns | 20.446 ns | 19.216 ns | 20.962 ns | 20.818 ns |  1.15 |    0.04 |     685 B |         - |          NA |
| MissDigest      | 1024     | SharedPrefix | 24.949 ns | 0.4250 ns | 0.6229 ns | 24.818 ns | 24.195 ns | 27.096 ns | 25.652 ns |  1.41 |    0.05 |   1,441 B |         - |          NA |

Ratio is against `HitFullCompare` within each group. The miss axis reads `MissDigest` against `MissFullCompare`: Random 0.64x / 0.57x / 0.54x, SharedPrefix 1.36x / 1.26x / 1.22x.

BenchmarkDotNet flagged `HitDigest` (mValue 3.86) and `MissDigest` (mValue 3.73) as bimodal - visible as the Mean/Median gap in the 1024/Random rows. The gap is well inside the effect being measured (0.54-0.65x), so the direction stands, but treat the third digit as unsettled.
