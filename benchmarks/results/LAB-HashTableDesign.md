# LAB: ankerl::unordered_dense layout vs Dictionary<string, int> (1024 keys)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*HashTableDesignBenchmark*"` and replace the tables below. Verdicts rest on generated code and allocation counts, which do not depend on the machine.
>
> ❗ **想定外 / unexpected** — this result contradicts (or goes beyond) the source article's claim. The verdict written below is provisional; decide adoption or rejection only after the HX 370 run confirms or overturns it.

- Verdict: NOT adopted. The Robin Hood + fingerprint layout is 1.26-1.66x SLOWER than Dictionary on hit lookups and 1.14-1.30x slower on misses; build cost is equal
- The gap is not the hash function: giving Dictionary the same randomized string hash (StringComparer.Ordinal) still leaves Ankerl 1.40x slower (9.78 vs 6.97 us)
- The article's 3.8x came from a Dictionary baseline of 18.6 us per 1024 lookups; .NET 10's Dictionary does the same work in 5.7-7.8 us here, i.e. the baseline was the outlier
- Recorded as R-22 in rejected-patterns

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| DictionaryHit        |  7.824 μs | 0.3977 μs | 0.5952 μs |  6.701 μs |  8.906 μs |  8.522 μs |  1.01 |    0.11 |      - |     803 B |      - |         - |          NA |
| DictionaryOrdinalHit |  6.968 μs | 0.6634 μs | 0.9930 μs |  5.358 μs |  8.362 μs |  8.034 μs |  0.90 |    0.14 |      - |     806 B |      - |         - |          NA |
| AnkerlHit            |  9.776 μs | 0.4679 μs | 0.7004 μs |  8.401 μs | 11.033 μs | 10.527 μs |  1.26 |    0.13 |      - |     908 B |      - |         - |          NA |
| DictionaryMiss       |  6.797 μs | 0.3834 μs | 0.5738 μs |  5.475 μs |  8.061 μs |  7.658 μs |  0.87 |    0.10 |      - |     488 B |      - |         - |          NA |
| AnkerlMiss           |  8.843 μs | 0.4007 μs | 0.5998 μs |  7.789 μs | 10.105 μs |  9.514 μs |  1.14 |    0.11 |      - |     849 B |      - |         - |          NA |
| DictionaryBuild      | 12.296 μs | 0.8987 μs | 1.3451 μs | 10.216 μs | 15.065 μs | 14.155 μs |  1.58 |    0.21 | 1.8463 |   4,110 B |      - |   30936 B |          NA |
| AnkerlBuild          | 12.902 μs | 0.9169 μs | 1.3723 μs | 10.981 μs | 15.656 μs | 14.298 μs |  1.66 |    0.21 | 1.7090 |   1,556 B | 0.0763 |   28744 B |          NA |
