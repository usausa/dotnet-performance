# MEM-03: Slice(offset, length) vs range operator

- Verdict: adopted (small but real)
- 122.8 vs 136.8 ns for 256 slices (0.90x, non-overlapping CIs); code size 100 vs 103 B
- ~0.05 ns per slice: only worth writing deliberately in hot loops; elsewhere pick for readability

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SliceMethod   | 106.6 ns | 0.35 ns | 0.50 ns | 105.6 ns | 107.5 ns | 107.3 ns |  1.00 |    0.01 |     100 B |         - |          NA |
| RangeOperator | 107.0 ns | 3.37 ns | 4.84 ns | 104.3 ns | 125.6 ns | 108.3 ns |  1.00 |    0.04 |     103 B |         - |          NA |
