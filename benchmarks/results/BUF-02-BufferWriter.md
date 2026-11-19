# BUF-02: IBufferWriter (PooledBufferWriter)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the pooled writer is 0.58x and ArrayBufferWriter 0.65x of MemoryStream on net11 (0.58x / 0.69x on net10); allocation identical

### BufferWriterBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| MemoryStreamWrite | 224.3 ns | 225.1 ns | 1.00 | 2976 B → 2976 B | 3,740 B → 4,378 B |
| ArrayBufferWriterWrite | 154.2 ns | 146.4 ns | 0.95 | 1864 B → 1864 B | 929 B → 1,101 B |
| PooledBufferWriterWrite | 130.0 ns | 130.4 ns | 1.00 | 32 B → 32 B | 7,222 B → 5,597 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.13 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| MemoryStreamWrite       | .NET 10.0           | .NET 10.0 | 224.3 ns | 1.59 ns | 2.32 ns | 219.9 ns | 229.6 ns | 227.1 ns |  1.00 |    0.00 |   3,740 B | 0.3557 | 0.0021 |    2976 B |        1.00 |
| ArrayBufferWriterWrite  | .NET 10.0           | .NET 10.0 | 154.2 ns | 2.42 ns | 3.40 ns | 147.9 ns | 161.3 ns | 158.5 ns |  0.69 |    0.02 |     929 B | 0.2227 | 0.0002 |    1864 B |        0.63 |
| PooledBufferWriterWrite | .NET 10.0           | .NET 10.0 | 130.0 ns | 0.57 ns | 0.80 ns | 128.4 ns | 131.3 ns | 131.1 ns |  0.58 |    0.01 |   7,222 B | 0.0038 |      - |      32 B |        0.01 |
|                         |                     |           |          |         |         |          |          |          |       |         |           |        |        |           |             |
| MemoryStreamWrite       | MediumRun-.NET 11.0 | .NET 11.0 | 225.1 ns | 1.56 ns | 2.23 ns | 221.5 ns | 231.9 ns | 227.4 ns |  1.00 |    0.00 |   4,378 B | 0.3557 | 0.0017 |    2976 B |        1.00 |
| ArrayBufferWriterWrite  | MediumRun-.NET 11.0 | .NET 11.0 | 146.4 ns | 2.79 ns | 4.17 ns | 139.3 ns | 156.5 ns | 151.8 ns |  0.65 |    0.02 |   1,101 B | 0.2227 |      - |    1864 B |        0.63 |
| PooledBufferWriterWrite | MediumRun-.NET 11.0 | .NET 11.0 | 130.4 ns | 1.04 ns | 1.46 ns | 128.0 ns | 135.5 ns | 131.7 ns |  0.58 |    0.01 |   5,597 B | 0.0038 |      - |      32 B |        0.01 |

## Earlier record

- Verdict: adopted (implemented)
- PooledBufferWriter 0.57x / ArrayBufferWriter 0.68x vs MemoryStream + ToArray - the pooled writer is the fastest of the three
- PooledBufferWriter allocation: 2,976 B -> 32 B (writer instance only)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| MemoryStreamWrite       | 212.8 ns | 1.63 ns | 2.18 ns | 206.7 ns | 218.6 ns | 214.7 ns |  1.00 |    0.01 |   2,364 B | 0.3557 | 0.0007 |    2976 B |        1.00 |
| ArrayBufferWriterWrite  | 143.8 ns | 3.03 ns | 4.53 ns | 130.8 ns | 149.4 ns | 148.2 ns |  0.68 |    0.02 |     918 B | 0.2227 |      - |    1864 B |        0.63 |
| PooledBufferWriterWrite | 121.4 ns | 1.12 ns | 1.61 ns | 118.8 ns | 124.8 ns | 123.6 ns |  0.57 |    0.01 |   4,909 B | 0.0038 |      - |      32 B |        0.01 |
