# COL-05: IEnumerable concrete-type dispatch

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted (conditional)
- List source 0.81x through the AsSpan branch on net11 (0.79x on net10); the array source gains nothing on either (0.97x)
- The iterator source moved: dispatch was 1.22x of plain enumeration on net10 and is 1.03x on net11

### EnumerableDispatchBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| EnumerateArray | 217.2 ns | 216.7 ns | 1.00 | - → - | 213 B → 213 B |
| DispatchArray | 211.6 ns | 211.1 ns | 1.00 | - → - | 625 B → 617 B |
| EnumerateList | 267.4 ns | 262.1 ns | 0.98 | - → - | 355 B → 297 B |
| DispatchList | 211.3 ns | 211.6 ns | 1.00 | - → - | 660 B → 659 B |
| EnumerateIterator | 495.4 ns | 505.4 ns | 1.02 | - → - | 491 B → 524 B |
| DispatchIterator | 602.3 ns | 518.3 ns | 0.86 | - → - | 984 B → 1,008 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.69 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Code Size | Allocated |
|------------------ |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|----------:|----------:|
| EnumerateArray    | .NET 10.0           | .NET 10.0 | 217.2 ns |  0.57 ns |  0.84 ns | 215.6 ns | 219.3 ns | 218.2 ns |     213 B |         - |
| DispatchArray     | .NET 10.0           | .NET 10.0 | 211.6 ns |  0.63 ns |  0.90 ns | 210.0 ns | 214.5 ns | 212.6 ns |     625 B |         - |
| EnumerateList     | .NET 10.0           | .NET 10.0 | 267.4 ns |  3.02 ns |  4.42 ns | 257.6 ns | 275.8 ns | 272.5 ns |     355 B |         - |
| DispatchList      | .NET 10.0           | .NET 10.0 | 211.3 ns |  0.35 ns |  0.50 ns | 210.1 ns | 212.4 ns | 211.7 ns |     660 B |         - |
| EnumerateIterator | .NET 10.0           | .NET 10.0 | 495.4 ns |  4.17 ns |  5.97 ns | 487.2 ns | 507.6 ns | 503.3 ns |     491 B |         - |
| DispatchIterator  | .NET 10.0           | .NET 10.0 | 602.3 ns |  7.10 ns | 10.40 ns | 579.5 ns | 625.1 ns | 615.1 ns |     984 B |         - |
| EnumerateArray    | MediumRun-.NET 11.0 | .NET 11.0 | 216.7 ns |  0.67 ns |  0.94 ns | 214.9 ns | 219.6 ns | 218.0 ns |     213 B |         - |
| DispatchArray     | MediumRun-.NET 11.0 | .NET 11.0 | 211.1 ns |  0.37 ns |  0.54 ns | 210.1 ns | 212.3 ns | 211.6 ns |     617 B |         - |
| EnumerateList     | MediumRun-.NET 11.0 | .NET 11.0 | 262.1 ns |  4.58 ns |  6.71 ns | 251.9 ns | 274.2 ns | 270.5 ns |     297 B |         - |
| DispatchList      | MediumRun-.NET 11.0 | .NET 11.0 | 211.6 ns |  0.74 ns |  1.08 ns | 210.1 ns | 214.8 ns | 213.0 ns |     659 B |         - |
| EnumerateIterator | MediumRun-.NET 11.0 | .NET 11.0 | 505.4 ns | 10.19 ns | 15.25 ns | 488.3 ns | 559.1 ns | 529.3 ns |     524 B |         - |
| DispatchIterator  | MediumRun-.NET 11.0 | .NET 11.0 | 518.3 ns |  2.94 ns |  4.21 ns | 510.8 ns | 526.7 ns | 523.9 ns |   1,008 B |         - |

## Earlier record

- Verdict: adopted (conditional)
- List source 0.83x via the CollectionsMarshal.AsSpan branch (253.7 -> 210.2 ns)
- Array source: no gain on net10 with dynamic PGO (guarded devirtualization already specializes the enumerator; 213.8 vs 209.8 ns) - on AOT the array branch remains valuable
- The iterator fallback pays 1.13x (486.7 -> 552.0 ns, non-overlapping CIs): a lazy-iterator argument pays for the type tests it never uses
- Net: the applicable range is narrow - adopt on hot APIs whose inputs are predominantly List, or under AOT; skip it on general-purpose IEnumerable parameters

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error    | StdDev   | Min      | Max      | P90      | Code Size | Allocated |
|------------------ |---------:|---------:|---------:|---------:|---------:|---------:|----------:|----------:|
| EnumerateArray    | 213.8 ns |  0.55 ns |  0.79 ns | 212.6 ns | 215.9 ns | 214.9 ns |     203 B |         - |
| DispatchArray     | 209.8 ns |  0.50 ns |  0.70 ns | 208.2 ns | 211.5 ns | 210.6 ns |     767 B |         - |
| EnumerateList     | 253.7 ns |  2.06 ns |  2.96 ns | 247.9 ns | 261.1 ns | 256.7 ns |     346 B |         - |
| DispatchList      | 210.2 ns |  0.79 ns |  1.11 ns | 209.1 ns | 212.9 ns | 211.9 ns |     786 B |         - |
| EnumerateIterator | 486.7 ns |  3.87 ns |  5.29 ns | 480.2 ns | 498.6 ns | 496.5 ns |     542 B |         - |
| DispatchIterator  | 552.0 ns | 28.85 ns | 42.29 ns | 501.2 ns | 623.2 ns | 598.8 ns |   1,175 B |         - |

