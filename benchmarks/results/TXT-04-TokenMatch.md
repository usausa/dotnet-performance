# TXT-04: Byte token matching without string allocation

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - both span forms got faster on net11; the uint constant compare is 0.27x and the SequenceEqual chain 0.28x of the string switch (2,048 B → 0 B)

### TokenMatchBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| StringSwitch | 323.54 ns | 306.32 ns | 0.95 | 2048 B → 2048 B | 2,245 B → 1,812 B |
| SequenceEqualChain | 95.31 ns | 84.98 ns | 0.89 | - → - | 226 B → 226 B |
| UIntConstantCompare | 107.16 ns | 83.04 ns | 0.77 | - → - | 166 B → 166 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.63 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean      | Error    | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |----------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| StringSwitch        | .NET 10.0           | .NET 10.0 | 323.54 ns | 3.281 ns |  4.809 ns | 315.76 ns | 333.47 ns | 329.40 ns |  1.00 |    0.00 | 0.2446 |   2,245 B |    2048 B |        1.00 |
| SequenceEqualChain  | .NET 10.0           | .NET 10.0 |  95.31 ns | 7.262 ns | 10.644 ns |  83.12 ns | 108.11 ns | 107.38 ns |  0.29 |    0.03 |      - |     226 B |         - |        0.00 |
| UIntConstantCompare | .NET 10.0           | .NET 10.0 | 107.16 ns | 2.065 ns |  2.895 ns | 101.66 ns | 112.42 ns | 110.96 ns |  0.33 |    0.01 |      - |     166 B |         - |        0.00 |
|                     |                     |           |           |          |           |           |           |           |       |         |        |           |           |             |
| StringSwitch        | MediumRun-.NET 11.0 | .NET 11.0 | 306.32 ns | 1.324 ns |  1.856 ns | 303.06 ns | 310.77 ns | 308.80 ns |  1.00 |    0.00 | 0.2446 |   1,812 B |    2048 B |        1.00 |
| SequenceEqualChain  | MediumRun-.NET 11.0 | .NET 11.0 |  84.98 ns | 0.938 ns |  1.345 ns |  82.09 ns |  87.80 ns |  86.58 ns |  0.28 |    0.00 |      - |     226 B |         - |        0.00 |
| UIntConstantCompare | MediumRun-.NET 11.0 | .NET 11.0 |  83.04 ns | 0.865 ns |  1.213 ns |  80.92 ns |  86.28 ns |  84.35 ns |  0.27 |    0.00 |      - |     166 B |         - |        0.00 |

## Earlier record

- Verdict: adopted
- 0.26x vs string switch, 2,048 B -> 0 B
- SequenceEqual(u8) chain and uint constant compare are equal in time (84.1 vs 82.6 ns, CIs overlap)
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
