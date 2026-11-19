# TYP-02: BitwiseComparer vs default comparers (16-byte struct keys)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the default comparer on a plain struct still boxes (96 B) and is 1.84x of the bitwise comparer on net11 (2.13x on net10)

### BitwiseComparerBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DefaultComparerPlain | 17.627 ns | 15.235 ns | 0.86 | 96 B → 96 B | 10,415 B → 11,011 B |
| DefaultComparerEquatable | 3.734 ns | 3.725 ns | 1.00 | - → - | 626 B → 609 B |
| BitwiseComparerPlain | 8.293 ns | 8.296 ns | 1.00 | - → - | 4,260 B → 4,930 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.22 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                   | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|-------:|----------:|------------:|
| DefaultComparerPlain     | .NET 10.0           | .NET 10.0 | 17.627 ns | 0.3494 ns | 0.5229 ns | 17.676 ns | 16.916 ns | 18.259 ns | 18.205 ns |  1.00 |  10,415 B | 0.0115 |      96 B |        1.00 |
| DefaultComparerEquatable | .NET 10.0           | .NET 10.0 |  3.734 ns | 0.0666 ns | 0.0911 ns |  3.677 ns |  3.628 ns |  3.854 ns |  3.833 ns |  0.21 |     626 B |      - |         - |        0.00 |
| BitwiseComparerPlain     | .NET 10.0           | .NET 10.0 |  8.293 ns | 0.0263 ns | 0.0377 ns |  8.286 ns |  8.226 ns |  8.380 ns |  8.336 ns |  0.47 |   4,260 B |      - |         - |        0.00 |
|                          |                     |           |           |           |           |           |           |           |           |       |           |        |           |             |
| DefaultComparerPlain     | MediumRun-.NET 11.0 | .NET 11.0 | 15.235 ns | 0.1726 ns | 0.2420 ns | 15.292 ns | 14.863 ns | 15.569 ns | 15.521 ns |  1.00 |  11,011 B | 0.0115 |      96 B |        1.00 |
| DefaultComparerEquatable | MediumRun-.NET 11.0 | .NET 11.0 |  3.725 ns | 0.0302 ns | 0.0443 ns |  3.719 ns |  3.662 ns |  3.804 ns |  3.776 ns |  0.24 |     609 B |      - |         - |        0.00 |
| BitwiseComparerPlain     | MediumRun-.NET 11.0 | .NET 11.0 |  8.296 ns | 0.0478 ns | 0.0701 ns |  8.306 ns |  8.195 ns |  8.436 ns |  8.384 ns |  0.54 |   4,930 B |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted (for structs without usable IEquatable)
- Dictionary lookup per key: default comparer on plain struct 15.8 ns + 96 B boxing per lookup (ObjectEqualityComparer path)
- BitwiseComparer on the same plain struct: 8.4 ns / 0 B (0.54x, no boxing, no Equals to write)
- Hand-written IEquatable is still fastest: 3.7 ns (0.23x) - implement it when you own the type; use BitwiseComparer for external types or to bypass custom Equals

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| DefaultComparerPlain     | 15.772 ns | 0.4323 ns | 0.6061 ns | 15.189 ns | 17.973 ns | 16.574 ns |  1.00 |    0.05 | 0.0115 |   5,509 B |      96 B |        1.00 |
| DefaultComparerEquatable |  3.687 ns | 0.0551 ns | 0.0808 ns |  3.579 ns |  3.949 ns |  3.759 ns |  0.23 |    0.01 |      - |     620 B |         - |        0.00 |
| BitwiseComparerPlain     |  8.429 ns | 0.0372 ns | 0.0521 ns |  8.340 ns |  8.514 ns |  8.489 ns |  0.54 |    0.02 |      - |   2,128 B |         - |        0.00 |
