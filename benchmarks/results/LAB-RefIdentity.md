# LAB-RefIdentity: ref identity and ref arithmetic (study queue 7-11)

- Verdict: index recovery rejected -> R-21; alias checking recorded as a quick-reference note
- Recovering the index from a ref with Unsafe.ByteOffset costs 1.45x against simply carrying the index
  (811.0 vs 560.1 ns) and the code is larger (82 vs 64 B). Same conclusion as R-02
- Alias checking: Unsafe.AreSame on the first elements is 0.71x of MemoryExtensions.Overlaps
  (714.1 vs 1,001.5 ns) with smaller code (89 vs 125 B)
- The two alias checks are not interchangeable. AreSame answers "do these start at the same address",
  Overlaps answers "do these ranges intersect at all"

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean       | Error    | StdDev    | Median     | Min      | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |-----------:|---------:|----------:|-----------:|---------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| IndexCarried           |   560.1 ns | 14.70 ns |  21.08 ns |   548.4 ns | 541.3 ns |   598.9 ns |   594.2 ns |  1.00 |    0.05 |      64 B |         - |          NA |
| IndexRecoveredFromRef  |   811.0 ns | 22.62 ns |  33.86 ns |   799.0 ns | 775.4 ns |   900.4 ns |   852.4 ns |  1.45 |    0.08 |      82 B |         - |          NA |
| AliasCheckWithAreSame  |   714.1 ns | 75.78 ns | 113.43 ns |   715.5 ns | 571.6 ns |   884.5 ns |   861.0 ns |  1.28 |    0.20 |      89 B |         - |          NA |
| AliasCheckWithOverlaps | 1,001.5 ns | 36.13 ns |  54.07 ns | 1,008.5 ns | 922.3 ns | 1,102.6 ns | 1,059.1 ns |  1.79 |    0.11 |     125 B |         - |          NA |

