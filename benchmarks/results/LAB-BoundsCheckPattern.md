# LAB: bounds-check elimination shapes on .NET 10 (R-15 / R-18 follow-up)

- Verdict 1 (length taken from another sequence, Zenn #13): `prefix.Length < path.Length ? path[prefix.Length]` loses the check for string and array (22 B, no RNGCHKFAIL) but KEEPS it for ReadOnlySpan<char> (48 B: the guard's `cmp/jge` is followed by the same two-register `cmp/jae`, a `sub rsp,28` frame and `CORINFO_HELP_RNGCHKFAIL`). 1.09x in this harness on x86-64-v4 (2.05x on the B550H x86-64-v3 run) — the time cost is core-dependent, the instruction sequences are identical on both machines
- Verdict 2 (switch on Length, .NET 10 PR #113998): `switch (span.Length) { 4 => span[0]+..+span[3] }` compiles to the same check-free body as the `if (span.Length == 4)` guard (32 vs 31 B, no RNGCHKFAIL in either; 363.2 vs 364.0 ns)
- Both verdicts are codegen facts (DisassemblyDiagnoser), independent of timing noise; the disassembly matches the B550H run method for method

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| OtherLength_String | 309.1 ns |  2.26 ns |  3.38 ns | 300.2 ns | 315.0 ns | 312.5 ns |  1.00 |    0.02 |      69 B |         - |          NA |
| OtherLength_Array  | 308.8 ns |  1.80 ns |  2.53 ns | 302.8 ns | 313.6 ns | 311.1 ns |  1.00 |    0.01 |      69 B |         - |          NA |
| OtherLength_Span   | 337.7 ns | 12.74 ns | 18.27 ns | 319.5 ns | 378.6 ns | 364.1 ns |  1.09 |    0.06 |     175 B |         - |          NA |
| LengthGuard_If     | 363.2 ns |  3.69 ns |  5.41 ns | 348.5 ns | 372.3 ns | 368.2 ns |  1.18 |    0.02 |     118 B |         - |          NA |
| LengthGuard_Switch | 364.0 ns |  4.39 ns |  6.57 ns | 347.8 ns | 377.4 ns | 370.8 ns |  1.18 |    0.02 |     119 B |         - |          NA |
