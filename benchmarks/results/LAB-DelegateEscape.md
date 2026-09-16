# LAB: delegate allocation under .NET 10 escape analysis (DSP-04 follow-up)

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
