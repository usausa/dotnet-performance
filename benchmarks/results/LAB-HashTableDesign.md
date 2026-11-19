# LAB: ankerl::unordered_dense layout vs Dictionary<string, int> (1024 keys)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - not adopted (R-22). The Robin Hood + fingerprint table is 1.55x of Dictionary on hit and 1.61x on miss on net11 (1.46x / 1.56x on net10)
- Dictionary hit got 0.95x faster on net11 (3.96 → 3.75 μs) and the table did not, so the gap widens slightly

### HashTableDesignBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DictionaryHit | 3.961 μs | 3.750 μs | 0.95 | - → - | 1,173 B → 572 B |
| DictionaryOrdinalHit | 3.916 μs | 3.780 μs | 0.97 | - → - | 1,017 B → 759 B |
| AnkerlHit | 5.767 μs | 5.811 μs | 1.01 | - → - | 1,064 B → 557 B |
| DictionaryMiss | 3.371 μs | 3.322 μs | 0.99 | - → - | 502 B → 485 B |
| AnkerlMiss | 5.271 μs | 5.354 μs | 1.02 | - → - | 1,043 B → 553 B |
| DictionaryBuild | 5.964 μs | 5.891 μs | 0.99 | 30936 B → 30936 B | 4,880 B → 5,129 B |
| AnkerlBuild | 7.355 μs | 7.101 μs | 0.97 | 28744 B → 28744 B | 1,630 B → 1,009 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.93 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| DictionaryHit        | .NET 10.0           | .NET 10.0 | 3.961 μs | 0.0672 μs | 0.1006 μs | 3.927 μs | 3.873 μs | 4.284 μs | 4.149 μs |  1.00 |    0.00 |   1,173 B |      - |      - |         - |          NA |
| DictionaryOrdinalHit | .NET 10.0           | .NET 10.0 | 3.916 μs | 0.0203 μs | 0.0297 μs | 3.924 μs | 3.861 μs | 3.953 μs | 3.947 μs |  0.99 |    0.02 |   1,017 B |      - |      - |         - |          NA |
| AnkerlHit            | .NET 10.0           | .NET 10.0 | 5.767 μs | 0.0425 μs | 0.0637 μs | 5.763 μs | 5.671 μs | 5.993 μs | 5.845 μs |  1.46 |    0.04 |   1,064 B |      - |      - |         - |          NA |
| DictionaryMiss       | .NET 10.0           | .NET 10.0 | 3.371 μs | 0.0097 μs | 0.0136 μs | 3.371 μs | 3.350 μs | 3.407 μs | 3.386 μs |  0.85 |    0.02 |     502 B |      - |      - |         - |          NA |
| AnkerlMiss           | .NET 10.0           | .NET 10.0 | 5.271 μs | 0.0290 μs | 0.0397 μs | 5.262 μs | 5.232 μs | 5.385 μs | 5.317 μs |  1.33 |    0.03 |   1,043 B |      - |      - |         - |          NA |
| DictionaryBuild      | .NET 10.0           | .NET 10.0 | 5.964 μs | 0.0980 μs | 0.1342 μs | 6.050 μs | 5.796 μs | 6.147 μs | 6.108 μs |  1.51 |    0.05 |   4,880 B | 3.6850 |      - |   30936 B |          NA |
| AnkerlBuild          | .NET 10.0           | .NET 10.0 | 7.355 μs | 0.0525 μs | 0.0736 μs | 7.389 μs | 7.255 μs | 7.462 μs | 7.437 μs |  1.86 |    0.05 |   1,630 B | 3.4332 | 0.1984 |   28744 B |          NA |
|                      |                     |           |          |           |           |          |          |          |          |       |         |           |        |        |           |             |
| DictionaryHit        | MediumRun-.NET 11.0 | .NET 11.0 | 3.750 μs | 0.0103 μs | 0.0151 μs | 3.751 μs | 3.727 μs | 3.777 μs | 3.772 μs |  1.00 |    0.00 |     572 B |      - |      - |         - |          NA |
| DictionaryOrdinalHit | MediumRun-.NET 11.0 | .NET 11.0 | 3.780 μs | 0.0300 μs | 0.0400 μs | 3.761 μs | 3.737 μs | 3.851 μs | 3.836 μs |  1.01 |    0.01 |     759 B |      - |      - |         - |          NA |
| AnkerlHit            | MediumRun-.NET 11.0 | .NET 11.0 | 5.811 μs | 0.0293 μs | 0.0430 μs | 5.799 μs | 5.753 μs | 5.876 μs | 5.863 μs |  1.55 |    0.01 |     557 B |      - |      - |         - |          NA |
| DictionaryMiss       | MediumRun-.NET 11.0 | .NET 11.0 | 3.322 μs | 0.0096 μs | 0.0131 μs | 3.322 μs | 3.296 μs | 3.349 μs | 3.337 μs |  0.89 |    0.00 |     485 B |      - |      - |         - |          NA |
| AnkerlMiss           | MediumRun-.NET 11.0 | .NET 11.0 | 5.354 μs | 0.0120 μs | 0.0179 μs | 5.356 μs | 5.328 μs | 5.391 μs | 5.373 μs |  1.43 |    0.01 |     553 B |      - |      - |         - |          NA |
| DictionaryBuild      | MediumRun-.NET 11.0 | .NET 11.0 | 5.891 μs | 0.0314 μs | 0.0460 μs | 5.896 μs | 5.812 μs | 5.992 μs | 5.943 μs |  1.57 |    0.01 |   5,129 B | 3.6850 |      - |   30936 B |          NA |
| AnkerlBuild          | MediumRun-.NET 11.0 | .NET 11.0 | 7.101 μs | 0.0136 μs | 0.0195 μs | 7.101 μs | 7.055 μs | 7.155 μs | 7.121 μs |  1.89 |    0.01 |   1,009 B | 3.4332 | 0.1984 |   28744 B |          NA |

## Earlier record

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
