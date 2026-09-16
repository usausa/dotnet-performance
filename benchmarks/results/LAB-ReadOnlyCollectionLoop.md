# LAB: ReadOnlyCollection<int> foreach vs indexer on .NET 10 (R-04 follow-up)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*ReadOnlyCollectionLoopBenchmark*"` and replace the tables below. Verdicts rest on generated code and allocation counts, which do not depend on the machine.
>
> ❗ **想定外 / unexpected** — this result contradicts (or goes beyond) the source article's claim. The verdict written below is provisional; decide adoption or rejection only after the HX 370 run confirms or overturns it.

- Verdict: the .NET 10 post's 'foreach now beats the indexer' did NOT reproduce. foreach is 1.26x slower, still allocates the 32 B enumerator, and compiles to 684 B against 134 B for the indexer
- Array-interface devirtualization did not reach through the wrapper's IList<T> field in this harness; the indexer remains the safe choice for wrapper collections

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method  | Mean       | Error    | StdDev   | Median     | Min      | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------- |-----------:|---------:|---------:|-----------:|---------:|-----------:|-----------:|------:|--------:|----------:|-------:|----------:|------------:|
| Indexer |   910.1 ns | 150.1 ns | 224.7 ns |   787.6 ns | 687.6 ns | 1,313.9 ns | 1,232.6 ns |  1.06 |    0.35 |     134 B |      - |         - |          NA |
| Foreach | 1,086.8 ns | 115.2 ns | 172.4 ns | 1,148.0 ns | 846.1 ns | 1,334.6 ns | 1,299.2 ns |  1.26 |    0.34 |     684 B | 0.0019 |      32 B |          NA |
