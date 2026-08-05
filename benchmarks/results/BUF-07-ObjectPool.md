# BUF-07: ObjectPool (ThreadStatic single-slot)

- Verdict: adopted
- 0.68x vs new StringBuilder per call (19.97 -> 13.51 ns); allocation 648 B -> 64 B (result string only, 0.10x)
- Retained-capacity cap prevents pool bloat; single-slot ThreadStatic keeps thread safety trivially

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| NewEveryTime     | 19.97 ns | 0.516 ns | 0.706 ns | 18.83 ns | 21.43 ns | 21.07 ns |  1.00 |    0.05 | 0.0775 |   2,326 B | 0.0001 |     648 B |        1.00 |
| ThreadStaticPool | 13.51 ns | 0.142 ns | 0.194 ns | 13.30 ns | 13.94 ns | 13.78 ns |  0.68 |    0.02 | 0.0076 |   3,817 B |      - |      64 B |        0.10 |
