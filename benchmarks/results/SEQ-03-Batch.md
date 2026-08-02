# SEQ-03 / STK-03: Batch with struct enumerators

- Verdict: adopted
- 1024 ints in chunks of 100: LINQ Chunk 570 ns + 4,424 B (allocates every chunk array) vs ArrayBatch (ArraySegment) 444 ns / 0 B (0.80x) vs SpanBatch 342 ns / 0 B (0.61x)
- Code size 1,769 B -> 141 / 108 B; struct enumerator (STK-03) + slicing removes both the per-chunk copy and the iterator allocation

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method     | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| LinqChunk  | 570.2 ns | 61.87 ns | 90.68 ns | 522.1 ns | 479.7 ns | 748.5 ns | 718.0 ns |  1.02 |    0.21 | 0.2642 |   1,769 B |    4424 B |        1.00 |
| ArrayBatch | 444.4 ns | 21.47 ns | 32.14 ns | 451.3 ns | 376.5 ns | 490.7 ns | 481.9 ns |  0.80 |    0.12 |      - |     141 B |         - |        0.00 |
| SpanBatch  | 341.5 ns | 39.78 ns | 58.32 ns | 340.7 ns | 258.4 ns | 442.4 ns | 419.0 ns |  0.61 |    0.13 |      - |     108 B |         - |        0.00 |
