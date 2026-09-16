# LAB: IEnumerable<T> enumeration under .NET 10 conditional escape analysis (STK-03 follow-up)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*EnumerableEscapeBenchmark*"` and replace the tables below. Verdicts rest on generated code and allocation counts, which do not depend on the machine.

- Verdict: on .NET 10 with dynamic PGO, foreach over an IEnumerable<int> field that always holds an int[] allocates nothing (0 B) and runs at array speed (324 vs 311 ns); the enumerator is stack-allocated
- When the concrete type alternates (int[] / List<int>) PGO cannot commit: 3,282 ns and 36 B per call, about 10x — the interface dispatch per element is back
- The STK-03 struct enumerator stays at 0 B regardless of PGO (417 ns, 55 B of code); the JIT path costs 206 B of code for the guarded devirtualization
- STK-03's allocation argument is therefore conditional on .NET 10: it still holds for polymorphic call sites and for code that must not depend on PGO

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev      | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|------------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| ArrayDirect      |   311.0 ns |  49.81 ns |    73.02 ns |   261.6 ns |   228.0 ns |   452.4 ns |   404.4 ns |  1.05 |    0.34 |      32 B |         - |          NA |
| InterfaceStatic  |   324.0 ns |   8.01 ns |    11.99 ns |   328.1 ns |   305.7 ns |   342.3 ns |   338.5 ns |  1.10 |    0.24 |     206 B |         - |          NA |
| InterfaceMixed   | 3,282.5 ns | 694.66 ns | 1,039.74 ns | 3,053.2 ns | 2,206.5 ns | 4,933.1 ns | 4,694.2 ns | 11.11 |    4.30 |     660 B |      36 B |          NA |
| StructEnumerator |   417.4 ns |  48.11 ns |    72.02 ns |   434.0 ns |   312.1 ns |   532.7 ns |   517.8 ns |  1.41 |    0.39 |      55 B |         - |          NA |
