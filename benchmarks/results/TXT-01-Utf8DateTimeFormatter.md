# TXT-01: Utf8DateTimeFormatter(ルックアップテーブル整形)

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
| Method          | Job                 | Runtime   | Mean     | Error     | StdDev    | Median   | Min      | Max       | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|----------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| ToStringEncode  | MediumRun-.NET 10.0 | .NET 10.0 | 62.84 ns |  8.248 ns | 12.346 ns | 57.42 ns | 50.71 ns |  83.65 ns | 81.09 ns |  1.04 |    0.27 |   9,573 B | 0.0033 |      56 B |        1.00 |
| TryFormatEncode | MediumRun-.NET 10.0 | .NET 10.0 | 67.04 ns |  8.084 ns | 12.099 ns | 71.21 ns | 43.76 ns |  80.58 ns | 78.65 ns |  1.10 |    0.28 |   8,832 B |      - |         - |        0.00 |
| TableFormat     | MediumRun-.NET 10.0 | .NET 10.0 | 19.25 ns |  1.047 ns |  1.566 ns | 18.72 ns | 17.43 ns |  23.14 ns | 21.78 ns |  0.32 |    0.06 |   1,364 B |      - |         - |        0.00 |
|                 |                     |           |          |           |           |          |          |           |          |       |         |           |        |           |             |
| ToStringEncode  | MediumRun-.NET 8.0  | .NET 8.0  | 64.40 ns |  1.666 ns |  2.389 ns | 64.02 ns | 60.40 ns |  70.71 ns | 67.04 ns |  1.00 |    0.05 |  10,246 B | 0.0033 |      56 B |        1.00 |
| TryFormatEncode | MediumRun-.NET 8.0  | .NET 8.0  | 67.02 ns | 13.006 ns | 19.064 ns | 54.61 ns | 50.26 ns | 105.63 ns | 92.01 ns |  1.04 |    0.29 |   9,988 B |      - |         - |        0.00 |
| TableFormat     | MediumRun-.NET 8.0  | .NET 8.0  | 14.98 ns |  1.472 ns |  2.203 ns | 14.39 ns | 12.42 ns |  18.17 ns | 17.63 ns |  0.23 |    0.03 |     893 B |      - |         - |        0.00 |
|                 |                     |           |          |           |           |          |          |           |          |       |         |           |        |           |             |
| ToStringEncode  | MediumRun-.NET 9.0  | .NET 9.0  | 72.28 ns |  8.127 ns | 12.164 ns | 69.94 ns | 58.02 ns |  91.26 ns | 86.49 ns |  1.03 |    0.24 |   9,519 B | 0.0033 |      56 B |        1.00 |
| TryFormatEncode | MediumRun-.NET 9.0  | .NET 9.0  | 71.98 ns |  4.679 ns |  7.004 ns | 74.23 ns | 51.77 ns |  80.75 ns | 78.57 ns |  1.02 |    0.19 |   9,257 B |      - |         - |        0.00 |
| TableFormat     | MediumRun-.NET 9.0  | .NET 9.0  | 16.01 ns |  0.954 ns |  1.427 ns | 16.51 ns | 12.07 ns |  17.49 ns | 17.17 ns |  0.23 |    0.04 |     875 B |      - |         - |        0.00 |
