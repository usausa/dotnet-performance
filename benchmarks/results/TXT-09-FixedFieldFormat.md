# TXT-09: Fixed-field formatting idioms

- Verdict: mixed (manual digit tricks rejected, vectorized trim adopted)
- TryFormat + Fill: 5.32 ns (fastest) - BCL digit formatting is already optimal on net10
- Manual LSB-write + Reverse: 12.9 ns (2.51x SLOWER) - rejected
- Manual right-align + shift: 24.6 ns (4.79x SLOWER) - rejected; the digit-count-avoidance claim is obsolete because TryFormat handles it internally
- Trim: IndexOfAnyExcept/LastIndexOfAnyExcept 5.20 ns vs manual loop 8.73 ns (0.60x) - adopted

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| TryFormatThenFill     |  5.323 ns | 0.7847 ns | 1.1746 ns |  4.581 ns |  8.006 ns |  7.512 ns |  1.04 |    0.29 |     857 B |         - |          NA |
| ManualLsbThenReverse  | 12.903 ns | 0.3905 ns | 0.5213 ns | 12.285 ns | 14.303 ns | 13.662 ns |  2.51 |    0.43 |     661 B |         - |          NA |
| ManualRightAlignShift | 24.559 ns | 0.7811 ns | 1.1691 ns | 22.520 ns | 27.557 ns | 26.038 ns |  4.79 |    0.83 |     996 B |         - |          NA |
| TrimManualLoop        |  8.733 ns | 0.3682 ns | 0.5281 ns |  7.831 ns |  9.639 ns |  9.516 ns |  1.70 |    0.30 |     138 B |         - |          NA |
| TrimVectorized        |  5.204 ns | 0.4324 ns | 0.6472 ns |  4.361 ns |  6.050 ns |  5.915 ns |  1.01 |    0.21 |   1,455 B |         - |          NA |
