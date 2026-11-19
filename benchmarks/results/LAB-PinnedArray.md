# LAB: Pinned (POH) buffer for speed (rejected, R-13)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - rejected. A POH allocation is 20.6x of a normal one on net11 (20.4x on net10)

### PinnedArrayBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| PinWithFixed | 0.8651 ns | 0.8298 ns | 0.96 | - → - | 56 B → 56 B |
| PinnedPointerDirect | 0.8175 ns | 0.8256 ns | 1.01 | - → - | 33 B → 33 B |
| AllocateNormal | 53.2982 ns | 55.1506 ns | 1.03 | 4120 B → 4120 B | 46 B → 46 B |
| AllocatePinned | 1,088.7497 ns | 1,137.9938 ns | 1.05 | 4122 B → 4122 B | 217 B → 217 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.63 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean          | Error      | StdDev     | Median        | Min           | Max           | P90           | Ratio    | RatioSD | Gen0   | Code Size | Gen1   | Gen2   | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |--------------:|-----------:|-----------:|--------------:|--------------:|--------------:|--------------:|---------:|--------:|-------:|----------:|-------:|-------:|----------:|------------:|
| PinWithFixed        | .NET 10.0           | .NET 10.0 |     0.8651 ns |  0.0227 ns |  0.0311 ns |     0.8854 ns |     0.8262 ns |     0.9020 ns |     0.8958 ns |     1.00 |    0.00 |      - |      56 B |      - |      - |         - |          NA |
| PinnedPointerDirect | .NET 10.0           | .NET 10.0 |     0.8175 ns |  0.0019 ns |  0.0026 ns |     0.8172 ns |     0.8121 ns |     0.8215 ns |     0.8206 ns |     0.95 |    0.03 |      - |      33 B |      - |      - |         - |          NA |
| AllocateNormal      | .NET 10.0           | .NET 10.0 |    53.2982 ns |  0.6501 ns |  0.9113 ns |    52.9415 ns |    52.1031 ns |    54.9826 ns |    54.9281 ns |    61.69 |    2.42 | 0.4928 |      46 B |      - |      - |    4120 B |          NA |
| AllocatePinned      | .NET 10.0           | .NET 10.0 | 1,088.7497 ns | 20.2664 ns | 27.0551 ns | 1,087.8241 ns | 1,043.3691 ns | 1,151.8820 ns | 1,112.8398 ns | 1,260.14 |   54.26 | 1.3084 |     217 B | 1.3084 | 1.3084 |    4122 B |          NA |
|                     |                     |           |               |            |            |               |               |               |               |          |         |        |           |        |        |           |             |
| PinWithFixed        | MediumRun-.NET 11.0 | .NET 11.0 |     0.8298 ns |  0.0029 ns |  0.0042 ns |     0.8300 ns |     0.8233 ns |     0.8390 ns |     0.8365 ns |     1.00 |    0.00 |      - |      56 B |      - |      - |         - |          NA |
| PinnedPointerDirect | MediumRun-.NET 11.0 | .NET 11.0 |     0.8256 ns |  0.0071 ns |  0.0102 ns |     0.8272 ns |     0.8104 ns |     0.8483 ns |     0.8360 ns |     0.99 |    0.01 |      - |      33 B |      - |      - |         - |          NA |
| AllocateNormal      | MediumRun-.NET 11.0 | .NET 11.0 |    55.1506 ns |  1.0933 ns |  1.6365 ns |    55.0391 ns |    52.3292 ns |    58.0245 ns |    57.4694 ns |    66.47 |    1.97 | 0.4928 |      46 B |      - |      - |    4120 B |          NA |
| AllocatePinned      | MediumRun-.NET 11.0 | .NET 11.0 | 1,137.9938 ns | 38.5715 ns | 55.3180 ns | 1,124.1323 ns | 1,059.3674 ns | 1,246.9057 ns | 1,227.9153 ns | 1,371.47 |   65.86 | 1.3084 |     217 B | 1.3084 | 1.3084 |    4122 B |          NA |

## Earlier record

- Verdict: rejected (for performance purposes)
- POH allocation 961 ns vs normal 49.9 ns (19.3x) + Gen1/Gen2 collections - never allocate POH buffers per operation
- fixed 0.118 ns vs pre-pinned pointer 0.015 ns (code 56 vs 33 B): a real but ~0.1 ns-per-pin difference, visible only in pin-per-iteration hot loops
- POH is a fragmentation countermeasure for long-lived I/O buffers allocated once at startup, not a speed tool

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean        | Error      | StdDev     | Min         | Max           | P90         | Ratio    | RatioSD | Gen0   | Code Size | Gen1   | Gen2   | Allocated | Alloc Ratio |
|-------------------- |------------:|-----------:|-----------:|------------:|--------------:|------------:|---------:|--------:|-------:|----------:|-------:|-------:|----------:|------------:|
| PinWithFixed        |   0.1184 ns |  0.0060 ns |  0.0090 ns |   0.1024 ns |     0.1393 ns |   0.1307 ns |     1.01 |    0.11 |      - |      56 B |      - |      - |         - |          NA |
| PinnedPointerDirect |   0.0149 ns |  0.0047 ns |  0.0067 ns |   0.0013 ns |     0.0241 ns |   0.0227 ns |     0.13 |    0.06 |      - |      33 B |      - |      - |         - |          NA |
| AllocateNormal      |  49.9137 ns |  1.2972 ns |  1.8604 ns |  42.8221 ns |    52.3023 ns |  51.6976 ns |   423.97 |   34.84 | 0.4923 |      46 B |      - |      - |    4120 B |          NA |
| AllocatePinned      | 961.0727 ns | 23.9547 ns | 32.7895 ns | 938.1147 ns | 1,089.3459 ns | 996.6645 ns | 8,163.46 |  659.81 | 1.3084 |     217 B | 1.3084 | 1.3084 |    4120 B |          NA |
