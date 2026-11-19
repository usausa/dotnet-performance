# BIT-05: Order-preserving 8-byte digest probes in a binary search

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - conditional. Random keys on net11: hit 0.76x / 0.71x / 0.63x, miss 0.63x / 0.56x / 0.51x at 64 / 256 / 1024; shared 8-byte prefix: 1.22-1.30x (net10 1.23-1.29x)
- At 1024 random keys the full-compare hit got 0.87x faster on net11 (19.52 → 17.02 ns), so the digest gain there narrows from 0.54x to 0.63x
- The digest miss probe shrank from 1,756 to about 1,000 B on net11 with no time change

### OrderedDigestSearchBenchmark

net10 → net11 per row:

| Method | KeyCount | Shape | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|---|
| HitFullCompare | 64 | Random | 10.463 ns | 10.091 ns | 0.96 | - → - | 672 B → 715 B |
| HitDigest | 64 | Random | 7.764 ns | 7.713 ns | 0.99 | - → - | 1,442 B → 1,443 B |
| MissFullCompare | 64 | Random | 9.932 ns | 10.249 ns | 1.03 | - → - | 696 B → 721 B |
| MissDigest | 64 | Random | 6.540 ns | 6.435 ns | 0.98 | - → - | 1,758 B → 1,001 B |
| HitFullCompare | 64 | SharedPrefix | 10.252 ns | 10.505 ns | 1.02 | - → - | 685 B → 710 B |
| HitDigest | 64 | SharedPrefix | 13.028 ns | 13.521 ns | 1.04 | - → - | 1,428 B → 1,464 B |
| MissFullCompare | 64 | SharedPrefix | 11.118 ns | 11.296 ns | 1.02 | - → - | 693 B → 718 B |
| MissDigest | 64 | SharedPrefix | 14.311 ns | 14.719 ns | 1.03 | - → - | 1,428 B → 1,460 B |
| HitFullCompare | 256 | Random | 13.438 ns | 13.118 ns | 0.98 | - → - | 691 B → 715 B |
| HitDigest | 256 | Random | 9.048 ns | 9.294 ns | 1.03 | - → - | 1,388 B → 1,431 B |
| MissFullCompare | 256 | Random | 14.108 ns | 13.780 ns | 0.98 | - → - | 686 B → 713 B |
| MissDigest | 256 | Random | 7.776 ns | 7.776 ns | 1.00 | - → - | 1,756 B → 1,006 B |
| HitFullCompare | 256 | SharedPrefix | 13.374 ns | 14.038 ns | 1.05 | - → - | 685 B → 710 B |
| HitDigest | 256 | SharedPrefix | 16.719 ns | 17.063 ns | 1.02 | - → - | 1,428 B → 1,449 B |
| MissFullCompare | 256 | SharedPrefix | 14.731 ns | 15.486 ns | 1.05 | - → - | 689 B → 709 B |
| MissDigest | 256 | SharedPrefix | 18.523 ns | 19.761 ns | 1.07 | - → - | 1,414 B → 1,449 B |
| HitFullCompare | 1024 | Random | 19.517 ns | 17.018 ns | 0.87 | - → - | 690 B → 715 B |
| HitDigest | 1024 | Random | 10.604 ns | 10.646 ns | 1.00 | - → - | 1,389 B → 1,443 B |
| MissFullCompare | 1024 | Random | 17.223 ns | 17.618 ns | 1.02 | - → - | 696 B → 713 B |
| MissDigest | 1024 | Random | 9.042 ns | 9.052 ns | 1.00 | - → - | 1,756 B → 1,002 B |
| HitFullCompare | 1024 | SharedPrefix | 17.491 ns | 17.885 ns | 1.02 | - → - | 678 B → 708 B |
| HitDigest | 1024 | SharedPrefix | 21.569 ns | 22.221 ns | 1.03 | - → - | 1,421 B → 1,464 B |
| MissFullCompare | 1024 | SharedPrefix | 18.889 ns | 19.798 ns | 1.05 | - → - | 682 B → 720 B |
| MissDigest | 1024 | SharedPrefix | 23.382 ns | 24.167 ns | 1.03 | - → - | 1,428 B → 1,458 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.19 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method          | Job                 | Runtime   | KeyCount | Shape        | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |--------- |------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **HitFullCompare**  | **.NET 10.0**           | **.NET 10.0** | **64**       | **Random**       | **10.463 ns** | **0.6114 ns** | **0.8962 ns** |  **9.745 ns** |  **9.523 ns** | **11.707 ns** | **11.422 ns** |  **1.00** |    **0.00** |     **672 B** |         **-** |          **NA** |
| HitDigest       | .NET 10.0           | .NET 10.0 | 64       | Random       |  7.764 ns | 0.0644 ns | 0.0924 ns |  7.762 ns |  7.620 ns |  7.999 ns |  7.865 ns |  0.75 |    0.06 |   1,442 B |         - |          NA |
| MissFullCompare | .NET 10.0           | .NET 10.0 | 64       | Random       |  9.932 ns | 0.1986 ns | 0.2911 ns | 10.048 ns |  9.605 ns | 10.338 ns | 10.275 ns |  0.96 |    0.08 |     696 B |         - |          NA |
| MissDigest      | .NET 10.0           | .NET 10.0 | 64       | Random       |  6.540 ns | 0.0417 ns | 0.0611 ns |  6.542 ns |  6.445 ns |  6.631 ns |  6.609 ns |  0.63 |    0.05 |   1,758 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| HitFullCompare  | MediumRun-.NET 11.0 | .NET 11.0 | 64       | Random       | 10.091 ns | 0.0565 ns | 0.0810 ns | 10.087 ns |  9.928 ns | 10.245 ns | 10.214 ns |  1.00 |    0.00 |     715 B |         - |          NA |
| HitDigest       | MediumRun-.NET 11.0 | .NET 11.0 | 64       | Random       |  7.713 ns | 0.0241 ns | 0.0346 ns |  7.710 ns |  7.658 ns |  7.800 ns |  7.756 ns |  0.76 |    0.01 |   1,443 B |         - |          NA |
| MissFullCompare | MediumRun-.NET 11.0 | .NET 11.0 | 64       | Random       | 10.249 ns | 0.0372 ns | 0.0521 ns | 10.250 ns | 10.161 ns | 10.375 ns | 10.314 ns |  1.02 |    0.01 |     721 B |         - |          NA |
| MissDigest      | MediumRun-.NET 11.0 | .NET 11.0 | 64       | Random       |  6.435 ns | 0.0410 ns | 0.0574 ns |  6.459 ns |  6.351 ns |  6.522 ns |  6.501 ns |  0.64 |    0.01 |   1,001 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **.NET 10.0**           | **.NET 10.0** | **64**       | **SharedPrefix** | **10.252 ns** | **0.1510 ns** | **0.2213 ns** | **10.136 ns** |  **9.953 ns** | **10.528 ns** | **10.497 ns** |  **1.00** |    **0.00** |     **685 B** |         **-** |          **NA** |
| HitDigest       | .NET 10.0           | .NET 10.0 | 64       | SharedPrefix | 13.028 ns | 0.1115 ns | 0.1563 ns | 12.965 ns | 12.866 ns | 13.427 ns | 13.242 ns |  1.27 |    0.03 |   1,428 B |         - |          NA |
| MissFullCompare | .NET 10.0           | .NET 10.0 | 64       | SharedPrefix | 11.118 ns | 0.3035 ns | 0.4449 ns | 11.475 ns | 10.622 ns | 11.588 ns | 11.566 ns |  1.08 |    0.05 |     693 B |         - |          NA |
| MissDigest      | .NET 10.0           | .NET 10.0 | 64       | SharedPrefix | 14.311 ns | 0.0478 ns | 0.0700 ns | 14.304 ns | 14.202 ns | 14.502 ns | 14.389 ns |  1.40 |    0.03 |   1,428 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| HitFullCompare  | MediumRun-.NET 11.0 | .NET 11.0 | 64       | SharedPrefix | 10.505 ns | 0.0408 ns | 0.0598 ns | 10.524 ns | 10.399 ns | 10.589 ns | 10.578 ns |  1.00 |    0.00 |     710 B |         - |          NA |
| HitDigest       | MediumRun-.NET 11.0 | .NET 11.0 | 64       | SharedPrefix | 13.521 ns | 0.0864 ns | 0.1266 ns | 13.543 ns | 13.332 ns | 13.764 ns | 13.671 ns |  1.29 |    0.01 |   1,464 B |         - |          NA |
| MissFullCompare | MediumRun-.NET 11.0 | .NET 11.0 | 64       | SharedPrefix | 11.296 ns | 0.0674 ns | 0.0988 ns | 11.327 ns | 11.150 ns | 11.479 ns | 11.401 ns |  1.08 |    0.01 |     718 B |         - |          NA |
| MissDigest      | MediumRun-.NET 11.0 | .NET 11.0 | 64       | SharedPrefix | 14.719 ns | 0.1494 ns | 0.2095 ns | 14.657 ns | 14.462 ns | 15.005 ns | 14.953 ns |  1.40 |    0.02 |   1,460 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **.NET 10.0**           | **.NET 10.0** | **256**      | **Random**       | **13.438 ns** | **0.5748 ns** | **0.8243 ns** | **12.786 ns** | **12.531 ns** | **14.391 ns** | **14.332 ns** |  **1.00** |    **0.00** |     **691 B** |         **-** |          **NA** |
| HitDigest       | .NET 10.0           | .NET 10.0 | 256      | Random       |  9.048 ns | 0.0537 ns | 0.0736 ns |  9.025 ns |  8.966 ns |  9.267 ns |  9.144 ns |  0.68 |    0.04 |   1,388 B |         - |          NA |
| MissFullCompare | .NET 10.0           | .NET 10.0 | 256      | Random       | 14.108 ns | 0.1123 ns | 0.1646 ns | 14.043 ns | 13.886 ns | 14.385 ns | 14.363 ns |  1.05 |    0.06 |     686 B |         - |          NA |
| MissDigest      | .NET 10.0           | .NET 10.0 | 256      | Random       |  7.776 ns | 0.0426 ns | 0.0598 ns |  7.766 ns |  7.683 ns |  7.907 ns |  7.846 ns |  0.58 |    0.04 |   1,756 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| HitFullCompare  | MediumRun-.NET 11.0 | .NET 11.0 | 256      | Random       | 13.118 ns | 0.0921 ns | 0.1291 ns | 13.131 ns | 12.886 ns | 13.403 ns | 13.266 ns |  1.00 |    0.00 |     715 B |         - |          NA |
| HitDigest       | MediumRun-.NET 11.0 | .NET 11.0 | 256      | Random       |  9.294 ns | 0.0888 ns | 0.1329 ns |  9.318 ns |  9.113 ns |  9.537 ns |  9.456 ns |  0.71 |    0.01 |   1,431 B |         - |          NA |
| MissFullCompare | MediumRun-.NET 11.0 | .NET 11.0 | 256      | Random       | 13.780 ns | 0.0522 ns | 0.0781 ns | 13.779 ns | 13.651 ns | 13.947 ns | 13.900 ns |  1.05 |    0.01 |     713 B |         - |          NA |
| MissDigest      | MediumRun-.NET 11.0 | .NET 11.0 | 256      | Random       |  7.776 ns | 0.0292 ns | 0.0409 ns |  7.774 ns |  7.705 ns |  7.846 ns |  7.833 ns |  0.59 |    0.01 |   1,006 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **.NET 10.0**           | **.NET 10.0** | **256**      | **SharedPrefix** | **13.374 ns** | **0.1220 ns** | **0.1749 ns** | **13.333 ns** | **13.135 ns** | **13.747 ns** | **13.593 ns** |  **1.00** |    **0.00** |     **685 B** |         **-** |          **NA** |
| HitDigest       | .NET 10.0           | .NET 10.0 | 256      | SharedPrefix | 16.719 ns | 0.0487 ns | 0.0683 ns | 16.713 ns | 16.604 ns | 16.881 ns | 16.789 ns |  1.25 |    0.02 |   1,428 B |         - |          NA |
| MissFullCompare | .NET 10.0           | .NET 10.0 | 256      | SharedPrefix | 14.731 ns | 0.0596 ns | 0.0796 ns | 14.703 ns | 14.629 ns | 14.916 ns | 14.851 ns |  1.10 |    0.02 |     689 B |         - |          NA |
| MissDigest      | .NET 10.0           | .NET 10.0 | 256      | SharedPrefix | 18.523 ns | 0.0622 ns | 0.0830 ns | 18.517 ns | 18.404 ns | 18.769 ns | 18.620 ns |  1.39 |    0.02 |   1,414 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| HitFullCompare  | MediumRun-.NET 11.0 | .NET 11.0 | 256      | SharedPrefix | 14.038 ns | 0.0816 ns | 0.1222 ns | 14.040 ns | 13.822 ns | 14.273 ns | 14.211 ns |  1.00 |    0.00 |     710 B |         - |          NA |
| HitDigest       | MediumRun-.NET 11.0 | .NET 11.0 | 256      | SharedPrefix | 17.063 ns | 0.0387 ns | 0.0543 ns | 17.061 ns | 16.982 ns | 17.200 ns | 17.133 ns |  1.22 |    0.01 |   1,449 B |         - |          NA |
| MissFullCompare | MediumRun-.NET 11.0 | .NET 11.0 | 256      | SharedPrefix | 15.486 ns | 0.0892 ns | 0.1280 ns | 15.482 ns | 15.311 ns | 15.799 ns | 15.665 ns |  1.10 |    0.01 |     709 B |         - |          NA |
| MissDigest      | MediumRun-.NET 11.0 | .NET 11.0 | 256      | SharedPrefix | 19.761 ns | 0.0557 ns | 0.0762 ns | 19.762 ns | 19.645 ns | 19.909 ns | 19.862 ns |  1.41 |    0.01 |   1,449 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **.NET 10.0**           | **.NET 10.0** | **1024**     | **Random**       | **19.517 ns** | **0.6348 ns** | **0.9104 ns** | **20.171 ns** | **18.335 ns** | **20.662 ns** | **20.434 ns** |  **1.00** |    **0.00** |     **690 B** |         **-** |          **NA** |
| HitDigest       | .NET 10.0           | .NET 10.0 | 1024     | Random       | 10.604 ns | 0.0316 ns | 0.0454 ns | 10.589 ns | 10.538 ns | 10.746 ns | 10.656 ns |  0.54 |    0.03 |   1,389 B |         - |          NA |
| MissFullCompare | .NET 10.0           | .NET 10.0 | 1024     | Random       | 17.223 ns | 0.0617 ns | 0.0865 ns | 17.223 ns | 17.069 ns | 17.398 ns | 17.336 ns |  0.88 |    0.04 |     696 B |         - |          NA |
| MissDigest      | .NET 10.0           | .NET 10.0 | 1024     | Random       |  9.042 ns | 0.0360 ns | 0.0505 ns |  9.036 ns |  8.977 ns |  9.169 ns |  9.113 ns |  0.46 |    0.02 |   1,756 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| HitFullCompare  | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | Random       | 17.018 ns | 0.0611 ns | 0.0876 ns | 17.034 ns | 16.864 ns | 17.149 ns | 17.124 ns |  1.00 |    0.00 |     715 B |         - |          NA |
| HitDigest       | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | Random       | 10.646 ns | 0.0291 ns | 0.0398 ns | 10.639 ns | 10.582 ns | 10.759 ns | 10.695 ns |  0.63 |    0.00 |   1,443 B |         - |          NA |
| MissFullCompare | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | Random       | 17.618 ns | 0.0629 ns | 0.0882 ns | 17.617 ns | 17.481 ns | 17.819 ns | 17.701 ns |  1.04 |    0.01 |     713 B |         - |          NA |
| MissDigest      | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | Random       |  9.052 ns | 0.0267 ns | 0.0391 ns |  9.050 ns |  8.998 ns |  9.137 ns |  9.099 ns |  0.53 |    0.00 |   1,002 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| **HitFullCompare**  | **.NET 10.0**           | **.NET 10.0** | **1024**     | **SharedPrefix** | **17.491 ns** | **0.0653 ns** | **0.0916 ns** | **17.475 ns** | **17.346 ns** | **17.637 ns** | **17.608 ns** |  **1.00** |    **0.00** |     **678 B** |         **-** |          **NA** |
| HitDigest       | .NET 10.0           | .NET 10.0 | 1024     | SharedPrefix | 21.569 ns | 0.2183 ns | 0.3268 ns | 21.622 ns | 21.115 ns | 21.974 ns | 21.936 ns |  1.23 |    0.02 |   1,421 B |         - |          NA |
| MissFullCompare | .NET 10.0           | .NET 10.0 | 1024     | SharedPrefix | 18.889 ns | 0.0508 ns | 0.0728 ns | 18.885 ns | 18.788 ns | 19.028 ns | 18.997 ns |  1.08 |    0.01 |     682 B |         - |          NA |
| MissDigest      | .NET 10.0           | .NET 10.0 | 1024     | SharedPrefix | 23.382 ns | 0.1064 ns | 0.1491 ns | 23.416 ns | 23.112 ns | 23.639 ns | 23.576 ns |  1.34 |    0.01 |   1,428 B |         - |          NA |
|                 |                     |           |          |              |           |           |           |           |           |           |           |       |         |           |           |             |
| HitFullCompare  | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | SharedPrefix | 17.885 ns | 0.1775 ns | 0.2429 ns | 17.795 ns | 17.703 ns | 18.788 ns | 18.153 ns |  1.00 |    0.00 |     708 B |         - |          NA |
| HitDigest       | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | SharedPrefix | 22.221 ns | 0.1451 ns | 0.2081 ns | 22.215 ns | 21.908 ns | 22.645 ns | 22.473 ns |  1.24 |    0.02 |   1,464 B |         - |          NA |
| MissFullCompare | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | SharedPrefix | 19.798 ns | 0.0882 ns | 0.1293 ns | 19.780 ns | 19.583 ns | 20.117 ns | 19.952 ns |  1.11 |    0.02 |     720 B |         - |          NA |
| MissDigest      | MediumRun-.NET 11.0 | .NET 11.0 | 1024     | SharedPrefix | 24.167 ns | 0.0683 ns | 0.0957 ns | 24.161 ns | 24.002 ns | 24.400 ns | 24.287 ns |  1.35 |    0.02 |   1,458 B |         - |          NA |

## Earlier record

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
