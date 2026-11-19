# TYP-05: Unsafe.As vs castclass vs is-pattern (type-guaranteed reference)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - Unsafe.As is 0.51x of castclass on net11 (0.57x on net10); castclass reads 1.12x on net11 with the same 189 B of code

### UnsafeAsCastBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| CastClass | 372.7 ns | 417.9 ns | 1.12 | - → - | 189 B → 189 B |
| IsPattern | 332.0 ns | 324.4 ns | 0.98 | - → - | 57 B → 57 B |
| UnsafeAs | 211.6 ns | 212.0 ns | 1.00 | - → - | 33 B → 33 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.55 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method    | Job                 | Runtime   | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| CastClass | .NET 10.0           | .NET 10.0 | 372.7 ns | 37.84 ns | 53.05 ns | 419.0 ns | 316.2 ns | 426.4 ns | 424.5 ns |  1.00 |    0.00 |     189 B |         - |          NA |
| IsPattern | .NET 10.0           | .NET 10.0 | 332.0 ns |  5.60 ns |  8.38 ns | 330.4 ns | 322.1 ns | 346.5 ns | 344.0 ns |  0.91 |    0.13 |      57 B |         - |          NA |
| UnsafeAs  | .NET 10.0           | .NET 10.0 | 211.6 ns |  0.40 ns |  0.56 ns | 211.6 ns | 210.8 ns | 212.7 ns | 212.2 ns |  0.58 |    0.08 |      33 B |         - |          NA |
|           |                     |           |          |          |          |          |          |          |          |       |         |           |           |             |
| CastClass | MediumRun-.NET 11.0 | .NET 11.0 | 417.9 ns |  2.27 ns |  3.25 ns | 416.8 ns | 414.0 ns | 429.2 ns | 421.3 ns |  1.00 |    0.00 |     189 B |         - |          NA |
| IsPattern | MediumRun-.NET 11.0 | .NET 11.0 | 324.4 ns |  1.02 ns |  1.47 ns | 324.6 ns | 322.3 ns | 328.4 ns | 326.1 ns |  0.78 |    0.01 |      57 B |         - |          NA |
| UnsafeAs  | MediumRun-.NET 11.0 | .NET 11.0 | 212.0 ns |  0.58 ns |  0.84 ns | 211.9 ns | 210.7 ns | 214.0 ns | 213.2 ns |  0.51 |    0.00 |      33 B |         - |          NA |

## Earlier record

- Verdict: adopted (where the invariant is structurally guaranteed)
- castclass 335.5 ns / is-pattern 324.7 ns (0.97x) / Unsafe.As 212.8 ns (0.63x)
- Code size 274 / 57 / 33 B - the cast helper and its EH path dominate the castclass form
- Safety: no type check at all; a wrong type is silent corruption. Restrict to registries where the value's type is established at registration

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method    | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| CastClass | 335.5 ns | 2.02 ns | 2.77 ns | 331.6 ns | 341.2 ns | 339.1 ns |  1.00 |     274 B |         - |          NA |
| IsPattern | 324.7 ns | 1.82 ns | 2.49 ns | 321.7 ns | 330.7 ns | 328.9 ns |  0.97 |      57 B |         - |          NA |
| UnsafeAs  | 212.8 ns | 1.23 ns | 1.76 ns | 210.5 ns | 217.3 ns | 215.2 ns |  0.63 |      33 B |         - |          NA |
