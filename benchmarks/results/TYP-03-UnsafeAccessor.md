# TYP-03: UnsafeAccessor vs reflection for private field access

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - UnsafeAccessor equals the public property (23 B both); reflection is 4.36 ns on net11 (5.15 ns on net10)

### UnsafeAccessorBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| PublicProperty | 0.2094 ns | 0.1989 ns | 0.95 | - → - | 23 B → 23 B |
| UnsafeAccessorField | 0.1803 ns | 0.1928 ns | 1.07 | - → - | 23 B → 23 B |
| ReflectionGetValue | 5.1543 ns | 4.3589 ns | 0.85 | 24 B → 24 B | 6,125 B → 6,008 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.54 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| PublicProperty      | .NET 10.0           | .NET 10.0 | 0.2094 ns | 0.0173 ns | 0.0254 ns | 0.1772 ns | 0.2832 ns | 0.2371 ns |  1.00 |    0.00 |      23 B |      - |         - |          NA |
| UnsafeAccessorField | .NET 10.0           | .NET 10.0 | 0.1803 ns | 0.0130 ns | 0.0173 ns | 0.1552 ns | 0.2069 ns | 0.1990 ns |  0.87 |    0.13 |      23 B |      - |         - |          NA |
| ReflectionGetValue  | .NET 10.0           | .NET 10.0 | 5.1543 ns | 0.0737 ns | 0.1057 ns | 5.0455 ns | 5.5012 ns | 5.2667 ns | 24.93 |    2.76 |   6,125 B | 0.0029 |      24 B |          NA |
|                     |                     |           |           |           |           |           |           |           |       |         |           |        |           |             |
| PublicProperty      | MediumRun-.NET 11.0 | .NET 11.0 | 0.1989 ns | 0.0096 ns | 0.0134 ns | 0.1660 ns | 0.2155 ns | 0.2130 ns |  1.00 |    0.00 |      23 B |      - |         - |          NA |
| UnsafeAccessorField | MediumRun-.NET 11.0 | .NET 11.0 | 0.1928 ns | 0.0058 ns | 0.0086 ns | 0.1755 ns | 0.2038 ns | 0.2025 ns |  0.97 |    0.08 |      23 B |      - |         - |          NA |
| ReflectionGetValue  | MediumRun-.NET 11.0 | .NET 11.0 | 4.3589 ns | 0.0551 ns | 0.0808 ns | 4.2787 ns | 4.6210 ns | 4.4525 ns | 22.02 |    1.60 |   6,008 B | 0.0029 |      24 B |          NA |

## Earlier record

- Verdict: adopted
- UnsafeAccessor 0.192 ns == public property 0.192 ns (code size 23 B both - compiles to a direct field load)
- FieldInfo.GetValue: 4.77 ns + 24 B boxing per read (24.9x)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| PublicProperty      | 0.1917 ns | 0.0014 ns | 0.0019 ns | 0.1879 ns | 0.1957 ns | 0.1941 ns |  1.00 |    0.01 |      - |      23 B |         - |          NA |
| UnsafeAccessorField | 0.1922 ns | 0.0019 ns | 0.0026 ns | 0.1879 ns | 0.1985 ns | 0.1958 ns |  1.00 |    0.02 |      - |      23 B |         - |          NA |
| ReflectionGetValue  | 4.7727 ns | 0.0699 ns | 0.0933 ns | 4.5929 ns | 5.0580 ns | 4.8853 ns | 24.90 |    0.54 | 0.0029 |   4,311 B |      24 B |          NA |
