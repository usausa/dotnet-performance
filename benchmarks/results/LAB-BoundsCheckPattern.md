# LAB: bounds-check elimination shapes on .NET 10 (R-15 / R-18 follow-up)

## net11 / net10 re-verification (2026-11-19, HX 370)

- **Verdict 1 changed on net11: the ReadOnlySpan<char> form no longer keeps its bounds check.** `prefix.Length < path.Length ? path[prefix.Length] : -1` compiles to 26 B with no `jae` / throw block on net11 (48 B with the check on net10), the same shape as string and array
- The time shows it only weakly (322.0 → 318.8 ns, 1.05x of string on net11 against 1.08x on net10), consistent with the earlier record's finding that the check costs little here
- Verdict 2 unchanged: the `switch (span.Length)` and `if (span.Length == 4)` forms stay check-free and equal (31 / 32 B, 358.9 / 359.1 ns)

### BoundsCheckPatternBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| OtherLength_String | 297.6 ns | 305.0 ns | 1.02 | - → - | 69 B → 69 B |
| OtherLength_Array | 296.4 ns | 305.8 ns | 1.03 | - → - | 69 B → 69 B |
| OtherLength_Span | 322.0 ns | 318.8 ns | 0.99 | - → - | 175 B → 153 B |
| LengthGuard_If | 358.3 ns | 358.9 ns | 1.00 | - → - | 118 B → 118 B |
| LengthGuard_Switch | 358.0 ns | 359.1 ns | 1.00 | - → - | 119 B → 119 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.92 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| OtherLength_String | .NET 10.0           | .NET 10.0 | 297.6 ns |  8.94 ns | 13.10 ns | 267.1 ns | 314.9 ns | 311.6 ns |  1.00 |    0.00 |      69 B |         - |          NA |
| OtherLength_Array  | .NET 10.0           | .NET 10.0 | 296.4 ns | 11.78 ns | 16.51 ns | 236.2 ns | 321.7 ns | 311.7 ns |  1.00 |    0.07 |      69 B |         - |          NA |
| OtherLength_Span   | .NET 10.0           | .NET 10.0 | 322.0 ns |  1.03 ns |  1.47 ns | 319.3 ns | 325.6 ns | 323.3 ns |  1.08 |    0.05 |     175 B |         - |          NA |
| LengthGuard_If     | .NET 10.0           | .NET 10.0 | 358.3 ns |  4.15 ns |  6.20 ns | 340.3 ns | 365.2 ns | 364.4 ns |  1.21 |    0.06 |     118 B |         - |          NA |
| LengthGuard_Switch | .NET 10.0           | .NET 10.0 | 358.0 ns |  4.48 ns |  6.28 ns | 344.8 ns | 370.3 ns | 364.0 ns |  1.21 |    0.06 |     119 B |         - |          NA |
|                    |                     |           |          |          |          |          |          |          |       |         |           |           |             |
| OtherLength_String | MediumRun-.NET 11.0 | .NET 11.0 | 305.0 ns |  4.68 ns |  7.01 ns | 284.7 ns | 312.3 ns | 311.8 ns |  1.00 |    0.00 |      69 B |         - |          NA |
| OtherLength_Array  | MediumRun-.NET 11.0 | .NET 11.0 | 305.8 ns |  3.29 ns |  4.92 ns | 292.7 ns | 312.4 ns | 311.4 ns |  1.00 |    0.03 |      69 B |         - |          NA |
| OtherLength_Span   | MediumRun-.NET 11.0 | .NET 11.0 | 318.8 ns |  0.78 ns |  1.14 ns | 316.5 ns | 321.9 ns | 320.5 ns |  1.05 |    0.02 |     153 B |         - |          NA |
| LengthGuard_If     | MediumRun-.NET 11.0 | .NET 11.0 | 358.9 ns |  4.74 ns |  7.09 ns | 334.2 ns | 368.3 ns | 365.6 ns |  1.18 |    0.04 |     118 B |         - |          NA |
| LengthGuard_Switch | MediumRun-.NET 11.0 | .NET 11.0 | 359.1 ns |  5.08 ns |  7.61 ns | 342.8 ns | 376.1 ns | 365.4 ns |  1.18 |    0.04 |     119 B |         - |          NA |

## Earlier record

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
