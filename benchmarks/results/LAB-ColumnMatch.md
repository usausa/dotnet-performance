# LAB-ColumnMatch: normalizing the probe vs matching case-insensitively (TXT-10 counter-case)

- Verdict: **converting the probe never pays - it loses in all 12 conditions measured.** For the match itself
  the crossover is at column count: the `Equals(OrdinalIgnoreCase)` chain wins at 8 columns (0.84x) and the
  plain ordinal switch wins at 24 (0.91x), both measured like for like
- Shape: resolve a DB reader's column names to ordinals under SQL identifier rules (OrdinalIgnoreCase). The
  generated form is a guarded `Equals(OrdinalIgnoreCase)` chain at 8 columns and a sampling-hash switch at 24;
  the alternative is to upper-case the probe first so a plain ordinal switch can be used
- Measured on x86-64-v4 (Zen 5, .NET 10.0.11). First pass: 4 classes x 5 variants x 2 casings = 40 cases. The
  24-column snake_case rows were re-measured after an unstable first run (100.93 ns +/- 11.37 -> 84.42 +/- 0.82),
  and the 8-column classes were re-run with a sixth variant after the harness fix below
- **The headline is not "IgnoreCase is expensive".** `string.Equals(name, "Id", OrdinalIgnoreCase)` is the
  cheapest thing here. What costs is the *conversion* introduced to avoid needing it

## The harness fix, and why the first reading of the 8-column rows was wrong

The 24-column classes always ran their baseline through the same harness as the variants
(`GeneratedHash() => ResolveByMatcher(names, static x => MatchHash(x))`), so those rows were like for like from
the start. **The 8-column classes did not:** `GeneratedChain() => ResolveChain(names)` is a direct call that
inlines into a 210-instruction body, while every variant went through `ResolveByMatcher` - a `Func<string,int>`
indirect call per column plus a non-inlined call into the matcher.

`ChainViaMatcher` was added to the two 8-column classes: the same chain, reached through the harness.

| 8 columns, AsDeclared | Chain, direct (inlined) | Chain, via the harness | Harness cost |
|---|---:|---:|---:|
| PascalCase | 8.427 ns | 25.895 ns | +17.5 ns (**2.18 ns/column**) |
| snake_case | 9.365 ns | 24.625 ns | +15.3 ns (**1.91 ns/column**) |

**The harness costs more than the difference it was hiding**, which is why the first pass read the plain switch
as 3.0-3.6x slower than the chain when the real figure is 1.17-1.19x. Any comparison where only some variants
pay a delegate call is worthless at this scale.

## 1. The match itself, like for like (all variants through the harness)

| Per column | 8 cols, Pascal | 8 cols, snake | 24 cols, Pascal | 24 cols, snake |
|---|---:|---:|---:|---:|
| `Equals(OrdinalIgnoreCase)` chain | **3.24 ns** | **3.08 ns** | - | - |
| Sampling-hash switch (generated at 24) | - | - | 3.55 ns | 3.61 ns |
| Plain ordinal switch, no case handling | 3.86 ns | 3.61 ns | **3.24 ns** | **3.52 ns** |

| Chain / hash vs plain ordinal switch | Ratio | CIs |
|---|---:|---|
| 8 cols, PascalCase | switch is **1.19x** slower | disjoint (25.43-26.36 vs 30.50-31.22 ns) |
| 8 cols, snake_case | switch is **1.17x** slower | disjoint (24.31-24.94 vs 28.61-29.10 ns) |
| 24 cols, PascalCase | switch is **0.91x** | disjoint |
| 24 cols, snake_case | switch is **0.98x** | overlap - a tie |

**The crossover is the chain's shape:** a chain checks the probe against the literals in declaration order, so
its cost per probe grows with the column count (about 4.5 comparisons on average at 8 columns, 12.5 at 24),
while a switch is flat - 3.24-3.86 ns per column at both sizes. That is exactly the boundary the catalog already
draws between a chain and a sampling-hash switch, now measured on both sides of it.

## 2. What the conversion costs (same harness, `AsDeclared`)

| Columns / naming | Plain switch (no conversion) | `Ascii.ToUpper` + span switch | `ToUpperInvariant` + span switch | `string.ToUpperInvariant()` + string switch |
|---|---:|---:|---:|---:|
| 8 / PascalCase | 30.86 ns | 52.59 (**1.70x**) | 58.07 (1.88x) | 77.38 (2.51x) + 304 B |
| 8 / snake_case | 28.86 ns | 54.09 (**1.87x**) | 59.62 (2.07x) | 88.07 (3.05x) + 328 B |
| 24 / PascalCase | 77.85 ns | 154.40 (**1.98x**) | 165.88 (2.13x) | 247.99 (3.19x) + 976 B |
| 24 / snake_case | 84.42 ns | 169.95 (**2.01x**) | 171.18 (2.03x) | 244.44 (2.90x) + 1,008 B |

- **The conversion costs 1.70-2.13x of the match itself** over a span, and 2.51-3.19x plus an allocation through
  `string.ToUpperInvariant()`. Against the generated form it is 2.0-3.0x at 8 columns and 1.35-2.91x at 24 -
  **12 of 12 conditions lose**
- The ASCII-specialised conversion is the cheapest of the three in every condition, consistent with TXT-06, but
  it only shaves 6-11% off the span-based one - not enough to change any decision
- `string.ToUpperInvariant()` allocates only when the input is not already upper case: 304-1,008 B under
  `AsDeclared`, **zero** under `AllUpper`, where the BCL returns the same instance. That is also why its
  `AllUpper` timings are so much better than its `AsDeclared` ones
- **`PlainSpanSwitch` resolves nothing under `AllUpper`** - it has no case handling, and Verify only checks it
  under `AsDeclared`. Those rows look fast because they bail out early and are excluded from every comparison

## What to weigh

1. **Keep the comparison case-insensitive. Never normalise the probe to make a case-sensitive switch usable.**
   It costs 1.70-2.13x of the match and loses to the generated form in every condition measured.
   `Equals(..., OrdinalIgnoreCase)` is the cheapest primitive in the whole table
2. **Pick the match shape by column count, which is what the catalog already says:** the chain wins at 8 columns
   (0.84x against a plain switch), the switch wins at 24 (0.91x against the sampling hash). The reason is
   structural - a chain is O(columns) per probe, a switch is flat - so the boundary moves with the count, not
   with the naming convention (PascalCase and snake_case agree within a few percent everywhere)
3. **Where case sensitivity is acceptable at 24+ columns, a plain ordinal switch is the better generated shape**:
   0.91x and a third less code (2,212 vs 3,261 B) against the sampling-hash form
4. **When a conversion is genuinely unavoidable, convert as few characters as possible** - upper-cased sampling
   plus an `OrdinalIgnoreCase` confirm touches 3 characters instead of all of them. Full-probe normalisation is
   structural cost: fold every character into a buffer, then read every character again in the switch
5. **Methodology: make every variant pay the same call shape before reading any ratio.** The harness asymmetry
   here was worth 1.9-2.2 ns per column and inverted the 8-column conclusion. Verify proved the variants
   returned equal results, which is not the same as proving they are equally *shaped*

## x86-64-v4 - 8 columns, PascalCase (with ChainViaMatcher)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedChain**       | **AsDeclared** |  **8.427 ns** | **0.2998 ns** | **0.4300 ns** |  **7.717 ns** |  **9.473 ns** |  **9.056 ns** |  **1.00** |    **0.07** |   **1,002 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 77.380 ns | 2.4477 ns | 3.4314 ns | 73.014 ns | 83.784 ns | 81.515 ns |  9.20 |    0.60 |   2,541 B | 0.0362 |     304 B |          NA |
| UpperSpanSwitch      | AsDeclared | 58.067 ns | 0.7419 ns | 1.1105 ns | 56.317 ns | 60.837 ns | 59.313 ns |  6.91 |    0.36 |   1,809 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 52.588 ns | 0.5470 ns | 0.8187 ns | 51.162 ns | 54.341 ns | 53.591 ns |  6.26 |    0.32 |   1,545 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 30.859 ns | 0.3600 ns | 0.5388 ns | 29.775 ns | 31.899 ns | 31.446 ns |  3.67 |    0.19 |     941 B |      - |         - |          NA |
| ChainViaMatcher      | AsDeclared | 25.895 ns | 0.4667 ns | 0.6986 ns | 24.754 ns | 27.334 ns | 26.686 ns |  3.08 |    0.17 |   1,053 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **AllUpper**   |  **9.169 ns** | **0.1516 ns** | **0.2270 ns** |  **8.784 ns** |  **9.672 ns** |  **9.401 ns** |  **1.00** |    **0.03** |     **999 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 42.874 ns | 0.7669 ns | 1.1478 ns | 40.637 ns | 45.321 ns | 44.239 ns |  4.68 |    0.17 |   2,199 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 58.331 ns | 0.6765 ns | 1.0126 ns | 56.625 ns | 60.252 ns | 59.539 ns |  6.37 |    0.19 |   1,796 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 52.334 ns | 0.7158 ns | 1.0714 ns | 50.766 ns | 54.993 ns | 53.566 ns |  5.71 |    0.18 |   1,592 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   | 27.849 ns | 0.3665 ns | 0.5486 ns | 26.953 ns | 29.408 ns | 28.424 ns |  3.04 |    0.09 |     921 B |      - |         - |          NA |
| ChainViaMatcher      | AllUpper   | 23.464 ns | 0.2681 ns | 0.3845 ns | 22.902 ns | 24.509 ns | 23.923 ns |  2.56 |    0.07 |   1,053 B |      - |         - |          NA |

## x86-64-v4 - 8 columns, snake_case (with ChainViaMatcher)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedChain**       | **AsDeclared** |  **9.365 ns** | **0.1327 ns** | **0.1904 ns** |  **9.388 ns** |  **8.989 ns** |  **9.618 ns** |  **9.575 ns** |  **1.00** |    **0.03** |   **1,024 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 88.072 ns | 1.2621 ns | 1.8891 ns | 87.892 ns | 83.206 ns | 91.934 ns | 90.735 ns |  9.41 |    0.27 |   2,516 B | 0.0391 |     328 B |          NA |
| UpperSpanSwitch      | AsDeclared | 59.621 ns | 0.7160 ns | 1.0716 ns | 59.413 ns | 58.034 ns | 62.388 ns | 60.892 ns |  6.37 |    0.17 |   1,799 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 54.087 ns | 0.8540 ns | 1.2248 ns | 53.627 ns | 52.369 ns | 56.403 ns | 55.772 ns |  5.78 |    0.17 |   1,563 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 28.858 ns | 0.2439 ns | 0.3575 ns | 28.742 ns | 28.289 ns | 29.620 ns | 29.354 ns |  3.08 |    0.07 |     959 B |      - |         - |          NA |
| ChainViaMatcher      | AsDeclared | 24.625 ns | 0.3184 ns | 0.4766 ns | 24.676 ns | 23.632 ns | 25.520 ns | 25.220 ns |  2.63 |    0.07 |   1,074 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **AllUpper**   |  **9.206 ns** | **0.2881 ns** | **0.4313 ns** |  **9.106 ns** |  **8.539 ns** |  **9.951 ns** |  **9.757 ns** |  **1.00** |    **0.07** |   **1,021 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 42.556 ns | 0.5993 ns | 0.8970 ns | 42.512 ns | 40.725 ns | 44.327 ns | 43.558 ns |  4.63 |    0.23 |   2,186 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 61.150 ns | 0.6623 ns | 0.9285 ns | 61.200 ns | 59.571 ns | 64.065 ns | 62.057 ns |  6.66 |    0.32 |   1,799 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 55.751 ns | 0.5540 ns | 0.8292 ns | 55.587 ns | 53.722 ns | 57.920 ns | 56.628 ns |  6.07 |    0.29 |   1,544 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   | 25.698 ns | 1.5517 ns | 2.3225 ns | 24.415 ns | 23.899 ns | 31.432 ns | 29.579 ns |  2.80 |    0.28 |     920 B |      - |         - |          NA |
| ChainViaMatcher      | AllUpper   | 21.374 ns | 0.3161 ns | 0.4533 ns | 21.247 ns | 20.695 ns | 22.630 ns | 21.950 ns |  2.33 |    0.12 |   1,074 B |      - |         - |          NA |

## x86-64-v4 - 24 columns, PascalCase

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedHash**        | **AsDeclared** |  **85.18 ns** | **1.021 ns** | **1.464 ns** |  **83.59 ns** |  **89.98 ns** |  **87.48 ns** |  **1.00** |    **0.02** |   **3,261 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 247.99 ns | 3.077 ns | 4.606 ns | 241.34 ns | 259.40 ns | 255.39 ns |  2.91 |    0.07 |   3,794 B | 0.1166 |     976 B |          NA |
| UpperSpanSwitch      | AsDeclared | 165.88 ns | 0.842 ns | 1.261 ns | 163.72 ns | 168.22 ns | 167.38 ns |  1.95 |    0.04 |   3,026 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 154.40 ns | 1.574 ns | 2.257 ns | 150.94 ns | 157.63 ns | 157.06 ns |  1.81 |    0.04 |   2,812 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared |  77.85 ns | 0.307 ns | 0.441 ns |  77.00 ns |  79.11 ns |  78.27 ns |  0.91 |    0.02 |   2,212 B |      - |         - |          NA |
|                      |            |           |          |          |           |           |           |       |         |           |        |           |             |
| **GeneratedHash**        | **AllUpper**   |  **87.41 ns** | **1.274 ns** | **1.867 ns** |  **84.04 ns** |  **89.69 ns** |  **89.49 ns** |  **1.00** |    **0.03** |   **3,314 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 118.21 ns | 1.026 ns | 1.439 ns | 115.12 ns | 120.43 ns | 119.72 ns |  1.35 |    0.03 |   3,462 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 168.52 ns | 1.124 ns | 1.647 ns | 165.05 ns | 172.45 ns | 170.59 ns |  1.93 |    0.04 |   3,019 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 154.08 ns | 1.427 ns | 2.092 ns | 150.03 ns | 160.08 ns | 156.39 ns |  1.76 |    0.04 |   2,819 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   |  81.97 ns | 0.706 ns | 1.035 ns |  80.37 ns |  84.17 ns |  83.46 ns |  0.94 |    0.02 |   2,174 B |      - |         - |          NA |

## x86-64-v4 - 24 columns, snake_case (run 1, the unstable PlainSpanSwitch row)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Casing     | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| **GeneratedHash**        | **AsDeclared** |  **85.23 ns** |  **2.862 ns** |  **4.011 ns** |  **83.07 ns** |  **81.37 ns** |  **93.30 ns** |  **90.78 ns** |  **1.00** |    **0.06** |   **3,294 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 244.44 ns |  2.463 ns |  3.687 ns | 243.16 ns | 239.53 ns | 253.45 ns | 249.87 ns |  2.87 |    0.14 |   3,814 B | 0.1202 |    1008 B |          NA |
| UpperSpanSwitch      | AsDeclared | 171.18 ns |  3.203 ns |  4.593 ns | 169.88 ns | 164.95 ns | 178.73 ns | 176.93 ns |  2.01 |    0.10 |   3,028 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 169.95 ns |  5.320 ns |  7.102 ns | 166.58 ns | 160.95 ns | 184.68 ns | 181.42 ns |  2.00 |    0.12 |   2,825 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 100.93 ns | 11.372 ns | 17.022 ns |  90.28 ns |  84.61 ns | 129.06 ns | 124.65 ns |  1.19 |    0.20 |   2,218 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedHash**        | **AllUpper**   |  **87.44 ns** |  **1.152 ns** |  **1.652 ns** |  **87.53 ns** |  **84.79 ns** |  **90.55 ns** |  **89.69 ns** |  **1.00** |    **0.03** |   **3,254 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 124.78 ns |  2.714 ns |  3.979 ns | 124.00 ns | 120.32 ns | 136.20 ns | 128.22 ns |  1.43 |    0.05 |   3,486 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 175.63 ns |  2.046 ns |  2.869 ns | 175.06 ns | 172.44 ns | 185.30 ns | 179.24 ns |  2.01 |    0.05 |   3,045 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 165.24 ns |  2.645 ns |  3.877 ns | 164.06 ns | 161.12 ns | 178.61 ns | 170.08 ns |  1.89 |    0.06 |   2,825 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   |  84.21 ns |  1.047 ns |  1.535 ns |  83.66 ns |  82.42 ns |  87.80 ns |  86.31 ns |  0.96 |    0.02 |   2,241 B |      - |         - |          NA |

## x86-64-v4 - 24 columns, snake_case (run 2, GeneratedHash / PlainSpanSwitch only)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Casing     | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **GeneratedHash**   | **AsDeclared** | **86.59 ns** | **2.146 ns** | **3.078 ns** | **82.70 ns** | **92.11 ns** | **90.20 ns** |  **1.00** |    **0.05** |   **3,265 B** |         **-** |          **NA** |
| PlainSpanSwitch | AsDeclared | 84.42 ns | 0.818 ns | 1.224 ns | 82.37 ns | 87.22 ns | 85.76 ns |  0.98 |    0.04 |   2,221 B |         - |          NA |
|                 |            |          |          |          |          |          |          |       |         |           |           |             |
| **GeneratedHash**   | **AllUpper**   | **86.37 ns** | **0.901 ns** | **1.263 ns** | **83.78 ns** | **88.73 ns** | **87.53 ns** |  **1.00** |    **0.02** |   **3,324 B** |         **-** |          **NA** |
| PlainSpanSwitch | AllUpper   | 78.06 ns | 1.100 ns | 1.542 ns | 75.91 ns | 82.37 ns | 79.40 ns |  0.90 |    0.02 |   2,248 B |         - |          NA |
