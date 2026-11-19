# STK-07: Lazy allocation and shared empty singletons

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged. The lazy list is 1.34x of the eager list at a 10% failure rate on net11 (1.24x on net10) and 0.63x when everything is valid
- `new int[0]` against `Array.Empty`: both 12 B of code and 0 B on both generations; the 0.15-0.18 ns gap swaps sign between the generations (noise)

### LazyAllocationBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| EagerList | 39.88 ns | 35.58 ns | 0.89 | 216 B → 216 B | 1,712 B → 2,070 B |
| LazyList | 49.32 ns | 47.58 ns | 0.96 | 216 B → 216 B | 1,741 B → 2,103 B |
| LazyListAllValid | 25.31 ns | 22.50 ns | 0.89 | - → - | 150 B → 165 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.38 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| EagerList        | .NET 10.0           | .NET 10.0 | 39.88 ns | 1.097 ns | 1.643 ns | 39.75 ns | 37.78 ns | 43.05 ns | 41.86 ns |  1.00 |    0.00 |   1,712 B | 0.0258 |     216 B |        1.00 |
| LazyList         | .NET 10.0           | .NET 10.0 | 49.32 ns | 0.701 ns | 1.027 ns | 49.34 ns | 47.00 ns | 51.29 ns | 50.50 ns |  1.24 |    0.06 |   1,741 B | 0.0258 |     216 B |        1.00 |
| LazyListAllValid | .NET 10.0           | .NET 10.0 | 25.31 ns | 0.496 ns | 0.711 ns | 25.72 ns | 24.32 ns | 26.30 ns | 26.08 ns |  0.64 |    0.03 |     150 B |      - |         - |        0.00 |
|                  |                     |           |          |          |          |          |          |          |          |       |         |           |        |           |             |
| EagerList        | MediumRun-.NET 11.0 | .NET 11.0 | 35.58 ns | 1.053 ns | 1.576 ns | 35.83 ns | 33.39 ns | 37.87 ns | 37.72 ns |  1.00 |    0.00 |   2,070 B | 0.0258 |     216 B |        1.00 |
| LazyList         | MediumRun-.NET 11.0 | .NET 11.0 | 47.58 ns | 0.958 ns | 1.434 ns | 47.37 ns | 45.47 ns | 49.92 ns | 49.45 ns |  1.34 |    0.07 |   2,103 B | 0.0258 |     216 B |        1.00 |
| LazyListAllValid | MediumRun-.NET 11.0 | .NET 11.0 | 22.50 ns | 0.373 ns | 0.547 ns | 22.35 ns | 22.06 ns | 24.45 ns | 22.97 ns |  0.63 |    0.03 |     165 B |      - |         - |        0.00 |

### SharedEmptyBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| NewEmptyArray | 0.1775 ns | 0.1653 ns | 0.93 | - → - | 12 B → 12 B |
| SharedEmptyArray | 0.1520 ns | 0.1815 ns | 1.19 | - → - | 12 B → 12 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.42 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| NewEmptyArray    | .NET 10.0           | .NET 10.0 | 0.1775 ns | 0.0069 ns | 0.0103 ns | 0.1625 ns | 0.1926 ns | 0.1893 ns |  1.00 |    0.00 |      12 B |         - |          NA |
| SharedEmptyArray | .NET 10.0           | .NET 10.0 | 0.1520 ns | 0.0031 ns | 0.0046 ns | 0.1431 ns | 0.1603 ns | 0.1569 ns |  0.86 |    0.06 |      12 B |         - |          NA |
|                  |                     |           |           |           |           |           |           |           |       |         |           |           |             |
| NewEmptyArray    | MediumRun-.NET 11.0 | .NET 11.0 | 0.1653 ns | 0.0132 ns | 0.0198 ns | 0.1402 ns | 0.1900 ns | 0.1877 ns |  1.00 |    0.00 |      12 B |         - |          NA |
| SharedEmptyArray | MediumRun-.NET 11.0 | .NET 11.0 | 0.1815 ns | 0.0045 ns | 0.0068 ns | 0.1691 ns | 0.1939 ns | 0.1909 ns |  1.11 |    0.14 |      12 B |         - |          NA |

## Earlier record

## Lazy error list (10% failure rate vs all-valid)

- Verdict: adopted (allocation semantics)
- With failures present: lazy 46.1 ns vs eager 37.3 ns (1.24x - the null check plus branchy first-add has a small cost when the failure branch actually runs), both allocate the list (216 B)
- All-valid path: lazy allocates NOTHING (0 B, 21.1 ns = 0.57x) while eager always pays 216 B - the win is structural, sized by how rare the failure path is

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| EagerList        | 37.32 ns | 1.242 ns | 1.820 ns | 35.22 ns | 41.92 ns | 40.05 ns |  1.00 |    0.07 | 0.0258 |   1,889 B |     216 B |        1.00 |
| LazyList         | 46.13 ns | 0.670 ns | 0.917 ns | 44.35 ns | 48.69 ns | 47.18 ns |  1.24 |    0.06 | 0.0258 |   1,918 B |     216 B |        1.00 |
| LazyListAllValid | 21.13 ns | 0.162 ns | 0.233 ns | 20.70 ns | 21.66 ns | 21.39 ns |  0.57 |    0.03 |      - |     259 B |         - |        0.00 |

## new int[0] vs Array.Empty (shared empty)

- Surprise: on net10, even new int[0] (const-foldable length) measures ZERO allocation - the runtime returns a shared instance from the helper path
- Codegen still differs: helper call 27 B vs cached-reference load 11 B; time 0.28 vs 0.31 ns (CIs overlap -> measurement-noise)
- Verdict: keep [] / Array.Empty as the default (smaller code, works for non-foldable lengths and collections); the allocation-avoidance claim is now enforced by the runtime itself in this shape

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| NewEmptyArray    | 0.1368 ns | 0.0018 ns | 0.0024 ns | 0.1324 ns | 0.1418 ns | 0.1397 ns |  1.00 |    0.02 |      12 B |         - |          NA |
| SharedEmptyArray | 0.1396 ns | 0.0018 ns | 0.0025 ns | 0.1349 ns | 0.1448 ns | 0.1427 ns |  1.02 |    0.02 |      12 B |         - |          NA |
