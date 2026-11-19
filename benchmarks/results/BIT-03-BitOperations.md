# BIT-03: BitOperations scan / popcount

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - identical code sizes on both generations; the TZCNT scan is 0.14x and PopCount 0.01-0.02x of the manual loops

### BitOperationsBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| SetBitScanLoop | 1,066.37 ns | 1,062.33 ns | 1.00 | - → - | 77 B → 77 B |
| SetBitScanTzcnt | 144.32 ns | 144.53 ns | 1.00 | - → - | 77 B → 77 B |
| PopCountManual | 858.21 ns | 885.97 ns | 1.03 | - → - | 73 B → 73 B |
| PopCountIntrinsic | 13.05 ns | 13.04 ns | 1.00 | - → - | 52 B → 52 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.54 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| SetBitScanLoop    | .NET 10.0           | .NET 10.0 | 1,066.37 ns | 11.527 ns | 17.254 ns | 1,030.65 ns | 1,090.81 ns | 1,086.13 ns |  1.00 |    0.00 |      77 B |         - |          NA |
| SetBitScanTzcnt   | .NET 10.0           | .NET 10.0 |   144.32 ns |  0.693 ns |  1.016 ns |   142.52 ns |   146.87 ns |   145.43 ns |  0.14 |    0.00 |      77 B |         - |          NA |
| PopCountManual    | .NET 10.0           | .NET 10.0 |   858.21 ns |  2.946 ns |  4.409 ns |   851.65 ns |   867.67 ns |   864.63 ns |  0.81 |    0.01 |      73 B |         - |          NA |
| PopCountIntrinsic | .NET 10.0           | .NET 10.0 |    13.05 ns |  0.083 ns |  0.119 ns |    12.93 ns |    13.38 ns |    13.22 ns |  0.01 |    0.00 |      52 B |         - |          NA |
|                   |                     |           |             |           |           |             |             |             |       |         |           |           |             |
| SetBitScanLoop    | MediumRun-.NET 11.0 | .NET 11.0 | 1,062.33 ns | 21.677 ns | 31.088 ns | 1,033.69 ns | 1,145.43 ns | 1,106.92 ns |  1.00 |    0.00 |      77 B |         - |          NA |
| SetBitScanTzcnt   | MediumRun-.NET 11.0 | .NET 11.0 |   144.53 ns |  0.937 ns |  1.402 ns |   142.18 ns |   147.66 ns |   146.47 ns |  0.14 |    0.00 |      77 B |         - |          NA |
| PopCountManual    | MediumRun-.NET 11.0 | .NET 11.0 |   885.97 ns |  3.681 ns |  5.395 ns |   875.74 ns |   900.26 ns |   892.51 ns |  0.83 |    0.02 |      73 B |         - |          NA |
| PopCountIntrinsic | MediumRun-.NET 11.0 | .NET 11.0 |    13.04 ns |  0.068 ns |  0.100 ns |    12.90 ns |    13.29 ns |    13.20 ns |  0.01 |    0.00 |      52 B |         - |          NA |

## Earlier record

- Verdict: adopted
- Set-bit scan via TrailingZeroCount + (mask &= mask - 1): 0.13x (1,056 -> 141 ns)
- PopCount intrinsic vs manual loop: 0.01x (854 -> 12.8 ns)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| SetBitScanLoop    | 1,055.60 ns | 13.918 ns | 19.052 ns | 1,027.61 ns | 1,099.84 ns | 1,079.97 ns |  1.00 |    0.02 |      77 B |         - |          NA |
| SetBitScanTzcnt   |   141.46 ns |  0.957 ns |  1.309 ns |   140.55 ns |   147.38 ns |   142.14 ns |  0.13 |    0.00 |      77 B |         - |          NA |
| PopCountManual    |   853.59 ns |  2.274 ns |  3.262 ns |   847.59 ns |   860.19 ns |   857.82 ns |  0.81 |    0.01 |      73 B |         - |          NA |
| PopCountIntrinsic |    12.83 ns |  0.031 ns |  0.044 ns |    12.76 ns |    12.94 ns |    12.88 ns |  0.01 |    0.00 |      52 B |         - |          NA |
