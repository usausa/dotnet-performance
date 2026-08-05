# SEQ-02: Struct stream I/O (1024 x 16-byte records)

- Verdict: adopted (largest win in the catalog)
- Write: field-by-field BinaryWriter 18,294 ns -> bulk MemoryMarshal.AsBytes 181.6 ns (0.010x = ~100x)
- Read: field-by-field BinaryReader 8,172 ns -> bulk ReadExactly 193.8 ns (0.024x = ~42x)
- Requires Pack=1 (or verified padding-free layout) and fixed endianness; see pattern notes

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean         | Error      | StdDev     | Min         | Max          | P90          | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|------------:|-------------:|-------------:|------:|--------:|----------:|-------:|----------:|------------:|
| WriteFieldByField | 10,199.03 ns | 566.318 ns | 812.197 ns | 9,666.89 ns | 12,322.40 ns | 11,917.74 ns | 1.005 |    0.10 |     748 B |      - |     104 B |        1.00 |
| WriteBulkCast     |    153.97 ns |   0.631 ns |   0.945 ns |   151.38 ns |    155.02 ns |    154.75 ns | 0.015 |    0.00 |   1,019 B | 0.0076 |      64 B |        0.62 |
| ReadFieldByField  |  4,608.58 ns |  15.695 ns |  22.509 ns | 4,561.11 ns |  4,651.52 ns |  4,635.91 ns | 0.454 |    0.03 |   1,033 B | 0.0076 |     120 B |        1.15 |
| ReadBulkCast      |     93.81 ns |   0.770 ns |   1.079 ns |    92.18 ns |     95.89 ns |     95.19 ns | 0.009 |    0.00 |     997 B | 0.0076 |      64 B |        0.62 |
