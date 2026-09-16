# LAB: ankerl::unordered_dense layout vs Dictionary<string, int> (1024 keys)

- Verdict: NOT adopted (R-22). The Robin Hood + fingerprint layout is 1.48x SLOWER than Dictionary on hit lookups (5.82 vs 3.93 us), 1.58x slower on misses (5.33 vs 3.38 us) and 1.21x slower to build (7.98 vs 6.59 us); every pair has non-overlapping CIs
- The gap is not the hash function: giving Dictionary the same randomized string hash (StringComparer.Ordinal) leaves Ankerl still 1.48x slower (5.82 vs 3.92 us)
- The B550H (x86-64-v3) provisional run had the same sign at a smaller margin (hit 1.26-1.66x, miss 1.14-1.30x, build equal), so the newer core widens the gap rather than closing it. Allocation counts match on both machines (30,936 vs 28,744 B per build)
- The article's 3.8x came from a Dictionary baseline of 18.6 us per 1024 lookups; .NET 10's Dictionary does the same work in 3.9 us here (5.7-7.8 us on x86-64-v3), i.e. the baseline was the outlier

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Gen1   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|-------:|----------:|------------:|
| DictionaryHit        | 3.931 μs | 0.0292 μs | 0.0400 μs | 3.929 μs | 3.878 μs | 4.048 μs | 3.986 μs |  1.00 |    0.01 |      - |     991 B |      - |         - |          NA |
| DictionaryOrdinalHit | 3.919 μs | 0.0229 μs | 0.0322 μs | 3.910 μs | 3.874 μs | 4.014 μs | 3.956 μs |  1.00 |    0.01 |      - |     803 B |      - |         - |          NA |
| AnkerlHit            | 5.819 μs | 0.0299 μs | 0.0389 μs | 5.806 μs | 5.763 μs | 5.918 μs | 5.864 μs |  1.48 |    0.02 |      - |     978 B |      - |         - |          NA |
| DictionaryMiss       | 3.376 μs | 0.0304 μs | 0.0436 μs | 3.365 μs | 3.331 μs | 3.520 μs | 3.421 μs |  0.86 |    0.01 |      - |     502 B |      - |         - |          NA |
| AnkerlMiss           | 5.328 μs | 0.0225 μs | 0.0309 μs | 5.327 μs | 5.278 μs | 5.376 μs | 5.368 μs |  1.36 |    0.02 |      - |     849 B |      - |         - |          NA |
| DictionaryBuild      | 6.593 μs | 0.3365 μs | 0.4717 μs | 6.848 μs | 6.075 μs | 7.369 μs | 7.129 μs |  1.68 |    0.12 | 3.6850 |   4,098 B |      - |   30936 B |          NA |
| AnkerlBuild          | 7.976 μs | 0.0778 μs | 0.1140 μs | 7.982 μs | 7.730 μs | 8.160 μs | 8.110 μs |  2.03 |    0.03 | 3.4256 |   1,633 B | 0.1678 |   28744 B |          NA |
