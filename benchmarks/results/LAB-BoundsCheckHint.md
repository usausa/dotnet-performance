# LAB: Tail-touch bounds check hint (rejected, R-15)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - still rejected (R-15). The tail touch neither helps nor hurts on net11
- All four forms run at 210.2-211.6 ns on both generations; `SumWithTailTouch` is 104 B of Tier1 code on both
- `DOTNET_JitDisasm` (FullOpts): `SumWithTailTouchCore` is 78 B with one bounds check on both generations, the same
  as `SumWithExternalLength` (77 B). The net11 folding of constant-index guards (A1, #127439 / #124705) does not reach
  this external-length loop
- Criterion for moving R-15 from "not needed" to "harmful" (the touch is slower or larger): not met

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.13 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| SumByLength          | MediumRun-.NET 10.0 | .NET 10.0 | 210.8 ns | 0.42 ns | 0.61 ns | 209.8 ns | 212.1 ns | 211.7 ns |  1.00 |     106 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 10.0 | .NET 10.0 | 211.1 ns | 0.35 ns | 0.52 ns | 210.2 ns | 212.0 ns | 211.9 ns |  1.00 |      44 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 10.0 | .NET 10.0 | 211.0 ns | 0.46 ns | 0.64 ns | 209.7 ns | 212.4 ns | 211.8 ns |  1.00 |     104 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 10.0 | .NET 10.0 | 211.1 ns | 0.63 ns | 0.88 ns | 209.7 ns | 213.8 ns | 212.1 ns |  1.00 |     143 B |         - |          NA |
|                      |                     |           |          |         |         |          |          |          |       |           |           |             |
| SumByLength          | MediumRun-.NET 11.0 | .NET 11.0 | 210.8 ns | 0.58 ns | 0.85 ns | 209.5 ns | 212.9 ns | 212.0 ns |  1.00 |     108 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 11.0 | .NET 11.0 | 211.2 ns | 0.49 ns | 0.69 ns | 209.9 ns | 212.9 ns | 212.0 ns |  1.00 |      44 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 11.0 | .NET 11.0 | 210.2 ns | 0.43 ns | 0.60 ns | 208.9 ns | 211.4 ns | 210.8 ns |  1.00 |     104 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 11.0 | .NET 11.0 | 211.6 ns | 1.39 ns | 2.00 ns | 209.4 ns | 216.2 ns | 214.8 ns |  1.00 |     128 B |         - |          NA |

## Earlier record

- Verdict: rejected
- No difference across all variants on net10 and net8 (1024-int sum; measured when the class ran net10 + net8 jobs, it now runs net10 + net11)
- array.Length loop form has the smallest code (34 B vs 94-140 B)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 8.0  : .NET 8.0.29 (8.0.29, 8.0.2926.32403), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SumByLength          | MediumRun-.NET 10.0 | .NET 10.0 | 212.1 ns | 4.85 ns | 7.26 ns | 207.8 ns | 233.7 ns | 226.2 ns |  1.00 |    0.05 |      96 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 10.0 | .NET 10.0 | 211.4 ns | 2.75 ns | 3.86 ns | 208.6 ns | 226.4 ns | 214.2 ns |  1.00 |    0.04 |      34 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 10.0 | .NET 10.0 | 209.6 ns | 0.61 ns | 0.88 ns | 208.6 ns | 212.3 ns | 211.0 ns |  0.99 |    0.03 |      94 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 10.0 | .NET 10.0 | 209.4 ns | 0.33 ns | 0.45 ns | 208.8 ns | 210.5 ns | 210.0 ns |  0.99 |    0.03 |     133 B |         - |          NA |
|                      |                     |           |          |         |         |          |          |          |       |         |           |           |             |
| SumByLength          | MediumRun-.NET 8.0  | .NET 8.0  | 209.0 ns | 0.64 ns | 0.85 ns | 207.7 ns | 212.0 ns | 209.8 ns |  1.00 |    0.01 |      99 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 8.0  | .NET 8.0  | 209.8 ns | 1.68 ns | 2.36 ns | 207.6 ns | 219.0 ns | 211.9 ns |  1.00 |    0.01 |      39 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 8.0  | .NET 8.0  | 208.7 ns | 0.43 ns | 0.63 ns | 207.1 ns | 209.7 ns | 209.5 ns |  1.00 |    0.00 |      98 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 8.0  | .NET 8.0  | 208.6 ns | 0.46 ns | 0.64 ns | 207.2 ns | 210.0 ns | 209.3 ns |  1.00 |    0.00 |     140 B |         - |          NA |
