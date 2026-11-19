# TXT-03: Try pattern vs exception control flow (10% invalid input)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the exception path got faster on net11 (128.6 → 94.2 ns per op, 46 → 35 B), and TryParse is still 0.03x of it

### TryPatternBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ExceptionControlFlow | 128.626 ns | 94.163 ns | 0.73 | 46 B → 35 B | 9,445 B → 15,299 B |
| TryPattern | 3.115 ns | 2.896 ns | 0.93 | - → - | 2,967 B → 3,078 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.4 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|----------:|-------:|----------:|------------:|
| ExceptionControlFlow | .NET 10.0           | .NET 10.0 | 128.626 ns | 0.6508 ns | 0.8908 ns | 127.168 ns | 130.962 ns | 129.719 ns |  1.00 |   9,445 B | 0.0053 |      46 B |        1.00 |
| TryPattern           | .NET 10.0           | .NET 10.0 |   3.115 ns | 0.0151 ns | 0.0226 ns |   3.077 ns |   3.164 ns |   3.138 ns |  0.02 |   2,967 B |      - |         - |        0.00 |
|                      |                     |           |            |           |           |            |            |            |       |           |        |           |             |
| ExceptionControlFlow | MediumRun-.NET 11.0 | .NET 11.0 |  94.163 ns | 0.5669 ns | 0.7947 ns |  92.955 ns |  95.395 ns |  95.203 ns |  1.00 |  15,299 B | 0.0041 |      35 B |        1.00 |
| TryPattern           | MediumRun-.NET 11.0 | .NET 11.0 |   2.896 ns | 0.0126 ns | 0.0188 ns |   2.868 ns |   2.946 ns |   2.915 ns |  0.03 |   3,078 B |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted
- Exception flow 132.5 ns/op + 48 B vs TryParse 2.89 ns/op / 0 B (0.02x = ~46x); one thrown exception costs ~1.3 us
- Code size 8,348 B vs 1,705 B (EH scaffolding)

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
