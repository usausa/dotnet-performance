# TXT-03: Try pattern vs exception control flow (10% invalid input)

- Verdict: adopted
- Exception flow 183.8 ns/op + 48 B vs TryParse 4.67 ns/op / 0 B (0.03x = ~39x); one thrown exception costs ~1.8 us
- Code size 8,117 B vs 1,712 B (EH scaffolding)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|----------:|------------:|
| ExceptionControlFlow | 132.468 ns | 1.4728 ns | 2.0646 ns | 129.955 ns | 137.959 ns | 135.131 ns |  1.00 |    0.02 | 0.0056 |   8,348 B |      48 B |        1.00 |
| TryPattern           |   2.891 ns | 0.0521 ns | 0.0731 ns |   2.834 ns |   3.180 ns |   2.931 ns |  0.02 |    0.00 |      - |   1,705 B |         - |        0.00 |
