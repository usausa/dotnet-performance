# SEQ-03 / STK-03: Batch with struct enumerators

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - ArrayBatch 0.76x and SpanBatch 0.60x of LINQ Chunk on net11; Chunk still allocates 4,424 B

### BatchBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| LinqChunk | 353.2 ns | 351.7 ns | 1.00 | 4424 B → 4424 B | 1,886 B → 1,920 B |
| ArrayBatch | 263.1 ns | 266.1 ns | 1.01 | - → - | 141 B → 141 B |
| SpanBatch | 212.4 ns | 211.6 ns | 1.00 | - → - | 110 B → 110 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.74 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method     | Job                 | Runtime   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------- |-------------------- |---------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| LinqChunk  | .NET 10.0           | .NET 10.0 | 353.2 ns | 1.35 ns |  1.94 ns | 350.3 ns | 357.3 ns | 356.1 ns |  1.00 |    0.00 |   1,886 B | 0.5288 | 0.0005 |    4424 B |        1.00 |
| ArrayBatch | .NET 10.0           | .NET 10.0 | 263.1 ns | 7.59 ns | 11.13 ns | 216.0 ns | 270.5 ns | 269.0 ns |  0.75 |    0.03 |     141 B |      - |      - |         - |        0.00 |
| SpanBatch  | .NET 10.0           | .NET 10.0 | 212.4 ns | 1.14 ns |  1.60 ns | 209.9 ns | 217.7 ns | 214.1 ns |  0.60 |    0.01 |     110 B |      - |      - |         - |        0.00 |
|            |                     |           |          |         |          |          |          |          |       |         |           |        |        |           |             |
| LinqChunk  | MediumRun-.NET 11.0 | .NET 11.0 | 351.7 ns | 0.99 ns |  1.35 ns | 348.8 ns | 354.4 ns | 353.4 ns |  1.00 |    0.00 |   1,920 B | 0.5288 | 0.0010 |    4424 B |        1.00 |
| ArrayBatch | MediumRun-.NET 11.0 | .NET 11.0 | 266.1 ns | 0.84 ns |  1.20 ns | 262.8 ns | 269.2 ns | 267.1 ns |  0.76 |    0.00 |     141 B |      - |      - |         - |        0.00 |
| SpanBatch  | MediumRun-.NET 11.0 | .NET 11.0 | 211.6 ns | 1.21 ns |  1.69 ns | 209.8 ns | 216.7 ns | 213.8 ns |  0.60 |    0.01 |     110 B |      - |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted
- 1024 ints in chunks of 100: LINQ Chunk 359 ns + 4,424 B (allocates every chunk array) vs ArrayBatch (ArraySegment) 266 ns / 0 B (0.74x) vs SpanBatch 227 ns / 0 B (0.63x)
- Code size 1,769 B -> 141 / 108 B; struct enumerator (STK-03) + slicing removes both the per-chunk copy and the iterator allocation

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method     | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|----------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| LinqChunk  | 359.4 ns | 3.36 ns | 4.60 ns | 346.8 ns | 367.0 ns | 364.2 ns |  1.00 |    0.02 | 0.5288 |   1,769 B | 0.0010 |    4424 B |        1.00 |
| ArrayBatch | 265.7 ns | 5.63 ns | 8.25 ns | 248.8 ns | 285.1 ns | 275.8 ns |  0.74 |    0.02 |      - |     141 B |      - |         - |        0.00 |
| SpanBatch  | 226.7 ns | 5.20 ns | 7.78 ns | 210.7 ns | 243.0 ns | 234.3 ns |  0.63 |    0.02 |      - |     108 B |      - |         - |        0.00 |
