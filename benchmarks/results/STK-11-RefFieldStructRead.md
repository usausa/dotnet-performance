# STK-11: ref field cursor for field-granular structured reads

- Verdict: adopted
- ParseRefFieldReader 0.75x against index arithmetic written at the call site, CIs do not overlap
  (761-836 vs 988-1095 ns), and the code is smaller (111 vs 163 B)
- The re-slicing cursor also wins without touching ref fields: ParseSliceReader 0.81x, 128 B
- This is the shape R-12 named in its "do this instead" line but never measured. R-12 remains correct for
  whole-element iteration (1.21x slower there); the conclusion inverts once each step reads a different width
- Record layout under test: [byte tag][ushort length][length bytes payload], 512 records

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean       | Error    | StdDev   | Min      | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |-----------:|---------:|---------:|---------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| ParseInlineIndex    | 1,038.8 ns | 29.79 ns | 44.60 ns | 988.2 ns | 1,095.0 ns | 1,091.9 ns |  1.00 |    0.06 |     163 B |         - |          NA |
| ParseSpanReader     |   994.0 ns | 28.66 ns | 42.90 ns | 942.2 ns | 1,055.6 ns | 1,043.4 ns |  0.96 |    0.06 |     153 B |         - |          NA |
| ParseSliceReader    |   842.3 ns | 18.44 ns | 25.85 ns | 823.2 ns |   902.3 ns |   891.3 ns |  0.81 |    0.04 |     128 B |         - |          NA |
| ParseRefFieldReader |   782.5 ns | 17.58 ns | 24.65 ns | 761.1 ns |   836.0 ns |   827.3 ns |  0.75 |    0.04 |     111 B |         - |          NA |

