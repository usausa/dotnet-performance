# SEQ-01: SpanReader / SpanWriter

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 8.0  : .NET 8.0.29 (8.0.29, 8.0.2926.32403), X64 RyuJIT x86-64-v3
  MediumRun-.NET 9.0  : .NET 9.0.18 (9.0.18, 9.0.1826.31522), X64 RyuJIT x86-64-v3

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean      | Error    | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |----------:|---------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| BinaryReaderParse | MediumRun-.NET 10.0 | .NET 10.0 | 134.33 ns | 3.707 ns |  5.549 ns | 134.51 ns | 121.08 ns | 143.67 ns | 140.90 ns |  1.00 |    0.06 | 0.0072 |   1,500 B |     120 B |        1.00 |
| ManualOffsetParse | MediumRun-.NET 10.0 | .NET 10.0 |  17.33 ns | 0.441 ns |  0.660 ns |  17.32 ns |  15.72 ns |  18.49 ns |  18.10 ns |  0.13 |    0.01 |      - |     125 B |         - |        0.00 |
| SpanReaderParse   | MediumRun-.NET 10.0 | .NET 10.0 |  13.59 ns | 1.369 ns |  2.006 ns |  13.97 ns |  11.27 ns |  16.78 ns |  15.79 ns |  0.10 |    0.02 |      - |     125 B |         - |        0.00 |
|                   |                     |           |           |          |           |           |           |           |           |       |         |        |           |           |             |
| BinaryReaderParse | MediumRun-.NET 8.0  | .NET 8.0  | 105.90 ns | 3.619 ns |  5.417 ns | 106.04 ns |  97.66 ns | 116.09 ns | 112.41 ns |  1.00 |    0.07 | 0.0134 |   2,330 B |     224 B |        1.00 |
| ManualOffsetParse | MediumRun-.NET 8.0  | .NET 8.0  |  15.29 ns | 1.018 ns |  1.428 ns |  14.85 ns |  14.04 ns |  18.41 ns |  17.81 ns |  0.14 |    0.02 |      - |     134 B |         - |        0.00 |
| SpanReaderParse   | MediumRun-.NET 8.0  | .NET 8.0  |  13.64 ns | 0.864 ns |  1.293 ns |  13.16 ns |  12.44 ns |  16.75 ns |  15.99 ns |  0.13 |    0.01 |      - |     138 B |         - |        0.00 |
|                   |                     |           |           |          |           |           |           |           |           |       |         |        |           |           |             |
| BinaryReaderParse | MediumRun-.NET 9.0  | .NET 9.0  | 101.14 ns | 9.024 ns | 13.507 ns |  95.30 ns |  90.70 ns | 142.60 ns | 120.47 ns |  1.01 |    0.17 | 0.0072 |   1,961 B |     120 B |        1.00 |
| ManualOffsetParse | MediumRun-.NET 9.0  | .NET 9.0  |  14.21 ns | 0.207 ns |  0.304 ns |  14.20 ns |  13.53 ns |  14.94 ns |  14.60 ns |  0.14 |    0.02 |      - |     122 B |         - |        0.00 |
| SpanReaderParse   | MediumRun-.NET 9.0  | .NET 9.0  |  13.35 ns | 1.382 ns |  2.068 ns |  12.14 ns |  11.37 ns |  17.35 ns |  16.63 ns |  0.13 |    0.03 |      - |     125 B |         - |        0.00 |
