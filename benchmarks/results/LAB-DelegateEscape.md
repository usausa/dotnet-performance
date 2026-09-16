# LAB: delegate allocation under .NET 10 escape analysis (DSP-04 follow-up)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*DelegateEscapeBenchmark*"` and replace the tables below. Verdicts rest on generated code and allocation counts, which do not depend on the machine.
>
> ❗ **想定外 / unexpected** — this result contradicts (or goes beyond) the source article's claim. The verdict written below is provisional; decide adoption or rejection only after the HX 370 run confirms or overturns it.

- Verdict: the .NET 10 post's delegate stack allocation did NOT reproduce here. A lambda capturing a loop-scoped variable still allocates 88 B per iteration (5,632 B = 64 x 88: 24 B display class + 64 B delegate)
- A lambda capturing a method-scoped variable is cached by Roslyn in the display class: 96 B per call regardless of iteration count
- static lambda + explicit state: 0 B in every shape (compiler-cached delegate). Time differences between the 0 B and 96 B forms are within noise on this machine
- DSP-04's guidance stands unchanged: reach for static + TState; do not count on the JIT to remove the closure

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| CapturingLocal     |  70.71 ns |  6.005 ns |  8.989 ns |  54.37 ns |  85.55 ns |  79.67 ns |  1.02 |    0.19 |     173 B | 0.0057 |      96 B |        1.00 |
| StaticWithState    |  51.68 ns |  6.144 ns |  9.196 ns |  34.43 ns |  66.84 ns |  62.61 ns |  0.74 |    0.16 |     170 B |      - |         - |        0.00 |
| CapturingLoopLocal | 487.30 ns | 26.779 ns | 40.082 ns | 397.88 ns | 556.63 ns | 530.56 ns |  7.01 |    1.09 |     161 B | 0.3366 |    5632 B |       58.67 |
| CapturingEscaped   | 308.80 ns | 22.812 ns | 34.144 ns | 252.02 ns | 369.61 ns | 351.95 ns |  4.44 |    0.77 |     194 B | 0.0057 |      96 B |        1.00 |
