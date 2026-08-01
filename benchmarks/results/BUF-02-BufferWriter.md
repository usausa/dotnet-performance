# BUF-02: IBufferWriter (PooledBufferWriter)

- Verdict: adopted (implemented)
- ArrayBufferWriter 0.56x / PooledBufferWriter 0.63x vs MemoryStream + ToArray
- PooledBufferWriter allocation: 2,976 B -> 32 B (writer instance only)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| MemoryStreamWrite       | 368.6 ns | 11.69 ns | 17.13 ns | 350.1 ns | 422.3 ns | 395.3 ns |  1.00 |    0.06 | 0.1779 |   2,376 B |    2976 B |        1.00 |
| ArrayBufferWriterWrite  | 206.5 ns |  5.91 ns |  8.85 ns | 186.4 ns | 225.5 ns | 216.2 ns |  0.56 |    0.03 | 0.1113 |     953 B |    1864 B |        0.63 |
| PooledBufferWriterWrite | 233.0 ns |  1.42 ns |  2.08 ns | 228.5 ns | 236.9 ns | 235.2 ns |  0.63 |    0.03 | 0.0019 |   4,924 B |      32 B |        0.01 |
