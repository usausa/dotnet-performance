# LAB: ReadOnlyCollection<int> foreach vs indexer on .NET 10 (R-04 follow-up)

- Verdict: the .NET 10 post's 'foreach now beats the indexer' did NOT reproduce. foreach is 1.21x slower (558.6 vs 460.0 ns), still allocates the 32 B enumerator, and compiles to 664 B against 134 B for the indexer. The B550H (x86-64-v3) provisional run agreed (1.26x, 32 B, 684 vs 134 B)
- What the disassembly shows: the array-interface devirtualization does reach through the wrapper's IList<T> field — both methods guard on `MT_System.Int32[]`, and foreach inlines `SZGenericArrayEnumerator<int>.MoveNext/Current` behind a guard on the enumerator's MT. But the enumerator object is still created with `CORINFO_HELP_NEWSFAST` (no stack allocation), so the loop reloads `_index` from the heap and performs three compares per element (MoveNext limit, Current limit, array bounds → RNGCHKFAIL), and the fallback interface-call paths inflate the method to 664 B
- The indexer loop is two type guards plus one bounds check per element, reading the array directly, with no allocation
- The indexer remains the safe choice for wrapper collections

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method  | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| Indexer | 460.0 ns |  3.28 ns |  4.81 ns | 454.4 ns | 477.3 ns | 465.2 ns |  1.00 |    0.01 |      - |     134 B |         - |          NA |
| Foreach | 558.6 ns | 30.28 ns | 42.45 ns | 509.1 ns | 615.3 ns | 602.6 ns |  1.21 |    0.09 | 0.0038 |     664 B |      32 B |          NA |
