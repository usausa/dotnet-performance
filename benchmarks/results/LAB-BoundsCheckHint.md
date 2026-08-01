# LAB: 末尾要素の事前タッチによる境界チェック誘導(反パターン判定)

判定: 反パターン表へ(net10 / net8 とも 1024 要素の合計ループで全バリアント差なし。array.Length 直接形がコードサイズ最小)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 8.0  : .NET 8.0.29 (8.0.29, 8.0.2926.32403), X64 RyuJIT x86-64-v3

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SumByLength          | MediumRun-.NET 10.0 | .NET 10.0 | 393.1 ns | 10.29 ns | 15.40 ns | 356.5 ns | 423.0 ns | 413.0 ns |  1.00 |    0.05 |      96 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 10.0 | .NET 10.0 | 391.2 ns | 10.25 ns | 15.35 ns | 355.5 ns | 415.1 ns | 407.4 ns |  1.00 |    0.05 |      34 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 10.0 | .NET 10.0 | 393.8 ns | 15.54 ns | 23.26 ns | 347.9 ns | 430.0 ns | 420.2 ns |  1.00 |    0.07 |      94 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 10.0 | .NET 10.0 | 389.3 ns | 15.13 ns | 22.64 ns | 308.9 ns | 422.1 ns | 412.5 ns |  0.99 |    0.07 |     133 B |         - |          NA |
|                      |                     |           |          |          |          |          |          |          |       |         |           |           |             |
| SumByLength          | MediumRun-.NET 8.0  | .NET 8.0  | 386.7 ns | 16.30 ns | 24.40 ns | 332.0 ns | 420.4 ns | 413.7 ns |  1.00 |    0.09 |      99 B |         - |          NA |
| SumByArrayLength     | MediumRun-.NET 8.0  | .NET 8.0  | 387.1 ns | 13.86 ns | 20.75 ns | 343.7 ns | 428.9 ns | 412.9 ns |  1.01 |    0.08 |      39 B |         - |          NA |
| SumWithTailTouch     | MediumRun-.NET 8.0  | .NET 8.0  | 389.7 ns | 12.57 ns | 18.82 ns | 357.5 ns | 424.5 ns | 411.8 ns |  1.01 |    0.08 |      98 B |         - |          NA |
| SumWithUnsignedGuard | MediumRun-.NET 8.0  | .NET 8.0  | 390.5 ns | 14.04 ns | 21.02 ns | 330.1 ns | 421.7 ns | 414.7 ns |  1.01 |    0.09 |     140 B |         - |          NA |
