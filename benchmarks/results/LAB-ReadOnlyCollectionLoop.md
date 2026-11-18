# LAB: ReadOnlyCollection<int> foreach vs indexer on .NET 10 (R-04 follow-up)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: the .NET 11 post's 32 → 0 B (#122946) did NOT reproduce. `foreach` over `ReadOnlyCollection<int>` still
  allocates 32 B per loop on net11 in every shape tried: the instance field, a static readonly field, and a wrapper
  created in the method
- Time moved: `foreach` is 1.32x of the indexer on net10 and 1.15x on net11 (593.1 → 517.2 ns), and its code shrinks
  from 814 to 615 B. The indexer is unchanged (450 ns, 134 B)
- The R-04 guidance "index wrapper collections" stays. The net10 non-reproduction recorded below was not a
  generation difference

### Instance field (the original shape)

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.37 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method  | Job                 | Runtime   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------- |-------------------- |---------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| Indexer | .NET 10.0           | .NET 10.0 | 450.1 ns | 1.94 ns |  2.85 ns | 446.2 ns | 456.8 ns | 453.7 ns |  1.00 |    0.00 |     134 B |      - |         - |          NA |
| Foreach | .NET 10.0           | .NET 10.0 | 593.1 ns | 8.27 ns | 12.13 ns | 567.2 ns | 611.2 ns | 608.2 ns |  1.32 |    0.03 |     814 B | 0.0038 |      32 B |          NA |
|         |                     |           |          |         |          |          |          |          |       |         |           |        |           |             |
| Indexer | MediumRun-.NET 11.0 | .NET 11.0 | 449.9 ns | 1.88 ns |  2.70 ns | 445.7 ns | 456.5 ns | 452.8 ns |  1.00 |    0.00 |     134 B |      - |         - |          NA |
| Foreach | MediumRun-.NET 11.0 | .NET 11.0 | 517.2 ns | 2.83 ns |  4.15 ns | 509.6 ns | 526.1 ns | 522.8 ns |  1.15 |    0.01 |     615 B | 0.0038 |      32 B |          NA |

### Static readonly field and a wrapper created in the method

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.67 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Code Size | Gen0   | Allocated |
|---------------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|----------:|-------:|----------:|
| Foreach               | .NET 10.0           | .NET 10.0 | 589.8 ns | 6.48 ns | 9.70 ns | 564.8 ns | 602.0 ns | 600.5 ns |     814 B | 0.0038 |      32 B |
| ForeachStaticReadOnly | .NET 10.0           | .NET 10.0 | 524.7 ns | 4.06 ns | 5.82 ns | 514.3 ns | 536.0 ns | 531.4 ns |     826 B | 0.0038 |      32 B |
| ForeachLocalWrapper   | .NET 10.0           | .NET 10.0 | 513.5 ns | 2.78 ns | 4.07 ns | 508.1 ns | 524.7 ns | 521.2 ns |     748 B | 0.0038 |      32 B |
| Foreach               | MediumRun-.NET 11.0 | .NET 11.0 | 521.9 ns | 2.90 ns | 4.26 ns | 513.1 ns | 529.5 ns | 526.9 ns |     617 B | 0.0038 |      32 B |
| ForeachStaticReadOnly | MediumRun-.NET 11.0 | .NET 11.0 | 517.7 ns | 4.61 ns | 6.61 ns | 506.7 ns | 530.0 ns | 528.1 ns |     616 B | 0.0038 |      32 B |
| ForeachLocalWrapper   | MediumRun-.NET 11.0 | .NET 11.0 | 511.3 ns | 2.47 ns | 3.54 ns | 506.0 ns | 519.6 ns | 516.3 ns |     538 B | 0.0038 |      32 B |

## Earlier record

- Verdict: the .NET 10 post's 'foreach now beats the indexer' did NOT reproduce. foreach is 1.21x slower (558.6 vs 460.0 ns), still allocates the 32 B enumerator, and compiles to 664 B against 134 B for the indexer. The B550H (x86-64-v3) provisional run agreed (1.26x, 32 B, 684 vs 134 B)
- What the disassembly shows: the array-interface devirtualization does reach through the wrapper's IList<T> field — both methods guard on `MT_System.Int32[]`, and foreach inlines `SZGenericArrayEnumerator<int>.MoveNext/Current` behind a guard on the enumerator's MT. But the enumerator object is still created with `CORINFO_HELP_NEWSFAST` (no stack allocation), so the loop reloads `_index` from the heap and performs three compares per element (MoveNext limit, Current limit, array bounds → RNGCHKFAIL), and the fallback interface-call paths inflate the method to 664 B
- The indexer loop is two type guards plus one bounds check per element, reading the array directly, with no allocation
- The indexer remains the safe choice for wrapper collections

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method  | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| Indexer | 460.0 ns |  3.28 ns |  4.81 ns | 454.4 ns | 477.3 ns | 465.2 ns |  1.00 |    0.01 |      - |     134 B |         - |          NA |
| Foreach | 558.6 ns | 30.28 ns | 42.45 ns | 509.1 ns | 615.3 ns | 602.6 ns |  1.21 |    0.09 | 0.0038 |     664 B |      32 B |          NA |
