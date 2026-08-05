# TXT-01: Utf8DateTimeFormatter (lookup table)

- Verdict: adopted (implemented)
- 0.41x vs ToString + Encoding.GetBytes, 56 B -> 0 B
- Code size ~10 KB -> 0.9 KB
- The margin narrowed from 0.32x on the previous Ryzen 9 5900X baseline: the BCL formatting path gained more from the newer core than the table walk did. The allocation and code-size wins are unchanged
- DateTime.TryFormat + encode is now a real time win over ToString as well (0.90x, was 1.10x), so the allocation-only framing no longer holds - but the table is still 2.2x ahead of it

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| ToStringEncode  | 30.53 ns | 0.329 ns | 0.439 ns | 30.13 ns | 32.11 ns | 30.69 ns |  1.00 |    0.02 | 0.0067 |   9,716 B |      56 B |        1.00 |
| TryFormatEncode | 27.35 ns | 0.222 ns | 0.311 ns | 26.71 ns | 27.95 ns | 27.74 ns |  0.90 |    0.02 |      - |   9,149 B |         - |        0.00 |
| TableFormat     | 12.38 ns | 0.024 ns | 0.035 ns | 12.29 ns | 12.46 ns | 12.43 ns |  0.41 |    0.01 |      - |   1,348 B |         - |        0.00 |
