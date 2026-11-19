# STK-05: Boxing vs cached boxes (-1/0/1 flag values)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the cached box is 0.69x of direct boxing on net11 (0.76x on net10), 0 B against 24 B

### BoxingCacheBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DirectBoxing | 3.604 ns | 3.475 ns | 0.96 | 24 B → 24 B | 557 B → 522 B |
| CachedBox | 2.725 ns | 2.393 ns | 0.88 | - → - | 634 B → 605 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.46 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method       | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| DirectBoxing | .NET 10.0           | .NET 10.0 | 3.604 ns | 0.0256 ns | 0.0351 ns | 3.567 ns | 3.718 ns | 3.644 ns |  1.00 |    0.00 |     557 B | 0.0029 | 0.0000 |      24 B |        1.00 |
| CachedBox    | .NET 10.0           | .NET 10.0 | 2.725 ns | 0.0080 ns | 0.0118 ns | 2.698 ns | 2.741 ns | 2.739 ns |  0.76 |    0.01 |     634 B |      - |      - |         - |        0.00 |
|              |                     |           |          |           |           |          |          |          |       |         |           |        |        |           |             |
| DirectBoxing | MediumRun-.NET 11.0 | .NET 11.0 | 3.475 ns | 0.0331 ns | 0.0485 ns | 3.405 ns | 3.600 ns | 3.566 ns |  1.00 |    0.00 |     522 B | 0.0029 | 0.0000 |      24 B |        1.00 |
| CachedBox    | MediumRun-.NET 11.0 | .NET 11.0 | 2.393 ns | 0.0445 ns | 0.0594 ns | 2.323 ns | 2.473 ns | 2.463 ns |  0.69 |    0.02 |     605 B |      - |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted (wins on both axes here)
- Direct boxing: 3.68 ns + 24 B per op; cached-box switch: 2.54 ns / 0 B (0.69x)
- The three-way branch is cheaper than the pointer-bump allocation on this core; where allocation outpaces branching the time can flip, but the GC-pressure win holds regardless
- Escape analysis already stack-allocates non-escaping boxes (see pattern body) - cache only escaping, hot, known-value boxes

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| DirectBoxing | 3.677 ns | 0.0845 ns | 0.1156 ns | 3.281 ns | 3.911 ns | 3.808 ns |  1.00 |    0.04 | 0.0029 |     597 B | 0.0000 |      24 B |        1.00 |
| CachedBox    | 2.537 ns | 0.0182 ns | 0.0255 ns | 2.492 ns | 2.615 ns | 2.568 ns |  0.69 |    0.02 |      - |     677 B |      - |         - |        0.00 |
