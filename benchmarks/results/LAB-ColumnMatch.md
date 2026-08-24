# LAB-ColumnMatch: normalizing the probe vs matching case-insensitively (TXT-10 counter-case)

- Verdict: **converting the probe never pays - it loses in all 12 conditions measured.** The un-converted
  ordinal switch is a viable generated form at 24 columns; at 8 columns this benchmark cannot decide it
- Shape: resolve a DB reader's column names to ordinals under SQL identifier rules (OrdinalIgnoreCase). The
  generated form is a guarded `Equals(OrdinalIgnoreCase)` chain at 8 columns and a sampling-hash switch at 24;
  the alternative is to upper-case the probe first so a plain ordinal switch can be used
- Measured on x86-64-v4 (Zen 5, .NET 10.0.11): 4 classes x 5 variants x 2 casings = 40 cases. The 24-column
  snake_case rows were re-measured after an unstable first run (100.93 ns +/- 11.37 did not reproduce: 84.42
  +/- 0.82)
- **The headline is not "IgnoreCase is expensive".** `string.Equals(name, "Id", OrdinalIgnoreCase)` is the
  cheapest thing here. What costs is the *conversion* introduced to avoid needing it

## Measurement validity: which comparisons this benchmark supports

**Only the baseline is inlined.** `GeneratedChain` / `GeneratedHash` compile to a 2-instruction tail call into a
fully inlined body (210 instructions / 989 B for the 8-column chain). The four alternatives go through
`ResolveByMatcher(names, static x => ...)`, paying a **`Func<string,int>` indirect call per column** plus a
non-inlined call into the matcher (`MatchXxxLabels`, 119 instructions / 503-563 B); each call site is
80 instructions / 293 B.

| Comparison | Valid? |
|---|---|
| One alternative against another (= what the conversion costs) | ✅ all four share the same harness |
| Baseline vs an alternative at 24 columns **where the alternative wins** | ✅ the handicap works against the winner, so the win is a lower bound |
| Baseline vs an alternative at 8 columns | ❌ the 3.0-3.6x for the un-converted switch cannot be separated from the 8 delegate calls plus 8 non-inlined calls it pays |

**`PlainSpanSwitch` resolves nothing under `AllUpper`** - it has no case handling, and Verify only checks it
under `AsDeclared`. Those rows look fast because they bail out early; they are excluded from every comparison
below.

## 1. What the conversion costs (same harness, `AsDeclared`)

| Columns / naming | `PlainSpanSwitch` (no conversion) | `Ascii.ToUpper` + span switch | `ToUpperInvariant` + span switch | `string.ToUpperInvariant()` + string switch |
|---|---:|---:|---:|---:|
| 8 / PascalCase | 27.39 ns | 48.01 (**1.75x**) | 51.31 (1.87x) | 75.95 (2.77x) + 304 B |
| 8 / snake_case | 26.48 ns | 47.98 (**1.81x**) | 52.80 (1.99x) | 81.96 (3.10x) + 328 B |
| 24 / PascalCase | 77.85 ns | 154.40 (**1.98x**) | 165.88 (2.13x) | 247.99 (3.19x) + 976 B |
| 24 / snake_case | 84.42 ns (run 2) | 169.95 (**2.01x**) | 171.18 (2.03x) | 244.44 (2.90x) + 1,008 B |

- **The conversion costs 1.75-2.13x of the match itself** when done over a span, and 2.77-3.19x plus an
  allocation when done through `string.ToUpperInvariant()`
- The ASCII-specialised conversion is the cheapest of the three in every condition, consistent with TXT-06
- `string.ToUpperInvariant()` allocates only when the input is not already upper case: 304-1,008 B under
  `AsDeclared`, **zero** under `AllUpper`, where the BCL returns the same instance. That is also why its
  `AllUpper` timings (118-125 ns at 24 columns) are so much better than its `AsDeclared` ones

## 2. Against the generated form: every converted variant loses

| Columns | Baseline | Converted variants |
|---|---:|---|
| 8 | `GeneratedChain` 7.60 / 8.73 ns | **4.5-10.0x** |
| 24 | `GeneratedHash` 85.18 / 85.23 ns | **1.35-2.91x** |

12 of 12 conditions (3 conversion forms x 4 classes) lose, and the harness handicap is not what decides it -
the margins are far larger than the ~20 ns of delegate and call overhead it can explain.

## 3. Can the un-converted ordinal switch replace the generated form?

| Columns / naming | Generated | `PlainSpanSwitch` | Verdict |
|---|---:|---:|---|
| 24 / PascalCase | 85.18 ns | **77.85 ns (0.91x)** | CIs disjoint - a win, and a lower bound |
| 24 / snake_case | 86.59 ns | 84.42 ns (0.98x) | CIs overlap - a tie |
| 8 / PascalCase | 7.60 ns | 27.39 ns (3.61x) | not decidable here (harness) |
| 8 / snake_case | 8.73 ns | 26.48 ns (3.03x) | not decidable here (harness) |

At 24 columns the plain switch also carries **1/1.5 the code**: 2,212-2,241 B against 3,254-3,294 B.

## What to weigh

1. **If the reader's spelling is guaranteed to match the baked literals, emit a plain ordinal switch.** At 24
   columns it is at least as fast as the sampling-hash switch while paying a handicap the generated form does
   not, and its code is a third smaller. This is the same conclusion TXT-10 reaches for fixed key sets
2. **If case-insensitive matching is required, keep the comparison case-insensitive.** Do not convert the probe
   to make a case-sensitive switch usable: that costs 1.75-2.13x of the match, or 2.77-3.19x plus an allocation,
   and loses to the generated form in every condition measured
3. **When a conversion is genuinely unavoidable, use the ASCII-specialised one** (`Ascii.ToUpper` into a
   stack buffer). It is the cheapest of the three everywhere, and `string.ToUpperInvariant()` is the only form
   that allocates
4. **The column count decides the generated *shape*, not this question.** Chain at 8, sampling hash at 24, plain
   switch when case sensitivity is acceptable - the "do not normalise first" rule holds across all of them
5. **To settle the 8-column case, the benchmark needs a delegate-dispatched baseline** (a `GeneratedChain`
   variant reached through `ResolveByMatcher`) so that all five variants pay the same call overhead. Until then
   the 8-column baseline column is a reference point, not a comparison

## x86-64-v4 - 8 columns, PascalCase

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
| **GeneratedChain**       | **AsDeclared** |  **7.601 ns** | **0.2226 ns** | **0.3262 ns** |  **7.235 ns** |  **8.328 ns** |  **8.096 ns** |  **1.00** |    **0.06** |     **999 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 75.949 ns | 1.9579 ns | 2.6138 ns | 73.050 ns | 80.950 ns | 79.521 ns | 10.01 |    0.53 |   2,521 B | 0.0362 |     304 B |          NA |
| UpperSpanSwitch      | AsDeclared | 51.308 ns | 0.3192 ns | 0.4578 ns | 50.343 ns | 52.146 ns | 51.817 ns |  6.76 |    0.28 |   1,800 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 48.007 ns | 0.7720 ns | 1.1072 ns | 46.820 ns | 50.739 ns | 49.985 ns |  6.33 |    0.29 |   1,576 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 27.387 ns | 0.5527 ns | 0.8101 ns | 26.635 ns | 30.131 ns | 28.667 ns |  3.61 |    0.18 |     928 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **AllUpper**   |  **7.756 ns** | **0.1744 ns** | **0.2611 ns** |  **7.483 ns** |  **8.504 ns** |  **8.154 ns** |  **1.00** |    **0.05** |   **1,002 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 39.475 ns | 2.6834 ns | 3.9334 ns | 36.739 ns | 53.837 ns | 45.231 ns |  5.09 |    0.53 |   2,186 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 51.356 ns | 1.1889 ns | 1.7427 ns | 48.871 ns | 56.333 ns | 52.555 ns |  6.63 |    0.31 |   1,787 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 46.678 ns | 1.1256 ns | 1.6144 ns | 44.447 ns | 49.367 ns | 48.441 ns |  6.02 |    0.28 |   1,579 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   | 25.868 ns | 0.1278 ns | 0.1706 ns | 25.479 ns | 26.289 ns | 26.045 ns |  3.34 |    0.11 |     921 B |      - |         - |          NA |

## x86-64-v4 - 8 columns, snake_case

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
| **GeneratedChain**       | **AsDeclared** |  **8.734 ns** | **0.1036 ns** | **0.1518 ns** |  **8.415 ns** |  **9.135 ns** |  **8.897 ns** |  **1.00** |    **0.02** |   **1,024 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AsDeclared | 81.963 ns | 3.0924 ns | 4.4350 ns | 75.946 ns | 88.759 ns | 87.502 ns |  9.39 |    0.52 |   2,506 B | 0.0391 |     328 B |          NA |
| UpperSpanSwitch      | AsDeclared | 52.798 ns | 0.3699 ns | 0.5537 ns | 51.844 ns | 54.038 ns | 53.510 ns |  6.05 |    0.12 |   1,786 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AsDeclared | 47.978 ns | 0.2384 ns | 0.3494 ns | 47.443 ns | 48.573 ns | 48.350 ns |  5.49 |    0.10 |   1,583 B |      - |         - |          NA |
| PlainSpanSwitch      | AsDeclared | 26.481 ns | 0.2744 ns | 0.4022 ns | 25.014 ns | 27.077 ns | 26.873 ns |  3.03 |    0.07 |     940 B |      - |         - |          NA |
|                      |            |           |           |           |           |           |           |       |         |           |        |           |             |
| **GeneratedChain**       | **AllUpper**   |  **8.369 ns** | **0.1361 ns** | **0.1995 ns** |  **7.959 ns** |  **8.722 ns** |  **8.567 ns** |  **1.00** |    **0.03** |   **1,024 B** |      **-** |         **-** |          **NA** |
| UpperStringSwitch    | AllUpper   | 37.696 ns | 0.7037 ns | 1.0314 ns | 35.160 ns | 40.390 ns | 38.599 ns |  4.51 |    0.16 |   2,199 B |      - |         - |          NA |
| UpperSpanSwitch      | AllUpper   | 54.967 ns | 0.8691 ns | 1.3008 ns | 52.478 ns | 57.700 ns | 56.632 ns |  6.57 |    0.22 |   1,786 B |      - |         - |          NA |
| AsciiUpperSpanSwitch | AllUpper   | 50.075 ns | 0.4165 ns | 0.6234 ns | 49.043 ns | 51.864 ns | 50.898 ns |  5.99 |    0.16 |   1,582 B |      - |         - |          NA |
| PlainSpanSwitch      | AllUpper   | 25.372 ns | 0.3167 ns | 0.4440 ns | 24.777 ns | 26.508 ns | 25.909 ns |  3.03 |    0.09 |     958 B |      - |         - |          NA |

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
