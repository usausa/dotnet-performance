# COL-08: Caching absence and sharing the empty result

- Verdict: **conditional** - adopted when the set of queried keys is closed and the same absent key is asked for
  more than once; the first call per key gets heavier
- First measurement in this repository, on the reference environment (HX 370, x86-64-v4, .NET 11.0.0 JIT)
- Steady-state miss: 63.15 ns / 56 B → 4.44 ns / 0 B (0.07x), the same as a registered hit (4.45 ns)
- First call, 20 distinct absent keys once each: 1,477 ns / 1,688 B → **2,762 ns / 13,168 B (1.87x)**
- Break-even: the negative cache adds about 64 ns per new key and saves about 59 ns per repeated miss, so it
  pays from the second miss of the same key
- Lock contention is not part of this benchmark; the catalog keeps the reference figure for it

## NegativeCacheBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.06 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                         | Mean         | Error      | StdDev     | Min          | Max          | P90          | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------- |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| MissWithoutNegativeCache       |    63.146 ns |  0.4178 ns |  0.5577 ns |    62.264 ns |    64.448 ns |    63.897 ns |  1.00 |    0.00 |   7,369 B | 0.0067 |      - |      56 B |        1.00 |
| MissWithNegativeCache          |     4.436 ns |  0.0565 ns |  0.0734 ns |     4.375 ns |     4.669 ns |     4.466 ns |  0.07 |    0.00 |   7,375 B |      - |      - |         - |        0.00 |
| RegisteredHit                  |     4.454 ns |  0.0519 ns |  0.0744 ns |     4.381 ns |     4.738 ns |     4.527 ns |  0.07 |    0.00 |   7,378 B |      - |      - |         - |        0.00 |
| ColdMissesWithoutNegativeCache | 1,476.787 ns | 14.3412 ns | 20.1044 ns | 1,444.454 ns | 1,539.312 ns | 1,501.031 ns | 23.39 |    0.37 |  11,552 B | 0.2003 |      - |    1688 B |       30.14 |
| ColdMissesWithNegativeCache    | 2,762.454 ns | 41.0559 ns | 58.8811 ns | 2,672.771 ns | 2,895.588 ns | 2,829.630 ns | 43.75 |    0.99 |  10,274 B | 1.5717 | 0.0076 |   13168 B |      235.14 |

## Basis for the verdict

1. **Steady state:** disjoint intervals, 14x faster, and the per-call empty array disappears. The miss takes
   the same lock-free path as a hit
2. **First call:** also disjoint, in the other direction. Each new key copies the snapshot dictionary, which
   is where the extra 11,480 B over 20 keys comes from; the cost grows with the number of cached keys
3. **Condition:** both axes have to be kept. With a closed key set and repeated misses the cache wins from the
   second call; with an open key set (type names from external input) the snapshot copies grow without bound
4. **Generated code:** the steady-state methods are the same size (7,369-7,378 B) because both resolvers share
   one `Resolve`; the difference is which branch the call takes, not the code that is emitted
5. **Against the Zen 3 reference:** same direction and similar ratios - steady state 0.07x here against 0.06x
   there, first call 1.87x against 2.14x
