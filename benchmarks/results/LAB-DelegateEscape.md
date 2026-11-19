# LAB: delegate allocation under .NET 10 escape analysis (DSP-04 follow-up)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the capture is still not stack-allocated on net11. Every capturing row shrinks by 8 B only (96 → 88 B, 5,632 → 5,120 B over 64 iterations), from the smaller delegate (#99200)
- CapturingLocal reads 1.25x on net11 (25.2 → 31.6 ns) with the same 173 B of code; not attributed to the generation without a second process

### DelegateEscapeBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| CapturingLocal | 25.21 ns | 31.62 ns | 1.25 | 96 B → 88 B | 173 B → 173 B |
| StaticWithState | 23.80 ns | 23.92 ns | 1.01 | - → - | 170 B → 170 B |
| CapturingLoopLocal | 256.90 ns | 251.57 ns | 0.98 | 5632 B → 5120 B | 161 B → 161 B |
| CapturingEscaped | 127.80 ns | 129.56 ns | 1.01 | 96 B → 88 B | 194 B → 194 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.56 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| CapturingLocal     | .NET 10.0           | .NET 10.0 |  25.21 ns | 0.073 ns | 0.109 ns |  25.03 ns |  25.50 ns |  25.32 ns |  1.00 |    0.00 | 0.0115 |     173 B |      96 B |        1.00 |
| StaticWithState    | .NET 10.0           | .NET 10.0 |  23.80 ns | 0.082 ns | 0.118 ns |  23.64 ns |  24.12 ns |  23.96 ns |  0.94 |    0.01 |      - |     170 B |         - |        0.00 |
| CapturingLoopLocal | .NET 10.0           | .NET 10.0 | 256.90 ns | 1.462 ns | 2.142 ns | 252.12 ns | 260.70 ns | 259.02 ns | 10.19 |    0.09 | 0.6733 |     161 B |    5632 B |       58.67 |
| CapturingEscaped   | .NET 10.0           | .NET 10.0 | 127.80 ns | 0.679 ns | 0.996 ns | 124.70 ns | 129.08 ns | 128.86 ns |  5.07 |    0.04 | 0.0114 |     194 B |      96 B |        1.00 |
|                    |                     |           |           |          |          |           |           |           |       |         |        |           |           |             |
| CapturingLocal     | MediumRun-.NET 11.0 | .NET 11.0 |  31.62 ns | 0.186 ns | 0.278 ns |  31.10 ns |  32.29 ns |  31.93 ns |  1.00 |    0.00 | 0.0105 |     173 B |      88 B |        1.00 |
| StaticWithState    | MediumRun-.NET 11.0 | .NET 11.0 |  23.92 ns | 0.076 ns | 0.109 ns |  23.75 ns |  24.14 ns |  24.08 ns |  0.76 |    0.01 |      - |     170 B |         - |        0.00 |
| CapturingLoopLocal | MediumRun-.NET 11.0 | .NET 11.0 | 251.57 ns | 2.706 ns | 3.966 ns | 246.36 ns | 262.14 ns | 255.98 ns |  7.96 |    0.14 | 0.6118 |     161 B |    5120 B |       58.18 |
| CapturingEscaped   | MediumRun-.NET 11.0 | .NET 11.0 | 129.56 ns | 0.724 ns | 1.062 ns | 127.67 ns | 131.49 ns | 130.99 ns |  4.10 |    0.05 | 0.0105 |     194 B |      88 B |        1.00 |

## Earlier record

- Verdict: the .NET 10 post's delegate stack allocation did NOT reproduce. A lambda capturing a loop-scoped variable still allocates 88 B per iteration (5,632 B = 64 x 88: 24 B display class + 64 B delegate), and the disassembly keeps two `CORINFO_HELP_NEWSFAST` calls per iteration
- Confirmed on the reference machine (x86-64-v4) after the B550H (x86-64-v3) provisional run: identical allocation counts (96 / 0 / 5,632 / 96 B) and code sizes (173 / 170 / 161 / 194 B) on both, so the result is a JIT fact, not a machine artifact
- A lambda capturing a method-scoped variable is cached by Roslyn in the display class: 96 B per call regardless of iteration count (31.4 ns)
- static lambda + explicit state: 0 B in every shape (compiler-cached delegate), 23.3 ns = 0.74x of the capturing form. The loop-scoped capture costs 8.18x (257.0 ns) and the escaped delegate 3.80x (119.4 ns)
- DSP-04's guidance stands unchanged: reach for static + TState; do not count on the JIT to remove the closure

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| CapturingLocal     |  31.42 ns | 0.307 ns | 0.430 ns |  30.62 ns |  32.50 ns |  31.86 ns |  1.00 |    0.02 | 0.0114 |     173 B |      96 B |        1.00 |
| StaticWithState    |  23.32 ns | 0.240 ns | 0.337 ns |  23.05 ns |  24.36 ns |  23.89 ns |  0.74 |    0.01 |      - |     170 B |         - |        0.00 |
| CapturingLoopLocal | 257.00 ns | 2.890 ns | 3.859 ns | 245.17 ns | 262.66 ns | 261.72 ns |  8.18 |    0.16 | 0.6733 |     161 B |    5632 B |       58.67 |
| CapturingEscaped   | 119.41 ns | 0.763 ns | 1.118 ns | 117.64 ns | 121.26 ns | 120.91 ns |  3.80 |    0.06 | 0.0114 |     194 B |      96 B |        1.00 |
