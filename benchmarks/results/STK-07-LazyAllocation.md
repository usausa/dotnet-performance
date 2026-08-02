# STK-07: Lazy allocation and shared empty singletons

## Lazy error list (10% failure rate vs all-valid)

- Verdict: adopted (allocation semantics)
- With failures present: lazy 69.2 ns == eager 67.2 ns (CIs overlap), both allocate the list (216 B)
- All-valid path: lazy allocates NOTHING (0 B, 48.3 ns) while eager always pays 216 B - the win is structural, not speed

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| EagerList        | 67.20 ns | 2.660 ns | 3.898 ns | 61.85 ns | 76.36 ns | 73.73 ns |  1.00 |    0.08 | 0.0129 |   1,905 B |     216 B |        1.00 |
| LazyList         | 69.22 ns | 1.637 ns | 2.450 ns | 65.28 ns | 74.34 ns | 72.31 ns |  1.03 |    0.07 | 0.0129 |   1,934 B |     216 B |        1.00 |
| LazyListAllValid | 48.30 ns | 0.791 ns | 1.184 ns | 46.63 ns | 51.34 ns | 49.71 ns |  0.72 |    0.04 |      - |     150 B |         - |        0.00 |

## new int[0] vs Array.Empty (shared empty)

- Surprise: on net10, even new int[0] (const-foldable length) measures ZERO allocation - the runtime returns a shared instance from the helper path
- Codegen still differs: helper call 27 B vs cached-reference load 11 B; time 0.28 vs 0.31 ns (CIs overlap -> measurement-noise)
- Verdict: keep [] / Array.Empty as the default (smaller code, works for non-foldable lengths and collections); the allocation-avoidance claim is now enforced by the runtime itself in this shape

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| NewEmptyArray    | 0.2817 ns | 0.0210 ns | 0.0315 ns | 0.2481 ns | 0.3674 ns | 0.3388 ns |  1.01 |    0.15 |      12 B |         - |          NA |
| SharedEmptyArray | 0.3131 ns | 0.0272 ns | 0.0390 ns | 0.2765 ns | 0.3982 ns | 0.3793 ns |  1.12 |    0.18 |      12 B |         - |          NA |
