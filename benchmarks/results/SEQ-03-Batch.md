# SEQ-03 / STK-03: Batch with struct enumerators

- Verdict: adopted
- 1024 ints in chunks of 100: LINQ Chunk 570 ns + 4,424 B (allocates every chunk array) vs ArrayBatch (ArraySegment) 444 ns / 0 B (0.80x) vs SpanBatch 342 ns / 0 B (0.61x)
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
