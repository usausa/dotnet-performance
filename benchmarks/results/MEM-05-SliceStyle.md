# MEM-05: Slice(offset, length) vs range operator

- Verdict: adopted (small but real)
- 122.8 vs 136.8 ns for 256 slices (0.90x, non-overlapping CIs); code size 100 vs 103 B
- ~0.05 ns per slice: only worth writing deliberately in hot loops; elsewhere pick for readability

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SliceMethod   | 122.8 ns | 1.44 ns | 2.11 ns | 118.9 ns | 125.8 ns | 125.4 ns |  1.00 |    0.02 |     100 B |         - |          NA |
| RangeOperator | 136.8 ns | 2.74 ns | 4.02 ns | 132.4 ns | 147.0 ns | 142.6 ns |  1.11 |    0.04 |     103 B |         - |          NA |
