# TXT-03: Try pattern vs exception control flow (10% invalid input)

- Verdict: adopted
- Exception flow 183.8 ns/op + 48 B vs TryParse 4.67 ns/op / 0 B (0.03x = ~39x); one thrown exception costs ~1.8 us
- Code size 8,117 B vs 1,712 B (EH scaffolding)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|-------:|----------:|------------:|
| ExceptionControlFlow | 183.782 ns | 2.5812 ns | 3.8634 ns | 177.791 ns | 191.346 ns | 187.834 ns |  1.00 |    0.03 |   8,117 B | 0.0027 |      48 B |        1.00 |
| TryPattern           |   4.669 ns | 0.0894 ns | 0.1311 ns |   4.497 ns |   4.960 ns |   4.833 ns |  0.03 |    0.00 |   1,712 B |      - |         - |        0.00 |
