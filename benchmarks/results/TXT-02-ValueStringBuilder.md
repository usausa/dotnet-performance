# TXT-02: ValueStringBuilder

- Verdict: adopted (implemented)
- 0.31-0.33x vs StringBuilder (no capacity), 760 B -> 216 B (result string only)
- On par with stackalloc-backed DefaultInterpolatedStringHandler
- Capacity-specified StringBuilder alone gives 0.43-0.47x

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|---------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| StringBuilderDefault  | 50.28 ns | 1.421 ns | 1.993 ns | 48.33 ns | 58.30 ns | 52.02 ns |  1.00 |    0.05 | 0.0908 |   1,565 B | 0.0002 |     760 B |        1.00 |
| StringBuilderCapacity | 22.60 ns | 0.369 ns | 0.552 ns | 21.76 ns | 23.46 ns | 23.18 ns |  0.45 |    0.02 | 0.0650 |   1,577 B | 0.0001 |     544 B |        0.72 |
| InterpolatedHandler   | 14.88 ns | 0.770 ns | 1.105 ns | 13.61 ns | 19.00 ns | 15.78 ns |  0.30 |    0.02 | 0.0258 |   1,297 B |      - |     216 B |        0.28 |
| ValueStringBuilder    | 14.98 ns | 0.173 ns | 0.243 ns | 14.38 ns | 15.52 ns | 15.32 ns |  0.30 |    0.01 | 0.0258 |   1,496 B |      - |     216 B |        0.28 |
