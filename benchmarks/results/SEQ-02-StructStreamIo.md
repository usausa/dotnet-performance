# SEQ-02: Struct stream I/O (1024 x 16-byte records)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the bulk cast write got faster on net11 (154.1 → 121.0 ns) and is 0.012x of field-by-field; the read is 0.021x

### StructStreamIoBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| WriteFieldByField | 10,605.65 ns | 10,240.63 ns | 0.97 | 104 B → 104 B | 784 B → 768 B |
| WriteBulkCast | 154.11 ns | 120.95 ns | 0.78 | 64 B → 64 B | 2,464 B → 3,189 B |
| ReadFieldByField | 4,711.39 ns | 4,542.15 ns | 0.96 | 120 B → 120 B | 1,327 B → 1,165 B |
| ReadBulkCast | 95.43 ns | 95.23 ns | 1.00 | 64 B → 64 B | 983 B → 959 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.64 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean         | Error      | StdDev     | Min          | Max          | P90          | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|----------:|-------:|----------:|------------:|
| WriteFieldByField | .NET 10.0           | .NET 10.0 | 10,605.65 ns | 244.688 ns | 334.931 ns | 10,072.45 ns | 11,548.77 ns | 10,979.14 ns | 1.000 |     784 B |      - |     104 B |        1.00 |
| WriteBulkCast     | .NET 10.0           | .NET 10.0 |    154.11 ns |   0.603 ns |   0.884 ns |    152.09 ns |    155.97 ns |    155.08 ns | 0.015 |   2,464 B | 0.0076 |      64 B |        0.62 |
| ReadFieldByField  | .NET 10.0           | .NET 10.0 |  4,711.39 ns |  35.205 ns |  51.604 ns |  4,651.50 ns |  4,873.48 ns |  4,774.59 ns | 0.445 |   1,327 B | 0.0076 |     120 B |        1.15 |
| ReadBulkCast      | .NET 10.0           | .NET 10.0 |     95.43 ns |   0.439 ns |   0.657 ns |     94.31 ns |     96.62 ns |     96.33 ns | 0.009 |     983 B | 0.0076 |      64 B |        0.62 |
|                   |                     |           |              |            |            |              |              |              |       |           |        |           |             |
| WriteFieldByField | MediumRun-.NET 11.0 | .NET 11.0 | 10,240.63 ns | 114.313 ns | 167.559 ns |  9,993.59 ns | 10,542.30 ns | 10,440.95 ns | 1.000 |     768 B |      - |     104 B |        1.00 |
| WriteBulkCast     | MediumRun-.NET 11.0 | .NET 11.0 |    120.95 ns |   0.448 ns |   0.657 ns |    119.84 ns |    122.15 ns |    121.83 ns | 0.012 |   3,189 B | 0.0076 |      64 B |        0.62 |
| ReadFieldByField  | MediumRun-.NET 11.0 | .NET 11.0 |  4,542.15 ns |  32.028 ns |  44.899 ns |  4,463.86 ns |  4,607.20 ns |  4,593.82 ns | 0.444 |   1,165 B | 0.0076 |     120 B |        1.15 |
| ReadBulkCast      | MediumRun-.NET 11.0 | .NET 11.0 |     95.23 ns |   0.702 ns |   1.051 ns |     93.64 ns |     96.89 ns |     96.60 ns | 0.009 |     959 B | 0.0076 |      64 B |        0.62 |

## Earlier record

- Verdict: adopted (largest win in the catalog)
- Write: field-by-field BinaryWriter 10,199 ns -> bulk MemoryMarshal.AsBytes 154.0 ns (0.015x = ~66x)
- Read: field-by-field BinaryReader 4,609 ns -> bulk ReadExactly 93.8 ns (0.020x = ~49x)
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
