# LAB: IEnumerable<T> enumeration under .NET 10 conditional escape analysis (STK-03 follow-up)

- Verdict: on .NET 10 with dynamic PGO, foreach over an IEnumerable<int> field that always holds an int[] allocates nothing (0 B) and runs at 1.09x of the array (234 vs 214 ns); the enumerator is stack-allocated
- When the concrete type alternates (int[] / List<int>) PGO cannot commit: 1,904 ns and 36 B per call, about 9x — the interface dispatch per element is back
- The STK-03 struct enumerator stays at 0 B regardless of PGO (218 ns, 1.02x of the array, 55 B of code); the JIT path costs 206 B of code for the guarded devirtualization
- STK-03's allocation argument is therefore conditional on .NET 10: it still holds for polymorphic call sites and for code that must not depend on PGO
- Same allocation counts and code sizes as the B550H (x86-64-v3) provisional run (0 / 0 / 36 / 0 B; 32 / 206 / 660→657 / 55 B), so the verdict does not depend on the machine

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|----------:|------------:|
| ArrayDirect      |   213.9 ns |  1.10 ns |  1.62 ns |   211.6 ns |   218.8 ns |   215.6 ns |  1.00 |    0.01 |      - |      32 B |         - |          NA |
| InterfaceStatic  |   233.8 ns |  1.40 ns |  1.86 ns |   231.3 ns |   240.8 ns |   235.1 ns |  1.09 |    0.01 |      - |     206 B |         - |          NA |
| InterfaceMixed   | 1,903.7 ns | 20.31 ns | 29.13 ns | 1,856.0 ns | 1,969.1 ns | 1,943.1 ns |  8.90 |    0.15 | 0.0038 |     657 B |      36 B |          NA |
| StructEnumerator |   218.3 ns |  0.94 ns |  1.28 ns |   216.3 ns |   221.1 ns |   220.0 ns |  1.02 |    0.01 |      - |      55 B |         - |          NA |
