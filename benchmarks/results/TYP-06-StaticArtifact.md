# TYP-06: Static pre-built artifacts per type

- Verdict: adopted
- Generic static field read: 0.09 ns / code size 6 B (effectively free, same as TYP-01 generic path)
- Dictionary<Type,string> cache: 4.8 ns (0.042x vs rebuild); rebuild every call: 116 ns + 760 B
- Static generic beats dictionary cache by ~53x; use dictionary only when the type is not known statically

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| BuildEveryCall     | 57.0466 ns | 1.0539 ns | 1.4426 ns | 56.8813 ns | 52.4937 ns | 60.9476 ns | 58.4941 ns | 1.001 |    0.04 | 0.0908 |   5,220 B | 0.0002 |     760 B |        1.00 |
| DictionaryCache    |  2.7237 ns | 0.0114 ns | 0.0160 ns |  2.7223 ns |  2.6940 ns |  2.7587 ns |  2.7434 ns | 0.048 |    0.00 |      - |     936 B |      - |         - |        0.00 |
| StaticGenericField |  0.0010 ns | 0.0019 ns | 0.0026 ns |  0.0000 ns |  0.0000 ns |  0.0110 ns |  0.0031 ns | 0.000 |    0.00 |      - |       6 B |      - |         - |        0.00 |
