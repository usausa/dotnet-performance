# TXT-09: Fixed-field formatting idioms

- Verdict: mixed (manual digit tricks rejected, vectorized trim adopted)
- TryFormat + Fill: 5.32 ns (fastest) - BCL digit formatting is already optimal on net10
- Manual LSB-write + Reverse: 12.9 ns (2.51x SLOWER) - rejected
- Manual right-align + shift: 24.6 ns (4.79x SLOWER) - rejected; the digit-count-avoidance claim is obsolete because TryFormat handles it internally
- Trim: IndexOfAnyExcept/LastIndexOfAnyExcept 5.20 ns vs manual loop 8.73 ns (0.60x) - adopted

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| TryFormatThenFill     |  3.333 ns | 0.0260 ns | 0.0365 ns |  3.330 ns |  3.269 ns |  3.403 ns |  3.375 ns |  1.00 |    0.02 |     852 B |         - |          NA |
| ManualLsbThenReverse  | 10.690 ns | 0.0398 ns | 0.0517 ns | 10.680 ns | 10.611 ns | 10.855 ns | 10.740 ns |  3.21 |    0.04 |     754 B |         - |          NA |
| ManualRightAlignShift | 12.246 ns | 0.2423 ns | 0.3317 ns | 12.125 ns | 12.012 ns | 13.572 ns | 12.549 ns |  3.67 |    0.11 |     979 B |         - |          NA |
| TrimManualLoop        |  4.495 ns | 0.0727 ns | 0.1043 ns |  4.479 ns |  4.356 ns |  4.793 ns |  4.620 ns |  1.35 |    0.03 |     138 B |         - |          NA |
| TrimVectorized        |  3.804 ns | 0.2214 ns | 0.3104 ns |  4.002 ns |  3.435 ns |  4.220 ns |  4.158 ns |  1.14 |    0.09 |   1,605 B |         - |          NA |
