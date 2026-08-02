# BUF-07: ObjectPool (ThreadStatic single-slot)

- Verdict: adopted
- 0.68x vs new StringBuilder per call; allocation 648 B -> 64 B (result string only, 0.10x)
- Retained-capacity cap prevents pool bloat; single-slot ThreadStatic keeps thread safety trivially

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| NewEveryTime     | 40.87 ns | 1.993 ns | 2.922 ns | 34.54 ns | 48.27 ns | 44.12 ns |  1.00 |    0.10 | 0.0387 |   2,338 B | 0.0001 |     648 B |        1.00 |
| ThreadStaticPool | 27.83 ns | 1.436 ns | 2.149 ns | 23.28 ns | 32.63 ns | 30.37 ns |  0.68 |    0.07 | 0.0038 |   3,833 B |      - |      64 B |        0.10 |
