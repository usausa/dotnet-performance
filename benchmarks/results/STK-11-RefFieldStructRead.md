# STK-11: ref field cursor for field-granular structured reads

- Verdict: adopted
- Re-measured on x86-64-v4 (Zen 5). Same ranking as the earlier x86-64-v3 (Zen 3) run, and the code sizes are
  byte for byte identical on both machines (163 / 153 / 128 / 111 B)
- ParseRefFieldReader 0.81x against index arithmetic written at the call site, CIs do not overlap
  (629-654 vs 786-793 ns), and the code is smaller (111 vs 163 B)
- The re-slicing cursor also wins without touching ref fields: ParseSliceReader 0.86x, 128 B
- Record layout under test: [byte tag][ushort length][length bytes payload], 512 records

## The margin narrows on the newer core - the direction does not

| | x86-64-v3 | x86-64-v4 |
|---|---|---|
| ParseSpanReader | 0.96x | 0.98x |
| ParseSliceReader | 0.81x | 0.86x |
| ParseRefFieldReader | **0.75x** | **0.81x** |

**Why it moves:** the generated code is identical on both machines, so this is entirely execution side. What the
cursor removes is the per-field address arithmetic (a base + offset computation before every read of a
differently-sized field). A wider out-of-order core hides more of that arithmetic behind the loads it feeds, so
the same removed work is worth less wall clock. Expect this family of wins to **keep its sign and lose
magnitude** as cores widen - and expect the reverse when the target is a narrow or in-order core.

## What to weigh

1. **Judge by the shape of the loop, not by the quoted ratio.** This wins because each step reads a different
   width, which keeps the index form from turning into a counted, vectorizable loop. Whole-element iteration is
   the opposite shape and the same technique loses there by 1.21x (R-12). The two records must be read together
2. **Treat the ratio as a floor that shrinks on newer hardware.** 0.75x on Zen 3, 0.81x on Zen 5, identical
   codegen. If the number is what justifies the complexity, re-measure on the deployment target
3. **The 0.75-0.81x is the reason to adopt it; the code-size drop is the part that carries across machines
   unchanged** (111 vs 163 B, identical on both). Never trade the time win away for the bytes - here both point
   the same way, which is what makes the choice easy
4. **Prefer the re-slicing cursor when ref fields are not otherwise needed.** ParseSliceReader captures most of
   the win (0.86x vs 0.81x) with an ordinary `ReadOnlySpan<T>` field and no ref-safety obligations

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ParseInlineIndex    | 789.4 ns |  3.77 ns |  5.52 ns | 781.5 ns | 803.6 ns | 793.3 ns |  1.00 |    0.01 |     163 B |         - |          NA |
| ParseSpanReader     | 776.4 ns |  3.36 ns |  4.93 ns | 769.2 ns | 787.2 ns | 783.1 ns |  0.98 |    0.01 |     153 B |         - |          NA |
| ParseSliceReader    | 676.2 ns |  3.20 ns |  4.69 ns | 669.7 ns | 688.2 ns | 683.0 ns |  0.86 |    0.01 |     128 B |         - |          NA |
| ParseRefFieldReader | 641.6 ns | 12.54 ns | 18.39 ns | 623.7 ns | 685.1 ns | 666.9 ns |  0.81 |    0.02 |     111 B |         - |          NA |
