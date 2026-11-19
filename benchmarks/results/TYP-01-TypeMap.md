# TYP-01: TypeMap / TypeSlot

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the generic slot is 0.20x of `Dictionary<Type, T>` on net11 (0.81 ns, 34 B)
- FrozenDictionary got faster on net11 (4.06 → 3.22 ns) and is now 0.79x of Dictionary

### TypeMapBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DictionaryLookup | 4.2369 ns | 4.0795 ns | 0.96 | - → - | 248 B → 238 B |
| FrozenLookup | 4.0561 ns | 3.2162 ns | 0.79 | - → - | 45 B → 830 B |
| TypeMapGeneric | 0.8069 ns | 0.8075 ns | 1.00 | - → - | 34 B → 34 B |
| TypeMapRuntimeType | 12.2673 ns | 11.8380 ns | 0.97 | - → - | 4,649 B → 5,290 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.33 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DictionaryLookup   | .NET 10.0           | .NET 10.0 |  4.2369 ns | 0.0571 ns | 0.0800 ns |  4.2918 ns |  4.1313 ns |  4.3286 ns |  4.3184 ns |  1.00 |    0.00 |     248 B |         - |          NA |
| FrozenLookup       | .NET 10.0           | .NET 10.0 |  4.0561 ns | 0.0159 ns | 0.0213 ns |  4.0543 ns |  4.0135 ns |  4.0995 ns |  4.0819 ns |  0.96 |    0.02 |      45 B |         - |          NA |
| TypeMapGeneric     | .NET 10.0           | .NET 10.0 |  0.8069 ns | 0.0019 ns | 0.0028 ns |  0.8066 ns |  0.8013 ns |  0.8123 ns |  0.8111 ns |  0.19 |    0.00 |      34 B |         - |          NA |
| TypeMapRuntimeType | .NET 10.0           | .NET 10.0 | 12.2673 ns | 0.0311 ns | 0.0446 ns | 12.2735 ns | 12.1598 ns | 12.3639 ns | 12.3126 ns |  2.90 |    0.05 |   4,649 B |         - |          NA |
|                    |                     |           |            |           |           |            |            |            |            |       |         |           |           |             |
| DictionaryLookup   | MediumRun-.NET 11.0 | .NET 11.0 |  4.0795 ns | 0.0329 ns | 0.0483 ns |  4.0827 ns |  4.0077 ns |  4.1634 ns |  4.1378 ns |  1.00 |    0.00 |     238 B |         - |          NA |
| FrozenLookup       | MediumRun-.NET 11.0 | .NET 11.0 |  3.2162 ns | 0.0151 ns | 0.0221 ns |  3.2094 ns |  3.1889 ns |  3.2739 ns |  3.2466 ns |  0.79 |    0.01 |     830 B |         - |          NA |
| TypeMapGeneric     | MediumRun-.NET 11.0 | .NET 11.0 |  0.8075 ns | 0.0031 ns | 0.0042 ns |  0.8078 ns |  0.8019 ns |  0.8214 ns |  0.8113 ns |  0.20 |    0.00 |      34 B |         - |          NA |
| TypeMapRuntimeType | MediumRun-.NET 11.0 | .NET 11.0 | 11.8380 ns | 0.0862 ns | 0.1236 ns | 11.8006 ns | 11.6501 ns | 12.0353 ns | 11.9798 ns |  2.90 |    0.05 |   5,290 B |         - |          NA |

## Earlier record

- Verdict: adopted (implemented)
- Generic path (TypeSlot<T>.Index): 0.23 ns, 0.09x vs Dictionary<Type,T> (~11x faster), code 34 B
- Runtime Type path: 10.4 ns, 4.22x vs Dictionary (dictionary lookup + array access) - use only when the type is not known statically
- FrozenDictionary 1.25x (slower than Dictionary here, consistent with R-08)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DictionaryLookup   |  2.4681 ns | 0.0505 ns | 0.0691 ns |  2.3476 ns |  2.6277 ns |  2.5262 ns |  1.00 |    0.04 |     921 B |         - |          NA |
| FrozenLookup       |  3.0734 ns | 0.0418 ns | 0.0558 ns |  2.9741 ns |  3.1824 ns |  3.1344 ns |  1.25 |    0.04 |      45 B |         - |          NA |
| TypeMapGeneric     |  0.2260 ns | 0.0555 ns | 0.0740 ns |  0.1697 ns |  0.4915 ns |  0.3030 ns |  0.09 |    0.03 |      34 B |         - |          NA |
| TypeMapRuntimeType | 10.4113 ns | 0.0451 ns | 0.0632 ns | 10.3206 ns | 10.5742 ns | 10.4934 ns |  4.22 |    0.12 |   3,486 B |         - |          NA |
