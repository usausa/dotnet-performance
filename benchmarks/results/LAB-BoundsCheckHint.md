# LAB: Tail-touch bounds check hint (rejected, R-15)

- Verdict: rejected
- No difference across all variants on net10 and net8 (1024-int sum)
- array.Length loop form has the smallest code (34 B vs 94-140 B)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 8.0  : .NET 8.0.29 (8.0.29, 8.0.2926.32403), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SumByLength          | MediumRun-.NET 10.0 | .NET 10.0 | 212.1 ns | 4.85 ns | 7.26 ns | 207.8 ns | 233.7 ns | 226.2 ns |  1.00 |    0.05 |      96 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 10.0 | .NET 10.0 | 211.4 ns | 2.75 ns | 3.86 ns | 208.6 ns | 226.4 ns | 214.2 ns |  1.00 |    0.04 |      34 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 10.0 | .NET 10.0 | 209.6 ns | 0.61 ns | 0.88 ns | 208.6 ns | 212.3 ns | 211.0 ns |  0.99 |    0.03 |      94 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 10.0 | .NET 10.0 | 209.4 ns | 0.33 ns | 0.45 ns | 208.8 ns | 210.5 ns | 210.0 ns |  0.99 |    0.03 |     133 B |         - |          NA |
|                      |                     |           |          |         |         |          |          |          |       |         |           |           |             |
| SumByLength          | MediumRun-.NET 8.0  | .NET 8.0  | 209.0 ns | 0.64 ns | 0.85 ns | 207.7 ns | 212.0 ns | 209.8 ns |  1.00 |    0.01 |      99 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 8.0  | .NET 8.0  | 209.8 ns | 1.68 ns | 2.36 ns | 207.6 ns | 219.0 ns | 211.9 ns |  1.00 |    0.01 |      39 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 8.0  | .NET 8.0  | 208.7 ns | 0.43 ns | 0.63 ns | 207.1 ns | 209.7 ns | 209.5 ns |  1.00 |    0.00 |      98 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 8.0  | .NET 8.0  | 208.6 ns | 0.46 ns | 0.64 ns | 207.2 ns | 210.0 ns | 209.3 ns |  1.00 |    0.00 |     140 B |         - |          NA |
