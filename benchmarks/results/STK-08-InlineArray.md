# STK-08: InlineArray vs stackalloc vs new array

- Verdict: adopted
- InlineArray 0.69x / stackalloc 0.66x vs new int[8], both zero-alloc (heap array: 56 B)
- InlineArray == stackalloc in speed; choose InlineArray when the buffer must live inside a struct field

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| NewArray          | 7.180 ns | 0.1510 ns | 0.2117 ns | 6.796 ns | 7.830 ns | 7.377 ns |  1.00 |    0.04 | 0.0033 |     113 B |      56 B |        1.00 |
| Stackalloc        | 4.708 ns | 0.0812 ns | 0.1215 ns | 4.567 ns | 5.037 ns | 4.891 ns |  0.66 |    0.02 |      - |     134 B |         - |        0.00 |
| InlineArrayBuffer | 4.932 ns | 0.0465 ns | 0.0652 ns | 4.820 ns | 5.060 ns | 5.001 ns |  0.69 |    0.02 |      - |     112 B |         - |        0.00 |
