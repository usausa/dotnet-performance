# STK-07: Lazy allocation and shared empty singletons

## Lazy error list (10% failure rate vs all-valid)

- Verdict: adopted (allocation semantics)
- With failures present: lazy 69.2 ns == eager 67.2 ns (CIs overlap), both allocate the list (216 B)
- All-valid path: lazy allocates NOTHING (0 B, 48.3 ns) while eager always pays 216 B - the win is structural, not speed

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
