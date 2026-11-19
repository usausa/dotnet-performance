# STK-08: InlineArray vs stackalloc vs new array

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - InlineArray 0.67x and stackalloc 0.65x of `new int[8]` on net11 (0.70x / 0.68x on net10), both 0 B

### InlineArrayBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| NewArray | 5.423 ns | 5.644 ns | 1.04 | 56 B → 56 B | 113 B → 113 B |
| Stackalloc | 3.662 ns | 3.652 ns | 1.00 | - → - | 134 B → 134 B |
| InlineArrayBuffer | 3.805 ns | 3.780 ns | 0.99 | - → - | 112 B → 112 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.6 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|----------:|-------:|----------:|------------:|
| NewArray          | .NET 10.0           | .NET 10.0 | 5.423 ns | 0.0578 ns | 0.0830 ns | 5.303 ns | 5.678 ns | 5.540 ns |  1.00 |     113 B | 0.0067 |      56 B |        1.00 |
| Stackalloc        | .NET 10.0           | .NET 10.0 | 3.662 ns | 0.0280 ns | 0.0410 ns | 3.605 ns | 3.755 ns | 3.721 ns |  0.68 |     134 B |      - |         - |        0.00 |
| InlineArrayBuffer | .NET 10.0           | .NET 10.0 | 3.805 ns | 0.0241 ns | 0.0353 ns | 3.743 ns | 3.881 ns | 3.846 ns |  0.70 |     112 B |      - |         - |        0.00 |
|                   |                     |           |          |           |           |          |          |          |       |           |        |           |             |
| NewArray          | MediumRun-.NET 11.0 | .NET 11.0 | 5.644 ns | 0.0403 ns | 0.0591 ns | 5.544 ns | 5.746 ns | 5.721 ns |  1.00 |     113 B | 0.0067 |      56 B |        1.00 |
| Stackalloc        | MediumRun-.NET 11.0 | .NET 11.0 | 3.652 ns | 0.0235 ns | 0.0344 ns | 3.566 ns | 3.708 ns | 3.691 ns |  0.65 |     134 B |      - |         - |        0.00 |
| InlineArrayBuffer | MediumRun-.NET 11.0 | .NET 11.0 | 3.780 ns | 0.0142 ns | 0.0204 ns | 3.742 ns | 3.836 ns | 3.808 ns |  0.67 |     112 B |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted
- InlineArray 0.61x / stackalloc 0.60x vs new int[8], both zero-alloc (heap array: 56 B)
- InlineArray and stackalloc are equal in time (2.92 vs 2.87 ns, CIs overlap); InlineArray's code is slightly smaller (112 vs 134 B)
- InlineArray's unique value remains that the buffer can live inside a struct field

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| NewArray          | 4.808 ns | 0.0581 ns | 0.0775 ns | 4.679 ns | 4.974 ns | 4.893 ns |  1.00 |    0.02 | 0.0067 |     113 B |      56 B |        1.00 |
| Stackalloc        | 2.872 ns | 0.0452 ns | 0.0649 ns | 2.778 ns | 3.045 ns | 2.953 ns |  0.60 |    0.02 |      - |     134 B |         - |        0.00 |
| InlineArrayBuffer | 2.916 ns | 0.0844 ns | 0.1126 ns | 2.816 ns | 3.306 ns | 2.967 ns |  0.61 |    0.02 |      - |     112 B |         - |        0.00 |
