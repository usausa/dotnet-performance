# LAB: IEnumerable<T> enumeration under .NET 10 conditional escape analysis (STK-03 follow-up)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged. Every row matches between net10 and net11 within the intervals
- Monomorphic `IEnumerable<int>` field: 0 B and 1.05-1.06x of the array on both generations
- Alternating concrete types: 10.3x and 36 B on both generations
- Struct enumerator: 0 B and 0.99x on both generations

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.37 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Mean       | Error    | StdDev   | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|-------:|----------:|------------:|
| ArrayDirect      | .NET 10.0           | .NET 10.0 |   220.3 ns |  0.68 ns |  0.95 ns |   220.1 ns |   219.0 ns |   222.4 ns |   221.7 ns |  1.00 |    0.00 |      32 B |      - |         - |          NA |
| InterfaceStatic  | .NET 10.0           | .NET 10.0 |   232.2 ns |  0.47 ns |  0.65 ns |   232.3 ns |   231.3 ns |   234.0 ns |   232.9 ns |  1.05 |    0.01 |     206 B |      - |         - |          NA |
| InterfaceMixed   | .NET 10.0           | .NET 10.0 | 2,274.3 ns | 34.82 ns | 51.04 ns | 2,247.4 ns | 2,213.2 ns | 2,350.2 ns | 2,332.4 ns | 10.32 |    0.23 |     750 B | 0.0038 |      36 B |          NA |
| StructEnumerator | .NET 10.0           | .NET 10.0 |   217.7 ns |  0.31 ns |  0.42 ns |   217.7 ns |   216.8 ns |   218.5 ns |   218.2 ns |  0.99 |    0.00 |      55 B |      - |         - |          NA |
|                  |                     |           |            |          |          |            |            |            |            |       |         |           |        |           |             |
| ArrayDirect      | MediumRun-.NET 11.0 | .NET 11.0 |   219.6 ns |  0.42 ns |  0.60 ns |   219.5 ns |   218.4 ns |   220.9 ns |   220.4 ns |  1.00 |    0.00 |      32 B |      - |         - |          NA |
| InterfaceStatic  | MediumRun-.NET 11.0 | .NET 11.0 |   232.3 ns |  0.71 ns |  1.01 ns |   232.1 ns |   230.6 ns |   235.2 ns |   233.3 ns |  1.06 |    0.01 |     206 B |      - |         - |          NA |
| InterfaceMixed   | MediumRun-.NET 11.0 | .NET 11.0 | 2,269.6 ns | 36.47 ns | 51.13 ns | 2,300.9 ns | 2,200.6 ns | 2,343.6 ns | 2,324.3 ns | 10.34 |    0.23 |     553 B | 0.0038 |      36 B |          NA |
| StructEnumerator | MediumRun-.NET 11.0 | .NET 11.0 |   217.8 ns |  0.66 ns |  0.92 ns |   217.8 ns |   216.3 ns |   220.8 ns |   218.7 ns |  0.99 |    0.00 |      55 B |      - |         - |          NA |

## Earlier record

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
