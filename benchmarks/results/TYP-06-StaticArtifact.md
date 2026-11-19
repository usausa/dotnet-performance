# TYP-06: Static pre-built artifacts per type

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. The Dictionary cache is 0.07x and the static generic field 0.01x of rebuilding on both generations

### StaticArtifactBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| BuildEveryCall | 57.5908 ns | 57.2018 ns | 0.99 | 760 B → 760 B | 6,071 B → 6,216 B |
| DictionaryCache | 4.1859 ns | 4.2088 ns | 1.01 | - → - | 260 B → 256 B |
| StaticGenericField | 0.7934 ns | 0.7923 ns | 1.00 | - → - | 6 B → 6 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.17 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|----------:|-------:|-------:|----------:|------------:|
| BuildEveryCall     | .NET 10.0           | .NET 10.0 | 57.5908 ns | 0.3594 ns | 0.5267 ns | 57.5241 ns | 56.7828 ns | 58.5404 ns | 58.2595 ns |  1.00 |   6,071 B | 0.0908 | 0.0002 |     760 B |        1.00 |
| DictionaryCache    | .NET 10.0           | .NET 10.0 |  4.1859 ns | 0.0308 ns | 0.0431 ns |  4.1751 ns |  4.1397 ns |  4.3257 ns |  4.2430 ns |  0.07 |     260 B |      - |      - |         - |        0.00 |
| StaticGenericField | .NET 10.0           | .NET 10.0 |  0.7934 ns | 0.0016 ns | 0.0024 ns |  0.7933 ns |  0.7900 ns |  0.8001 ns |  0.7964 ns |  0.01 |       6 B |      - |      - |         - |        0.00 |
|                    |                     |           |            |           |           |            |            |            |            |       |           |        |        |           |             |
| BuildEveryCall     | MediumRun-.NET 11.0 | .NET 11.0 | 57.2018 ns | 0.3878 ns | 0.5804 ns | 57.2364 ns | 56.1537 ns | 58.3178 ns | 57.9987 ns |  1.00 |   6,216 B | 0.0908 | 0.0002 |     760 B |        1.00 |
| DictionaryCache    | MediumRun-.NET 11.0 | .NET 11.0 |  4.2088 ns | 0.0626 ns | 0.0918 ns |  4.2700 ns |  4.0928 ns |  4.3310 ns |  4.3050 ns |  0.07 |     256 B |      - |      - |         - |        0.00 |
| StaticGenericField | MediumRun-.NET 11.0 | .NET 11.0 |  0.7923 ns | 0.0014 ns | 0.0020 ns |  0.7917 ns |  0.7895 ns |  0.7975 ns |  0.7948 ns |  0.01 |       6 B |      - |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted
- Generic static field read: ~0 ns / code size 6 B (below measurement resolution, same as TYP-01 generic path)
- Dictionary<Type,string> cache: 2.7 ns (0.048x vs rebuild); rebuild every call: 57.0 ns + 760 B
- Static generic is effectively free; use dictionary only when the type is not known statically

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| BuildEveryCall     | 57.0466 ns | 1.0539 ns | 1.4426 ns | 56.8813 ns | 52.4937 ns | 60.9476 ns | 58.4941 ns | 1.001 |    0.04 | 0.0908 |   5,220 B | 0.0002 |     760 B |        1.00 |
| DictionaryCache    |  2.7237 ns | 0.0114 ns | 0.0160 ns |  2.7223 ns |  2.6940 ns |  2.7587 ns |  2.7434 ns | 0.048 |    0.00 |      - |     936 B |      - |         - |        0.00 |
| StaticGenericField |  0.0010 ns | 0.0019 ns | 0.0026 ns |  0.0000 ns |  0.0000 ns |  0.0110 ns |  0.0031 ns | 0.000 |    0.00 |      - |       6 B |      - |         - |        0.00 |
