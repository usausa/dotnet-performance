# BUF-07: ObjectPool (ThreadStatic single-slot)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. The ThreadStatic pool is 0.62x of a new StringBuilder per call on net10 and 0.64x on net11 (13.07 vs 20.27 ns); allocation identical

### ObjectPoolBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| NewEveryTime | 20.46 ns | 20.27 ns | 0.99 | 648 B → 648 B | 2,920 B → 3,173 B |
| ThreadStaticPool | 12.72 ns | 13.07 ns | 1.03 | 64 B → 64 B | 5,251 B → 5,319 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.15 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|----------:|-------:|-------:|----------:|------------:|
| NewEveryTime     | .NET 10.0           | .NET 10.0 | 20.46 ns | 0.114 ns | 0.171 ns | 20.07 ns | 20.78 ns | 20.72 ns |  1.00 |   2,920 B | 0.0775 | 0.0001 |     648 B |        1.00 |
| ThreadStaticPool | .NET 10.0           | .NET 10.0 | 12.72 ns | 0.049 ns | 0.069 ns | 12.61 ns | 12.83 ns | 12.80 ns |  0.62 |   5,251 B | 0.0076 |      - |      64 B |        0.10 |
|                  |                     |           |          |          |          |          |          |          |       |           |        |        |           |             |
| NewEveryTime     | MediumRun-.NET 11.0 | .NET 11.0 | 20.27 ns | 0.095 ns | 0.136 ns | 20.07 ns | 20.56 ns | 20.51 ns |  1.00 |   3,173 B | 0.0775 | 0.0001 |     648 B |        1.00 |
| ThreadStaticPool | MediumRun-.NET 11.0 | .NET 11.0 | 13.07 ns | 0.188 ns | 0.276 ns | 12.72 ns | 13.51 ns | 13.39 ns |  0.64 |   5,319 B | 0.0076 |      - |      64 B |        0.10 |

## Earlier record

- Verdict: adopted
- 0.68x vs new StringBuilder per call (19.97 -> 13.51 ns); allocation 648 B -> 64 B (result string only, 0.10x)
- Retained-capacity cap prevents pool bloat; single-slot ThreadStatic keeps thread safety trivially

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| NewEveryTime     | 19.97 ns | 0.516 ns | 0.706 ns | 18.83 ns | 21.43 ns | 21.07 ns |  1.00 |    0.05 | 0.0775 |   2,326 B | 0.0001 |     648 B |        1.00 |
| ThreadStaticPool | 13.51 ns | 0.142 ns | 0.194 ns | 13.30 ns | 13.94 ns | 13.78 ns |  0.68 |    0.02 | 0.0076 |   3,817 B |      - |      64 B |        0.10 |
