# TXT-04: Byte token matching without string allocation

- Verdict: adopted
- 0.23x vs string switch, 2,048 B -> 0 B
- SequenceEqual(u8) chain and uint constant compare are equal in time (115.4 vs 115.2 ns)
- uint constants only reduce code size (226 B -> 166 B); prefer SequenceEqual by default

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|-------:|----------:|----------:|------------:|
| StringSwitch        | 314.05 ns | 1.615 ns | 2.316 ns | 309.14 ns | 318.02 ns | 316.62 ns |  1.00 | 0.2446 |   1,395 B |    2048 B |        1.00 |
| SequenceEqualChain  |  84.14 ns | 0.791 ns | 1.135 ns |  81.62 ns |  87.14 ns |  85.44 ns |  0.27 |      - |     226 B |         - |        0.00 |
| UIntConstantCompare |  82.60 ns | 1.303 ns | 1.951 ns |  80.05 ns |  87.02 ns |  85.01 ns |  0.26 |      - |     166 B |         - |        0.00 |
