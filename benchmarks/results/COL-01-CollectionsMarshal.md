# COL-01: CollectionsMarshal

- Verdict: adopted (verified) - still worth it, but the margins are smaller than on the previous baseline
- AsSpan iteration 0.85x vs List foreach (was 0.52x on Ryzen 9 5900X): List's own foreach gained 1.97x from the newer core while the span walk gained only 1.20x, so most of the gap closed. Note `for` over List is now the slowest form at 1.07x
- GetValueRefOrAddDefault read-modify-write 0.64x vs double lookup (unchanged - hashing dominates and both paths pay it once vs twice)
- SetCount + span bulk build 0.21x (16) / 0.41x (1024) vs Add loop, allocation still halved. At 1024 the win shrank from 0.22x because the Add loop itself sped up 2.4x
- Takeaway unchanged in direction, but AsSpan iteration is no longer a 2x lever - reach for it in hot loops, not as a blanket rewrite

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

