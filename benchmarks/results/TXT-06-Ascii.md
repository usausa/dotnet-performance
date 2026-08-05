# TXT-06: Ascii class comparison

- Verdict: adopted
- Ascii.EqualsIgnoreCase (bytes) 0.76x vs string.Equals(OrdinalIgnoreCase)
- Manual (b | 0x20) compare 0.59x, but collides on symbol pairs ('@' vs backquote) - closed token sets only

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| StringEqualsIgnoreCase | 55.47 ns | 0.190 ns | 0.284 ns | 55.05 ns | 55.95 ns | 55.86 ns |  1.00 |    0.01 |   1,848 B |         - |          NA |
| AsciiEqualsIgnoreCase  | 41.94 ns | 2.897 ns | 4.336 ns | 39.49 ns | 57.22 ns | 48.95 ns |  0.76 |    0.08 |   1,215 B |         - |          NA |
| ManualOr20Compare      | 32.66 ns | 0.321 ns | 0.461 ns | 32.03 ns | 33.33 ns | 33.22 ns |  0.59 |    0.01 |     242 B |         - |          NA |
