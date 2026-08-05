# COL-06: Collection conversion shape specialization

## ImmutableArray construction

- Verdict: conditional
- Contiguous source (array/span): ToImmutableArray wins outright (7.2 ns / 88 B at 16; builder paths 2.5-3.4x slower) - bulk copy beats per-element Add
- Incremental build: MoveToImmutable vs ToImmutable = 17.7 ns/88 B vs 24.3 ns/176 B at 16 (halves allocation, saves the final copy); at 256 the time gap is within overlapping CIs (measurement-noise; codegen differs, 903 vs 2,035 B) while the allocation halving remains
- Rule: know your source shape; only reach for Builder when elements arrive one by one, and size the builder exactly to use MoveToImmutable

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Count | Mean       | Error     | StdDev     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------ |-----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **ToImmutableArrayExtension** | **16**    |   **3.956 ns** | **0.0365 ns** |  **0.0500 ns** |   **3.842 ns** |   **4.054 ns** |   **4.016 ns** |  **1.00** |    **0.02** | **0.0105** |     **706 B** |      **-** |      **88 B** |        **1.00** |
| BuilderToImmutable        | 16    |  14.338 ns | 0.2332 ns |  0.3490 ns |  13.893 ns |  15.301 ns |  14.836 ns |  3.63 |    0.10 | 0.0210 |   2,033 B |      - |     176 B |        2.00 |
| BuilderMoveToImmutable    | 16    |  11.310 ns | 0.1517 ns |  0.2076 ns |  11.074 ns |  11.777 ns |  11.594 ns |  2.86 |    0.06 | 0.0105 |     891 B |      - |      88 B |        1.00 |
|                           |       |            |           |            |            |            |            |       |         |        |           |        |           |             |
| **ToImmutableArrayExtension** | **256**   |  **23.111 ns** | **0.3207 ns** |  **0.4599 ns** |  **22.484 ns** |  **24.695 ns** |  **23.613 ns** |  **1.00** |    **0.03** | **0.1253** |     **710 B** |      **-** |    **1048 B** |        **1.00** |
| BuilderToImmutable        | 256   | 202.878 ns | 8.9281 ns | 12.8044 ns | 185.504 ns | 224.524 ns | 220.133 ns |  8.78 |    0.57 | 0.2506 |   2,035 B | 0.0005 |    2096 B |        2.00 |
| BuilderMoveToImmutable    | 256   | 170.600 ns | 1.3788 ns |  1.9329 ns | 166.529 ns | 174.476 ns | 172.755 ns |  7.38 |    0.16 | 0.1252 |     891 B |      - |    1048 B |        1.00 |

## List reuse and fill strategy

- Verdict: adopted
- new List(capacity): 0.46x vs no-capacity at 16 elements; reuse with Clear+EnsureCapacity: zero allocation
- Reuse + SetCount + AsSpan fill (COL-01): 0.21x (16) / 0.27x (256) with zero allocation - the fastest fill path

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Count | Mean       | Error      | StdDev     | Median     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **NewListNoCapacity**     | **16**    |  **30.449 ns** |  **1.0377 ns** |  **1.4204 ns** |  **30.429 ns** |  **28.326 ns** |  **33.909 ns** |  **32.355 ns** |  **1.00** |    **0.06** | **0.0258** |   **1,901 B** |      **-** |     **216 B** |        **1.00** |
| NewListWithCapacity   | 16    |  15.578 ns |  0.1371 ns |  0.1830 ns |  15.604 ns |  15.019 ns |  15.946 ns |  15.696 ns |  0.51 |    0.02 | 0.0143 |     303 B |      - |     120 B |        0.56 |
| ReuseWithClear        | 16    |  19.111 ns |  0.2780 ns |  0.4161 ns |  19.123 ns |  17.610 ns |  19.929 ns |  19.583 ns |  0.63 |    0.03 |      - |     463 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | 16    |   5.681 ns |  0.0767 ns |  0.1024 ns |   5.654 ns |   5.602 ns |   6.076 ns |   5.721 ns |  0.19 |    0.01 |      - |     360 B |      - |         - |        0.00 |
|                       |       |            |            |            |            |            |            |            |       |         |        |           |        |           |             |
| **NewListNoCapacity**     | **256**   | **296.438 ns** | **23.9837 ns** | **34.3968 ns** | **269.638 ns** | **255.026 ns** | **337.229 ns** | **333.489 ns** |  **1.01** |    **0.16** | **0.2666** |   **1,891 B** | **0.0010** |    **2232 B** |        **1.00** |
| NewListWithCapacity   | 256   | 237.432 ns |  1.7959 ns |  2.3975 ns | 237.868 ns | 228.213 ns | 241.572 ns | 239.297 ns |  0.81 |    0.09 | 0.1290 |     303 B |      - |    1080 B |        0.48 |
| ReuseWithClear        | 256   | 204.001 ns |  2.1630 ns |  3.1021 ns | 203.247 ns | 200.688 ns | 212.034 ns | 208.527 ns |  0.70 |    0.08 |      - |     463 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | 256   |  76.383 ns |  0.8630 ns |  1.1812 ns |  76.190 ns |  74.876 ns |  79.829 ns |  77.677 ns |  0.26 |    0.03 |      - |     360 B |      - |         - |        0.00 |
