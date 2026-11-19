# TXT-08: ContainsAny vs IndexOfAny(...) >= 0

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - ContainsAny is 0.67x / 0.85x / 0.98x of IndexOfAny (early / late / absent) on net11
- IndexOfAny is 1.05-1.11x slower on net11; ContainsAny is unchanged

### ContainsAnyBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| IndexOfAny_Early | 1.633 ns | 1.815 ns | 1.11 | - → - | 560 B → 587 B |
| ContainsAny_Early | 1.231 ns | 1.224 ns | 0.99 | - → - | 401 B → 404 B |
| IndexOfAny_Late | 4.100 ns | 4.304 ns | 1.05 | - → - | 557 B → 588 B |
| ContainsAny_Late | 3.668 ns | 3.661 ns | 1.00 | - → - | 376 B → 396 B |
| IndexOfAny_Absent | 3.685 ns | 3.862 ns | 1.05 | - → - | 560 B → 587 B |
| ContainsAny_Absent | 3.673 ns | 3.766 ns | 1.03 | - → - | 396 B → 391 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.61 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| IndexOfAny_Early   | .NET 10.0           | .NET 10.0 | 1.633 ns | 0.0159 ns | 0.0223 ns | 1.624 ns | 1.615 ns | 1.706 ns | 1.660 ns |  1.00 |    0.00 |     560 B |         - |          NA |
| ContainsAny_Early  | .NET 10.0           | .NET 10.0 | 1.231 ns | 0.0050 ns | 0.0075 ns | 1.229 ns | 1.219 ns | 1.247 ns | 1.242 ns |  0.75 |    0.01 |     401 B |         - |          NA |
| IndexOfAny_Late    | .NET 10.0           | .NET 10.0 | 4.100 ns | 0.0109 ns | 0.0153 ns | 4.102 ns | 4.065 ns | 4.129 ns | 4.116 ns |  2.51 |    0.03 |     557 B |         - |          NA |
| ContainsAny_Late   | .NET 10.0           | .NET 10.0 | 3.668 ns | 0.0137 ns | 0.0201 ns | 3.665 ns | 3.644 ns | 3.715 ns | 3.696 ns |  2.25 |    0.03 |     376 B |         - |          NA |
| IndexOfAny_Absent  | .NET 10.0           | .NET 10.0 | 3.685 ns | 0.0102 ns | 0.0146 ns | 3.686 ns | 3.659 ns | 3.714 ns | 3.706 ns |  2.26 |    0.03 |     560 B |         - |          NA |
| ContainsAny_Absent | .NET 10.0           | .NET 10.0 | 3.673 ns | 0.0168 ns | 0.0230 ns | 3.669 ns | 3.645 ns | 3.738 ns | 3.698 ns |  2.25 |    0.03 |     396 B |         - |          NA |
|                    |                     |           |          |           |           |          |          |          |          |       |         |           |           |             |
| IndexOfAny_Early   | MediumRun-.NET 11.0 | .NET 11.0 | 1.815 ns | 0.0071 ns | 0.0097 ns | 1.812 ns | 1.800 ns | 1.841 ns | 1.826 ns |  1.00 |    0.00 |     587 B |         - |          NA |
| ContainsAny_Early  | MediumRun-.NET 11.0 | .NET 11.0 | 1.224 ns | 0.0034 ns | 0.0048 ns | 1.224 ns | 1.216 ns | 1.233 ns | 1.231 ns |  0.67 |    0.00 |     404 B |         - |          NA |
| IndexOfAny_Late    | MediumRun-.NET 11.0 | .NET 11.0 | 4.304 ns | 0.0401 ns | 0.0575 ns | 4.288 ns | 4.253 ns | 4.490 ns | 4.366 ns |  2.37 |    0.03 |     588 B |         - |          NA |
| ContainsAny_Late   | MediumRun-.NET 11.0 | .NET 11.0 | 3.661 ns | 0.0101 ns | 0.0141 ns | 3.665 ns | 3.638 ns | 3.697 ns | 3.677 ns |  2.02 |    0.01 |     396 B |         - |          NA |
| IndexOfAny_Absent  | MediumRun-.NET 11.0 | .NET 11.0 | 3.862 ns | 0.0099 ns | 0.0142 ns | 3.865 ns | 3.837 ns | 3.904 ns | 3.875 ns |  2.13 |    0.01 |     587 B |         - |          NA |
| ContainsAny_Absent | MediumRun-.NET 11.0 | .NET 11.0 | 3.766 ns | 0.0853 ns | 0.1224 ns | 3.691 ns | 3.642 ns | 3.956 ns | 3.938 ns |  2.08 |    0.07 |     391 B |         - |          NA |

## Earlier record

- Verdict: adopted (use ContainsAny whenever the position is not needed)
- Re-measured on x86-64-v4 (Zen 5). The verdict is unchanged and **two rows that were noise on x86-64-v3
  (Zen 3) now resolve**, both in ContainsAny's favour or as a tie
- Early match (index 4 of 256): **0.66x**, CIs non-overlapping (x86-64-v3: 0.73x)
- Late match (index 248): **0.86x**, CIs non-overlapping - on x86-64-v3 this row read 1.08x and was recorded as
  noise. It is a real win here, not a regression
- No match (full scan): 1.03x with **overlapping CIs** - a tie (x86-64-v3: 0.90x, also within noise)
- Code size: about 30% smaller in every case (560/549/553 B -> 401/384/396 B), **within 6 B of the x86-64-v3
  figures**, so that axis is machine independent
- Rationale: ContainsAny does not have to extract the lane position out of the matching vector, so the saving
  grows the earlier the data matches - 0.66x early, 0.86x late, a tie when there is nothing to find

| Match position (256 chars) | x86-64-v3 | x86-64-v4 |
|---|---:|---:|
| Near the head (index 4) | 0.73x | **0.66x** |
| Near the tail (index 248) | 1.08x (noise) | **0.86x** (resolved) |
| No match (full scan) | 0.90x (noise) | 1.03x (tie) |

## What to weigh

1. **Default to `ContainsAny` wherever the position is unused.** Two axes point the same way on both machines:
   time (0.66-0.86x when there is a match) and code size (about 30% smaller everywhere)
2. **The gain scales with how early the match occurs**, because what it removes is the lane-extraction step that
   runs once a match is found. A scan that usually finds nothing gets code size and nothing else - which is
   still a reason to prefer it, not a reason to avoid it
3. **Do not read the x86-64-v3 "1.08x" as a regression risk.** That row was inside its own error bars there and
   resolves to 0.86x in favour of ContainsAny on a machine that measures it cleanly

## x86-64-v4

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9278/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| IndexOfAny_Early   | 0.7406 ns | 0.0143 ns | 0.0200 ns | 0.7107 ns | 0.7866 ns | 0.7675 ns |  1.00 |    0.04 |     560 B |         - |          NA |
| ContainsAny_Early  | 0.4883 ns | 0.0119 ns | 0.0166 ns | 0.4507 ns | 0.5121 ns | 0.5054 ns |  0.66 |    0.03 |     401 B |         - |          NA |
| IndexOfAny_Late    | 3.7330 ns | 0.0286 ns | 0.0419 ns | 3.6572 ns | 3.8278 ns | 3.7836 ns |  5.04 |    0.14 |     549 B |         - |          NA |
| ContainsAny_Late   | 3.2236 ns | 0.0415 ns | 0.0594 ns | 3.1255 ns | 3.3879 ns | 3.2961 ns |  4.36 |    0.14 |     384 B |         - |          NA |
| IndexOfAny_Absent  | 3.4338 ns | 0.1559 ns | 0.2235 ns | 3.0786 ns | 3.6979 ns | 3.6679 ns |  4.64 |    0.32 |     553 B |         - |          NA |
| ContainsAny_Absent | 3.5464 ns | 0.2265 ns | 0.3248 ns | 3.1156 ns | 4.3188 ns | 3.9596 ns |  4.79 |    0.45 |     396 B |         - |          NA |
