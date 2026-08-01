# LAB: ref フィールドカーソル(反パターン判定)

判定: 反復用途は反パターン(Span+index の for 比 1.21 倍。SpanReader の要素単位 Read は 2.06 倍 — カーソルは構造読み専用)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SumSpanIndex      | 249.0 ns |  1.85 ns |  2.60 ns | 244.4 ns | 255.4 ns | 251.7 ns |  1.00 |    0.01 |      53 B |         - |          NA |
| SumSpanReader     | 513.0 ns | 35.62 ns | 53.31 ns | 452.3 ns | 618.5 ns | 578.7 ns |  2.06 |    0.21 |      78 B |         - |          NA |
| SumRefFieldCursor | 302.5 ns | 16.15 ns | 23.68 ns | 276.4 ns | 359.1 ns | 329.8 ns |  1.21 |    0.09 |      56 B |         - |          NA |
