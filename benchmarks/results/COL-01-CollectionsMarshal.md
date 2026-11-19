# COL-01: CollectionsMarshal

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted
- List iteration: AsSpan is 0.88x of foreach on both generations (210.6 vs 239.3 ns on net11); a `for` over the List stays the slowest (1.09x)
- GetValueRefOrAddDefault: 0.59x of the double lookup on net11 (0.63x on net10)
- SetCount + span write: 0.23x / 0.41x of the Add loop at 16 / 1024 on net11

### ListSetCountBenchmark

net10 → net11 per row:

| Method | Size | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| AddLoop | 16 | 31.233 ns | 27.129 ns | 0.87 | 216 B → 216 B | 1,694 B → 2,052 B |
| AddLoopCapacity | 16 | 13.734 ns | 15.025 ns | 1.09 | 120 B → 120 B | 426 B → 216 B |
| SetCountSpanWrite | 16 | 6.196 ns | 6.264 ns | 1.01 | 88 B → 88 B | 1,943 B → 2,055 B |
| SetCountCapacitySpanWrite | 16 | 7.533 ns | 7.782 ns | 1.03 | 120 B → 120 B | 487 B → 484 B |
| AddLoop | 1024 | 681.805 ns | 660.323 ns | 0.97 | 8424 B → 8424 B | 1,694 B → 2,052 B |
| AddLoopCapacity | 1024 | 512.355 ns | 584.364 ns | 1.14 | 4152 B → 4152 B | 424 B → 214 B |
| SetCountSpanWrite | 1024 | 271.823 ns | 273.045 ns | 1.00 | 4120 B → 4120 B | 1,943 B → 2,055 B |
| SetCountCapacitySpanWrite | 1024 | 278.342 ns | 276.036 ns | 0.99 | 4152 B → 4152 B | 487 B → 274 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.58 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                    | Job                 | Runtime   | Size | Mean       | Error      | StdDev     | Median     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |-------------------- |---------- |----- |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **AddLoop**                   | **.NET 10.0**           | **.NET 10.0** | **16**   |  **31.233 ns** |  **1.2819 ns** |  **1.8790 ns** |  **29.951 ns** |  **29.187 ns** |  **33.964 ns** |  **33.451 ns** |  **1.00** |    **0.00** | **0.0258** |   **1,694 B** |      **-** |     **216 B** |        **1.00** |
| AddLoopCapacity           | .NET 10.0           | .NET 10.0 | 16   |  13.734 ns |  0.9474 ns |  1.3281 ns |  12.706 ns |  12.297 ns |  15.136 ns |  15.111 ns |  0.44 |    0.05 | 0.0143 |     426 B |      - |     120 B |        0.56 |
| SetCountSpanWrite         | .NET 10.0           | .NET 10.0 | 16   |   6.196 ns |  0.0279 ns |  0.0401 ns |   6.188 ns |   6.126 ns |   6.305 ns |   6.244 ns |  0.20 |    0.01 | 0.0105 |   1,943 B |      - |      88 B |        0.41 |
| SetCountCapacitySpanWrite | .NET 10.0           | .NET 10.0 | 16   |   7.533 ns |  0.0596 ns |  0.0892 ns |   7.507 ns |   7.413 ns |   7.727 ns |   7.665 ns |  0.24 |    0.01 | 0.0143 |     487 B |      - |     120 B |        0.56 |
|                           |                     |           |      |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| AddLoop                   | MediumRun-.NET 11.0 | .NET 11.0 | 16   |  27.129 ns |  0.4015 ns |  0.5758 ns |  27.338 ns |  26.344 ns |  27.915 ns |  27.776 ns |  1.00 |    0.00 | 0.0258 |   2,052 B |      - |     216 B |        1.00 |
| AddLoopCapacity           | MediumRun-.NET 11.0 | .NET 11.0 | 16   |  15.025 ns |  0.0903 ns |  0.1351 ns |  15.053 ns |  14.746 ns |  15.281 ns |  15.159 ns |  0.55 |    0.01 | 0.0143 |     216 B |      - |     120 B |        0.56 |
| SetCountSpanWrite         | MediumRun-.NET 11.0 | .NET 11.0 | 16   |   6.264 ns |  0.0529 ns |  0.0776 ns |   6.241 ns |   6.178 ns |   6.467 ns |   6.377 ns |  0.23 |    0.01 | 0.0105 |   2,055 B |      - |      88 B |        0.41 |
| SetCountCapacitySpanWrite | MediumRun-.NET 11.0 | .NET 11.0 | 16   |   7.782 ns |  0.1445 ns |  0.2073 ns |   7.754 ns |   7.437 ns |   8.290 ns |   8.040 ns |  0.29 |    0.01 | 0.0143 |     484 B |      - |     120 B |        0.56 |
|                           |                     |           |      |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| **AddLoop**                   | **.NET 10.0**           | **.NET 10.0** | **1024** | **681.805 ns** |  **6.7905 ns** |  **9.7388 ns** | **680.151 ns** | **670.884 ns** | **709.549 ns** | **694.637 ns** |  **1.00** |    **0.00** | **1.0071** |   **1,694 B** | **0.0200** |    **8424 B** |        **1.00** |
| AddLoopCapacity           | .NET 10.0           | .NET 10.0 | 1024 | 512.355 ns |  4.4909 ns |  6.1472 ns | 511.286 ns | 503.840 ns | 532.015 ns | 519.941 ns |  0.75 |    0.01 | 0.4959 |     424 B | 0.0067 |    4152 B |        0.49 |
| SetCountSpanWrite         | .NET 10.0           | .NET 10.0 | 1024 | 271.823 ns |  2.2209 ns |  3.2554 ns | 270.968 ns | 266.550 ns | 279.048 ns | 276.147 ns |  0.40 |    0.01 | 0.4926 |   1,943 B |      - |    4120 B |        0.49 |
| SetCountCapacitySpanWrite | .NET 10.0           | .NET 10.0 | 1024 | 278.342 ns |  1.5325 ns |  2.2938 ns | 278.284 ns | 274.143 ns | 283.804 ns | 280.447 ns |  0.41 |    0.01 | 0.4964 |     487 B | 0.0076 |    4152 B |        0.49 |
|                           |                     |           |      |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| AddLoop                   | MediumRun-.NET 11.0 | .NET 11.0 | 1024 | 660.323 ns |  3.0521 ns |  4.3772 ns | 658.853 ns | 651.156 ns | 668.339 ns | 665.779 ns |  1.00 |    0.00 | 1.0080 |   2,052 B |      - |    8424 B |        1.00 |
| AddLoopCapacity           | MediumRun-.NET 11.0 | .NET 11.0 | 1024 | 584.364 ns | 48.3187 ns | 69.2973 ns | 576.019 ns | 512.211 ns | 688.561 ns | 667.440 ns |  0.89 |    0.10 | 0.4959 |     214 B |      - |    4152 B |        0.49 |
| SetCountSpanWrite         | MediumRun-.NET 11.0 | .NET 11.0 | 1024 | 273.045 ns |  1.3432 ns |  1.9689 ns | 272.945 ns | 269.472 ns | 276.293 ns | 275.485 ns |  0.41 |    0.00 | 0.4926 |   2,055 B |      - |    4120 B |        0.49 |
| SetCountCapacitySpanWrite | MediumRun-.NET 11.0 | .NET 11.0 | 1024 | 276.036 ns |  0.9454 ns |  1.3558 ns | 275.983 ns | 273.570 ns | 279.404 ns | 277.699 ns |  0.42 |    0.00 | 0.4964 |     274 B |      - |    4152 B |        0.49 |

### ListIterationBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ForEachList | 240.8 ns | 239.3 ns | 0.99 | - → - | 68 B → 68 B |
| ForList | 262.1 ns | 260.4 ns | 0.99 | - → - | 80 B → 80 B |
| AsSpanFor | 211.0 ns | 210.6 ns | 1.00 | - → - | 68 B → 68 B |
| AsSpanForEach | 211.0 ns | 211.1 ns | 1.00 | - → - | 68 B → 68 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.56 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ForEachList   | .NET 10.0           | .NET 10.0 | 240.8 ns | 1.44 ns | 2.11 ns | 236.9 ns | 245.0 ns | 243.2 ns |  1.00 |      68 B |         - |          NA |
| ForList       | .NET 10.0           | .NET 10.0 | 262.1 ns | 1.53 ns | 2.15 ns | 258.5 ns | 267.9 ns | 264.7 ns |  1.09 |      80 B |         - |          NA |
| AsSpanFor     | .NET 10.0           | .NET 10.0 | 211.0 ns | 0.61 ns | 0.87 ns | 209.8 ns | 213.1 ns | 212.1 ns |  0.88 |      68 B |         - |          NA |
| AsSpanForEach | .NET 10.0           | .NET 10.0 | 211.0 ns | 0.70 ns | 1.03 ns | 209.7 ns | 213.4 ns | 212.5 ns |  0.88 |      68 B |         - |          NA |
|               |                     |           |          |         |         |          |          |          |       |           |           |             |
| ForEachList   | MediumRun-.NET 11.0 | .NET 11.0 | 239.3 ns | 1.39 ns | 2.08 ns | 235.1 ns | 242.8 ns | 241.5 ns |  1.00 |      68 B |         - |          NA |
| ForList       | MediumRun-.NET 11.0 | .NET 11.0 | 260.4 ns | 0.78 ns | 1.09 ns | 258.0 ns | 262.0 ns | 261.7 ns |  1.09 |      80 B |         - |          NA |
| AsSpanFor     | MediumRun-.NET 11.0 | .NET 11.0 | 210.6 ns | 0.49 ns | 0.71 ns | 209.5 ns | 211.8 ns | 211.6 ns |  0.88 |      68 B |         - |          NA |
| AsSpanForEach | MediumRun-.NET 11.0 | .NET 11.0 | 211.1 ns | 0.60 ns | 0.88 ns | 209.8 ns | 213.0 ns | 212.4 ns |  0.88 |      68 B |         - |          NA |

### DictionaryCountBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DoubleLookup | 10.917 μs | 11.404 μs | 1.04 | 21.71 KB → 21.71 KB | 6,002 B → 6,166 B |
| RefLookup | 6.845 μs | 6.695 μs | 0.98 | 21.71 KB → 21.71 KB | 7,012 B → 6,824 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.48 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method       | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|-------:|-------:|----------:|------------:|
| DoubleLookup | .NET 10.0           | .NET 10.0 | 10.917 μs | 0.0519 μs | 0.0727 μs | 10.753 μs | 11.057 μs | 11.018 μs |  1.00 |   6,002 B | 2.6550 | 0.1526 |  21.71 KB |        1.00 |
| RefLookup    | .NET 10.0           | .NET 10.0 |  6.845 μs | 0.0410 μs | 0.0589 μs |  6.728 μs |  6.935 μs |  6.916 μs |  0.63 |   7,012 B | 2.6550 | 0.0992 |  21.71 KB |        1.00 |
|              |                     |           |           |           |           |           |           |           |       |           |        |        |           |             |
| DoubleLookup | MediumRun-.NET 11.0 | .NET 11.0 | 11.404 μs | 0.0764 μs | 0.1096 μs | 11.224 μs | 11.646 μs | 11.571 μs |  1.00 |   6,166 B | 2.6550 | 0.0916 |  21.71 KB |        1.00 |
| RefLookup    | MediumRun-.NET 11.0 | .NET 11.0 |  6.695 μs | 0.0457 μs | 0.0656 μs |  6.558 μs |  6.861 μs |  6.768 μs |  0.59 |   6,824 B | 2.6550 |      - |  21.71 KB |        1.00 |

## Earlier record

- Verdict: adopted (verified)
- AsSpan iteration 0.85x vs List foreach (a for loop over List is the slowest form at 1.07x) - a hot-loop optimization, not a blanket rewrite
- GetValueRefOrAddDefault read-modify-write 0.64x vs double lookup (one hash computation instead of two)
- SetCount + span bulk build 0.21x (16) / 0.41x (1024) vs Add loop, allocation halved

## ListIterationBenchmark

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ForEachList   | 246.8 ns | 7.36 ns | 11.01 ns | 237.2 ns | 275.4 ns | 266.6 ns |  1.00 |    0.06 |      68 B |         - |          NA |
| ForList       | 263.8 ns | 1.54 ns |  2.31 ns | 259.8 ns | 267.9 ns | 267.5 ns |  1.07 |    0.05 |      80 B |         - |          NA |
| AsSpanFor     | 210.0 ns | 0.52 ns |  0.75 ns | 209.1 ns | 212.0 ns | 210.8 ns |  0.85 |    0.04 |      68 B |         - |          NA |
| AsSpanForEach | 211.4 ns | 3.14 ns |  4.40 ns | 208.8 ns | 228.7 ns | 212.3 ns |  0.86 |    0.04 |      68 B |         - |          NA |

## DictionaryCountBenchmark

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|-------:|-------:|----------:|------------:|
| DoubleLookup | 10.738 μs | 0.0494 μs | 0.0739 μs | 10.616 μs | 10.910 μs | 10.821 μs |  1.00 |   5,190 B | 2.6550 | 0.1526 |  21.71 KB |        1.00 |
| RefLookup    |  6.908 μs | 0.1018 μs | 0.1393 μs |  6.656 μs |  7.373 μs |  6.984 μs |  0.64 |   7,314 B | 2.6550 | 0.0992 |  21.71 KB |        1.00 |

## ListSetCountBenchmark

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Size | Mean       | Error      | StdDev     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |----- |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **AddLoop**                   | **16**   |  **27.541 ns** |  **0.7571 ns** |  **0.9845 ns** |  **25.670 ns** |  **29.945 ns** |  **29.068 ns** |  **1.00** |    **0.05** |   **1,878 B** | **0.0258** |     **216 B** |        **1.00** |
| AddLoopCapacity           | 16   |  11.966 ns |  0.1215 ns |  0.1781 ns |  11.513 ns |  12.306 ns |  12.201 ns |  0.43 |    0.02 |     276 B | 0.0143 |     120 B |        0.56 |
| SetCountSpanWrite         | 16   |   5.732 ns |  0.4149 ns |  0.5951 ns |   5.441 ns |   7.777 ns |   6.507 ns |  0.21 |    0.02 |     663 B | 0.0105 |      88 B |        0.41 |
| SetCountCapacitySpanWrite | 16   |   6.767 ns |  0.0860 ns |  0.1287 ns |   6.387 ns |   6.958 ns |   6.884 ns |  0.25 |    0.01 |     347 B | 0.0143 |     120 B |        0.56 |
|                           |      |            |            |            |            |            |            |       |         |           |        |           |             |
| **AddLoop**                   | **1024** | **680.708 ns** | **17.3954 ns** | **24.9480 ns** | **640.705 ns** | **767.927 ns** | **707.329 ns** |  **1.00** |    **0.05** |   **1,862 B** | **1.0061** |    **8424 B** |        **1.00** |
| AddLoopCapacity           | 1024 | 521.159 ns |  4.8912 ns |  7.1694 ns | 493.249 ns | 532.231 ns | 528.082 ns |  0.77 |    0.03 |     276 B | 0.4959 |    4152 B |        0.49 |
| SetCountSpanWrite         | 1024 | 280.888 ns |  5.9460 ns |  8.1389 ns | 265.195 ns | 314.872 ns | 282.527 ns |  0.41 |    0.02 |     663 B | 0.4921 |    4120 B |        0.49 |
| SetCountCapacitySpanWrite | 1024 | 281.424 ns |  3.0982 ns |  4.4433 ns | 264.980 ns | 287.118 ns | 284.823 ns |  0.41 |    0.02 |     347 B | 0.4959 |    4152 B |        0.49 |

