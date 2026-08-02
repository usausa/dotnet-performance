# SEQ-03: Struct stream I/O (1024 x 16-byte records)

- Verdict: adopted (largest win in the catalog)
- Write: field-by-field BinaryWriter 18,294 ns -> bulk MemoryMarshal.AsBytes 181.6 ns (0.010x = ~100x)
- Read: field-by-field BinaryReader 8,172 ns -> bulk ReadExactly 193.8 ns (0.024x = ~42x)
- Requires Pack=1 (or verified padding-free layout) and fixed endianness; see pattern notes

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|-------:|----------:|----------:|------------:|
| WriteFieldByField | 18,294.3 ns | 688.41 ns | 942.30 ns | 16,334.2 ns | 20,867.3 ns | 19,130.9 ns | 1.003 |    0.07 |      - |     748 B |     104 B |        1.00 |
| WriteBulkCast     |    181.6 ns |   7.27 ns |   9.96 ns |    172.5 ns |    215.9 ns |    193.0 ns | 0.010 |    0.00 | 0.0038 |   1,031 B |      64 B |        0.62 |
| ReadFieldByField  |  8,171.6 ns | 606.04 ns | 907.09 ns |  7,019.4 ns |  9,907.7 ns |  9,381.9 ns | 0.448 |    0.05 |      - |   1,033 B |     120 B |        1.15 |
| ReadBulkCast      |    193.8 ns |  15.59 ns |  21.86 ns |    168.2 ns |    232.6 ns |    223.7 ns | 0.011 |    0.00 | 0.0038 |   1,002 B |      64 B |        0.62 |
