# DAT-01: Column resolution strategies (per-row cost, 3 columns)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. Cached ordinals are 0.13x of GetOrdinal per row on net10 and 0.15x on net11 (GetOrdinal itself got 0.92x faster: 7.27 → 6.66 ns)
- GetValue + cast stays 0.57-0.64x of GetOrdinal and 4x the typed getters

### OrdinalResolveBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| GetOrdinalPerRow | 7.2657 ns | 6.6584 ns | 0.92 | - → - | 2,246 B → 2,050 B |
| CachedOrdinalsStruct | 0.9792 ns | 0.9964 ns | 1.02 | - → - | 537 B → 593 B |
| CachedOrdinalsGetValueBoxing | 4.1200 ns | 4.2884 ns | 1.04 | 48 B → 48 B | 1,110 B → 1,138 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.26 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                       | Job                 | Runtime   | Mean      | Error     | StdDev    | Median   | Min       | Max      | P90      | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------------- |---------- |----------:|----------:|----------:|---------:|----------:|---------:|---------:|------:|----------:|-------:|----------:|------------:|
| GetOrdinalPerRow             | .NET 10.0           | .NET 10.0 | 7.2657 ns | 0.0440 ns | 0.0630 ns | 7.250 ns | 7.1796 ns | 7.448 ns | 7.328 ns |  1.00 |   2,246 B |      - |         - |          NA |
| CachedOrdinalsStruct         | .NET 10.0           | .NET 10.0 | 0.9792 ns | 0.0280 ns | 0.0410 ns | 1.006 ns | 0.9261 ns | 1.023 ns | 1.022 ns |  0.13 |     537 B |      - |         - |          NA |
| CachedOrdinalsGetValueBoxing | .NET 10.0           | .NET 10.0 | 4.1200 ns | 0.0453 ns | 0.0635 ns | 4.119 ns | 4.0414 ns | 4.281 ns | 4.208 ns |  0.57 |   1,110 B | 0.0057 |      48 B |          NA |
|                              |                     |           |           |           |           |          |           |          |          |       |           |        |           |             |
| GetOrdinalPerRow             | MediumRun-.NET 11.0 | .NET 11.0 | 6.6584 ns | 0.0097 ns | 0.0133 ns | 6.659 ns | 6.6364 ns | 6.680 ns | 6.676 ns |  1.00 |   2,050 B |      - |         - |          NA |
| CachedOrdinalsStruct         | MediumRun-.NET 11.0 | .NET 11.0 | 0.9964 ns | 0.0256 ns | 0.0350 ns | 1.021 ns | 0.9542 ns | 1.038 ns | 1.030 ns |  0.15 |     593 B |      - |         - |          NA |
| CachedOrdinalsGetValueBoxing | MediumRun-.NET 11.0 | .NET 11.0 | 4.2884 ns | 0.0118 ns | 0.0165 ns | 4.284 ns | 4.2668 ns | 4.333 ns | 4.308 ns |  0.64 |   1,138 B | 0.0057 |      48 B |          NA |

## Earlier record

- Verdict: adopted
- GetOrdinal per row: 7.45 ns/row -> cached ordinals struct + in: 1.00 ns/row (0.13x, ~7x faster), code size 2,219 B -> 533 B
- GetValue + cast instead of typed getters: 4.26 ns/row + 48 B/row boxing (int + bool) - use GetInt32/GetString/GetBoolean
- In-memory sealed reader stand-in; real-provider virtual dispatch and I/O are out of scope (deltas isolate the resolution strategy)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Mean      | Error     | StdDev    | Median   | Min       | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------------------- |----------:|----------:|----------:|---------:|----------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| GetOrdinalPerRow             | 7.4462 ns | 0.0616 ns | 0.0864 ns | 7.421 ns | 7.3529 ns | 7.730 ns | 7.562 ns |  1.00 |    0.02 |      - |   2,219 B |         - |          NA |
| CachedOrdinalsStruct         | 0.9983 ns | 0.0368 ns | 0.0491 ns | 1.027 ns | 0.9323 ns | 1.057 ns | 1.049 ns |  0.13 |    0.01 |      - |     533 B |         - |          NA |
| CachedOrdinalsGetValueBoxing | 4.2622 ns | 0.0581 ns | 0.0852 ns | 4.282 ns | 3.9987 ns | 4.383 ns | 4.341 ns |  0.57 |    0.01 | 0.0057 |   1,169 B |      48 B |          NA |
