# JIT-05: RuntimeHelpers.IsReferenceOrContainsReferences branch

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the clear for `int[]` folds away on both generations (0.80 ns, 28 B); for `string[]` both forms still clear (102 ns)
- The string-array forms grew from 219-237 B to 698 B of code on net11 with no time change

### ReferenceContainsBranchBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ClearAlwaysInt | 18.2074 ns | 18.5684 ns | 1.02 | - → - | 609 B → 655 B |
| ClearIfNeededInt | 0.8061 ns | 0.8012 ns | 0.99 | - → - | 28 B → 28 B |
| ClearAlwaysString | 102.1864 ns | 102.2774 ns | 1.00 | - → - | 237 B → 698 B |
| ClearIfNeededString | 103.1109 ns | 102.1913 ns | 0.99 | - → - | 219 B → 698 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.58 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean        | Error     | StdDev    | Median      | Min         | Max         | P90         | Code Size | Allocated |
|-------------------- |-------------------- |---------- |------------:|----------:|----------:|------------:|------------:|------------:|------------:|----------:|----------:|
| ClearAlwaysInt      | .NET 10.0           | .NET 10.0 |  18.2074 ns | 1.0588 ns | 1.5519 ns |  19.5851 ns |  16.4823 ns |  19.8127 ns |  19.7408 ns |     609 B |         - |
| ClearIfNeededInt    | .NET 10.0           | .NET 10.0 |   0.8061 ns | 0.0067 ns | 0.0100 ns |   0.8035 ns |   0.7945 ns |   0.8347 ns |   0.8187 ns |      28 B |         - |
| ClearAlwaysString   | .NET 10.0           | .NET 10.0 | 102.1864 ns | 0.1733 ns | 0.2485 ns | 102.1599 ns | 101.6596 ns | 102.8507 ns | 102.4865 ns |     237 B |         - |
| ClearIfNeededString | .NET 10.0           | .NET 10.0 | 103.1109 ns | 0.6537 ns | 0.9582 ns | 103.6781 ns | 101.8531 ns | 104.6159 ns | 104.1527 ns |     219 B |         - |
| ClearAlwaysInt      | MediumRun-.NET 11.0 | .NET 11.0 |  18.5684 ns | 0.5610 ns | 0.7864 ns |  19.1902 ns |  17.6507 ns |  19.4594 ns |  19.3497 ns |     655 B |         - |
| ClearIfNeededInt    | MediumRun-.NET 11.0 | .NET 11.0 |   0.8012 ns | 0.0027 ns | 0.0040 ns |   0.8015 ns |   0.7959 ns |   0.8113 ns |   0.8054 ns |      28 B |         - |
| ClearAlwaysString   | MediumRun-.NET 11.0 | .NET 11.0 | 102.2774 ns | 0.1509 ns | 0.2065 ns | 102.3023 ns | 101.8589 ns | 102.6363 ns | 102.5174 ns |     698 B |         - |
| ClearIfNeededString | MediumRun-.NET 11.0 | .NET 11.0 | 102.1913 ns | 0.1461 ns | 0.2000 ns | 102.2218 ns | 101.7777 ns | 102.5200 ns | 102.3882 ns |     698 B |         - |

## Earlier record

- Verdict: adopted
- Clear skipped for int[1024]: 19.0 ns -> 0.008 ns (code 510 B -> 28 B) - the JIT folds the intrinsic to a constant, and the branch plus the clear vanish entirely
- Zero measured overhead when clear is still required (string[]: 101.5 vs 102.4 ns, same code size)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean        | Error     | StdDev    | Min         | Max         | P90         | Code Size | Allocated |
|-------------------- |------------:|----------:|----------:|------------:|------------:|------------:|----------:|----------:|
| ClearAlwaysInt      |  18.9921 ns | 0.1033 ns | 0.1515 ns |  18.7426 ns |  19.2092 ns |  19.1336 ns |     504 B |         - |
| ClearIfNeededInt    |   0.0082 ns | 0.0025 ns | 0.0036 ns |   0.0010 ns |   0.0165 ns |   0.0121 ns |      28 B |         - |
| ClearAlwaysString   | 102.4121 ns | 1.7971 ns | 2.5192 ns | 101.1673 ns | 111.7872 ns | 103.3811 ns |     543 B |         - |
| ClearIfNeededString | 101.5426 ns | 0.2118 ns | 0.3104 ns | 101.1049 ns | 102.3920 ns | 101.9654 ns |     543 B |         - |
