# STK-08: InlineArray vs stackalloc vs new array

- Verdict: adopted
- InlineArray 0.61x / stackalloc 0.60x vs new int[8], both zero-alloc (heap array: 56 B)
- InlineArray and stackalloc are equal in time (2.92 vs 2.87 ns, CIs overlap); InlineArray's code is slightly smaller (112 vs 134 B)
- InlineArray's unique value remains that the buffer can live inside a struct field

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| NewArray          | 4.808 ns | 0.0581 ns | 0.0775 ns | 4.679 ns | 4.974 ns | 4.893 ns |  1.00 |    0.02 | 0.0067 |     113 B |      56 B |        1.00 |
| Stackalloc        | 2.872 ns | 0.0452 ns | 0.0649 ns | 2.778 ns | 3.045 ns | 2.953 ns |  0.60 |    0.02 |      - |     134 B |         - |        0.00 |
| InlineArrayBuffer | 2.916 ns | 0.0844 ns | 0.1126 ns | 2.816 ns | 3.306 ns | 2.967 ns |  0.61 |    0.02 |      - |     112 B |         - |        0.00 |
