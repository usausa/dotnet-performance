# ASY-04: IAsyncEnumerable cost

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted (cost awareness)
- await foreach over synchronously completing items is 13.3x of foreach on net10 and 15.6x on net11 (7.21 → 8.43 ns per item)

### AsyncEnumerableBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| SyncForeach | 0.5406 ns | 0.5396 ns | 1.00 | - → - | 494 B → 162 B |
| AsyncForeach | 7.2081 ns | 8.4348 ns | 1.17 | - → - | 6,581 B → 6,463 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.29 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method       | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| SyncForeach  | .NET 10.0           | .NET 10.0 | 0.5406 ns | 0.0334 ns | 0.0480 ns | 0.5729 ns | 0.4859 ns | 0.5958 ns | 0.5934 ns |  1.00 |    0.00 | 0.0000 |     494 B |         - |          NA |
| AsyncForeach | .NET 10.0           | .NET 10.0 | 7.2081 ns | 0.0289 ns | 0.0423 ns | 7.2059 ns | 7.0997 ns | 7.2890 ns | 7.2558 ns | 13.44 |    1.19 | 0.0000 |   6,581 B |         - |          NA |
|              |                     |           |           |           |           |           |           |           |           |       |         |        |           |           |             |
| SyncForeach  | MediumRun-.NET 11.0 | .NET 11.0 | 0.5396 ns | 0.0336 ns | 0.0502 ns | 0.5333 ns | 0.4798 ns | 0.6033 ns | 0.5972 ns |  1.00 |    0.00 | 0.0000 |     162 B |         - |          NA |
| AsyncForeach | MediumRun-.NET 11.0 | .NET 11.0 | 8.4348 ns | 0.0183 ns | 0.0268 ns | 8.4404 ns | 8.3847 ns | 8.4874 ns | 8.4616 ns | 15.76 |    1.44 | 0.0000 |   6,463 B |         - |          NA |

## Earlier record

- Verdict: adopted (cost awareness)
- await foreach over sync-completing items: 15.2x per item (0.48 -> 7.3 ns)
- Use IAsyncEnumerable only when element production is truly asynchronous

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| SyncForeach  | 0.4775 ns | 0.0014 ns | 0.0020 ns | 0.4739 ns | 0.4813 ns | 0.4799 ns |  1.00 |    0.01 |     488 B | 0.0000 |         - |          NA |
| AsyncForeach | 7.2553 ns | 0.0167 ns | 0.0250 ns | 7.2124 ns | 7.2945 ns | 7.2887 ns | 15.20 |    0.08 |   3,896 B | 0.0000 |         - |          NA |
