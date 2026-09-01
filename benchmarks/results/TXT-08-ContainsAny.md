# TXT-08: ContainsAny vs IndexOfAny(...) >= 0

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
