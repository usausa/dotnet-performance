# COL-06: Collection conversion shape specialization

## ImmutableArray construction

- Verdict: conditional
- Contiguous source (array/span): ToImmutableArray wins outright (7.2 ns / 88 B at 16; builder paths 2.5-3.4x slower) - bulk copy beats per-element Add
- Incremental build: MoveToImmutable vs ToImmutable = 17.7 ns/88 B vs 24.3 ns/176 B at 16 (halves allocation, saves the final copy)
- Rule: know your source shape; only reach for Builder when elements arrive one by one, and size the builder exactly to use MoveToImmutable

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Count | Mean       | Error      | StdDev     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------------- |------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|----------:|------------:|
| **ToImmutableArrayExtension** | **16**    |   **7.155 ns** |  **0.4101 ns** |  **0.6010 ns** |   **6.447 ns** |   **8.515 ns** |   **8.009 ns** |  **1.01** |    **0.11** | **0.0053** |     **718 B** |      **88 B** |        **1.00** |
| BuilderToImmutable        | 16    |  24.286 ns |  0.3214 ns |  0.4711 ns |  23.588 ns |  25.571 ns |  24.798 ns |  3.42 |    0.27 | 0.0105 |   2,037 B |     176 B |        2.00 |
| BuilderMoveToImmutable    | 16    |  17.700 ns |  0.2848 ns |  0.3992 ns |  17.104 ns |  18.751 ns |  18.174 ns |  2.49 |    0.20 | 0.0052 |     903 B |      88 B |        1.00 |
|                           |       |            |            |            |            |            |            |       |         |        |           |           |             |
| **ToImmutableArrayExtension** | **256**   |  **36.918 ns** |  **1.0331 ns** |  **1.5142 ns** |  **34.647 ns** |  **41.220 ns** |  **39.293 ns** |  **1.00** |    **0.06** | **0.0626** |     **712 B** |    **1048 B** |        **1.00** |
| BuilderToImmutable        | 256   | 288.052 ns | 17.5496 ns | 25.1692 ns | 260.242 ns | 363.491 ns | 309.606 ns |  7.81 |    0.74 | 0.1249 |   2,035 B |    2096 B |        2.00 |
| BuilderMoveToImmutable    | 256   | 278.516 ns | 32.2828 ns | 48.3193 ns | 224.647 ns | 364.398 ns | 347.476 ns |  7.56 |    1.32 | 0.0625 |     903 B |    1048 B |        1.00 |

## List reuse and fill strategy

- Verdict: adopted
- new List(capacity): 0.46x vs no-capacity at 16 elements; reuse with Clear+EnsureCapacity: zero allocation
- Reuse + SetCount + AsSpan fill (COL-01): 0.21x (16) / 0.27x (256) with zero allocation - the fastest fill path

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Count | Mean      | Error     | StdDev     | Min        | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------------- |------ |----------:|----------:|-----------:|-----------:|----------:|----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| **NewListNoCapacity**     | **16**    |  **55.21 ns** |  **0.790 ns** |   **1.107 ns** |  **52.858 ns** |  **57.87 ns** |  **56.36 ns** |  **1.00** |    **0.03** | **0.0129** |   **1,923 B** |      **-** |     **216 B** |        **1.00** |
| NewListWithCapacity   | 16    |  25.22 ns |  1.165 ns |   1.634 ns |  23.254 ns |  28.89 ns |  27.22 ns |  0.46 |    0.03 | 0.0072 |     200 B |      - |     120 B |        0.56 |
| ReuseWithClear        | 16    |  28.51 ns |  0.527 ns |   0.789 ns |  27.126 ns |  30.36 ns |  29.39 ns |  0.52 |    0.02 |      - |     354 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | 16    |  11.85 ns |  2.313 ns |   3.242 ns |   8.037 ns |  16.17 ns |  15.67 ns |  0.21 |    0.06 |      - |     360 B |      - |         - |        0.00 |
|                       |       |           |           |            |            |           |           |       |         |        |           |        |           |             |
| **NewListNoCapacity**     | **256**   | **506.04 ns** | **71.614 ns** | **104.971 ns** | **393.182 ns** | **675.51 ns** | **623.56 ns** |  **1.04** |    **0.30** | **0.1330** |   **1,903 B** | **0.0005** |    **2232 B** |        **1.00** |
| NewListWithCapacity   | 256   | 395.20 ns | 17.433 ns |  26.093 ns | 347.003 ns | 442.42 ns | 426.04 ns |  0.81 |    0.17 | 0.0644 |     194 B |      - |    1080 B |        0.48 |
| ReuseWithClear        | 256   | 412.79 ns | 15.470 ns |  22.676 ns | 346.031 ns | 434.58 ns | 431.76 ns |  0.85 |    0.18 |      - |     354 B |      - |         - |        0.00 |
| ReuseWithSetCountSpan | 256   | 129.41 ns |  3.007 ns |   4.313 ns | 121.009 ns | 138.67 ns | 134.06 ns |  0.27 |    0.05 |      - |     360 B |      - |         - |        0.00 |
