# BUF-02: IBufferWriter (PooledBufferWriter)

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
