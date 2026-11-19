# STK-10: ref field cursor for field-granular structured reads

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted, stronger on net11. The ref-field reader is 0.68x of inline index arithmetic on net11 (514.4 vs 757.0 ns) against 0.80x on net10; it got 0.84x faster on net11 (613.2 → 514.4 ns)
- The slice reader stays 0.86x on both generations

### RefFieldStructReadBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ParseInlineIndex | 768.1 ns | 757.0 ns | 0.99 | - → - | 163 B → 162 B |
| ParseSpanReader | 759.5 ns | 743.3 ns | 0.98 | - → - | 153 B → 156 B |
| ParseSliceReader | 658.2 ns | 652.0 ns | 0.99 | - → - | 128 B → 137 B |
| ParseRefFieldReader | 613.2 ns | 514.4 ns | 0.84 | - → - | 111 B → 108 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.79 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ParseInlineIndex    | .NET 10.0           | .NET 10.0 | 768.1 ns |  1.51 ns |  2.26 ns | 764.6 ns | 772.6 ns | 771.5 ns |  1.00 |    0.00 |     163 B |         - |          NA |
| ParseSpanReader     | .NET 10.0           | .NET 10.0 | 759.5 ns |  1.24 ns |  1.82 ns | 756.1 ns | 762.5 ns | 761.6 ns |  0.99 |    0.00 |     153 B |         - |          NA |
| ParseSliceReader    | .NET 10.0           | .NET 10.0 | 658.2 ns |  1.24 ns |  1.82 ns | 655.2 ns | 660.9 ns | 660.4 ns |  0.86 |    0.00 |     128 B |         - |          NA |
| ParseRefFieldReader | .NET 10.0           | .NET 10.0 | 613.2 ns |  1.18 ns |  1.66 ns | 610.6 ns | 617.1 ns | 615.7 ns |  0.80 |    0.00 |     111 B |         - |          NA |
|                     |                     |           |          |          |          |          |          |          |       |         |           |           |             |
| ParseInlineIndex    | MediumRun-.NET 11.0 | .NET 11.0 | 757.0 ns |  0.87 ns |  1.25 ns | 754.9 ns | 759.7 ns | 758.6 ns |  1.00 |    0.00 |     162 B |         - |          NA |
| ParseSpanReader     | MediumRun-.NET 11.0 | .NET 11.0 | 743.3 ns | 10.60 ns | 15.86 ns | 724.9 ns | 764.2 ns | 760.5 ns |  0.98 |    0.02 |     156 B |         - |          NA |
| ParseSliceReader    | MediumRun-.NET 11.0 | .NET 11.0 | 652.0 ns |  1.07 ns |  1.60 ns | 649.1 ns | 655.7 ns | 653.8 ns |  0.86 |    0.00 |     137 B |         - |          NA |
| ParseRefFieldReader | MediumRun-.NET 11.0 | .NET 11.0 | 514.4 ns |  1.03 ns |  1.55 ns | 511.4 ns | 517.4 ns | 516.0 ns |  0.68 |    0.00 |     108 B |         - |          NA |

## Earlier record

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
