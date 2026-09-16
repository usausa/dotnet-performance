# LAB: bounds-check elimination shapes on .NET 10 (R-15 / R-18 follow-up)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*BoundsCheckPatternBenchmark*"` and replace the tables below. Verdicts rest on generated code and allocation counts, which do not depend on the machine.

- Verdict 1 (length taken from another sequence, Zenn #13): `prefix.Length < path.Length ? path[prefix.Length]` loses the check for string and array (22 B, no RNGCHKFAIL) but KEEPS it for ReadOnlySpan<char> (48 B, cmp/jae + CORINFO_HELP_RNGCHKFAIL + stack frame). 2.05x in this harness
- Verdict 2 (switch on Length, .NET 10 PR #113998): `switch (span.Length) { 4 => span[0]+..+span[3] }` compiles to the same check-free body as the `if (span.Length == 4)` guard (32 vs 31 B)
- Both verdicts are codegen facts (DisassemblyDiagnoser), independent of timing noise

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| OtherLength_String | 377.5 ns | 19.04 ns | 28.50 ns | 378.7 ns | 325.3 ns | 435.5 ns | 415.1 ns |  1.01 |    0.11 |      69 B |         - |          NA |
| OtherLength_Array  | 387.0 ns | 17.15 ns | 25.67 ns | 386.8 ns | 340.0 ns | 428.4 ns | 423.5 ns |  1.03 |    0.10 |      69 B |         - |          NA |
| OtherLength_Span   | 768.0 ns | 63.70 ns | 95.34 ns | 782.0 ns | 552.4 ns | 903.8 ns | 878.3 ns |  2.05 |    0.29 |     175 B |         - |          NA |
| LengthGuard_If     | 434.2 ns | 48.21 ns | 70.66 ns | 441.5 ns | 343.9 ns | 584.6 ns | 525.5 ns |  1.16 |    0.20 |     118 B |         - |          NA |
| LengthGuard_Switch | 411.3 ns | 59.53 ns | 89.11 ns | 366.3 ns | 344.8 ns | 595.3 ns | 557.7 ns |  1.10 |    0.25 |     119 B |         - |          NA |
