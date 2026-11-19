# MEM-02: Class-element array vs struct-element array (+ref access)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - all three forms stay within 3.5% on both generations with identical code sizes

### StructArrayRefBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ClassArray | 403.1 ns | 402.3 ns | 1.00 | - → - | 52 B → 52 B |
| StructArrayCopy | 415.8 ns | 416.3 ns | 1.00 | - → - | 70 B → 70 B |
| StructArrayRef | 412.1 ns | 411.8 ns | 1.00 | - → - | 64 B → 64 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.7 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method          | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ClassArray      | .NET 10.0           | .NET 10.0 | 403.1 ns | 0.51 ns | 0.74 ns | 401.4 ns | 404.6 ns | 403.8 ns |  1.00 |      52 B |         - |          NA |
| StructArrayCopy | .NET 10.0           | .NET 10.0 | 415.8 ns | 1.34 ns | 1.96 ns | 412.3 ns | 419.5 ns | 418.4 ns |  1.03 |      70 B |         - |          NA |
| StructArrayRef  | .NET 10.0           | .NET 10.0 | 412.1 ns | 1.43 ns | 2.05 ns | 409.1 ns | 417.1 ns | 414.6 ns |  1.02 |      64 B |         - |          NA |
|                 |                     |           |          |         |         |          |          |          |       |           |           |             |
| ClassArray      | MediumRun-.NET 11.0 | .NET 11.0 | 402.3 ns | 0.70 ns | 1.03 ns | 400.4 ns | 404.5 ns | 403.5 ns |  1.00 |      52 B |         - |          NA |
| StructArrayCopy | MediumRun-.NET 11.0 | .NET 11.0 | 416.3 ns | 1.52 ns | 2.08 ns | 413.5 ns | 420.3 ns | 419.1 ns |  1.03 |      70 B |         - |          NA |
| StructArrayRef  | MediumRun-.NET 11.0 | .NET 11.0 | 411.8 ns | 0.99 ns | 1.38 ns | 408.9 ns | 414.1 ns | 413.6 ns |  1.02 |      64 B |         - |          NA |

## Earlier record

- Verdict: adopted (structural memory win; time-neutral in this micro)
- ClassArray 401.6 ns ~= StructArrayRef 412.9 ns ~= StructArrayCopy 414.7 ns (within ~3%; a 16-byte element copy is effectively free at this size)
- The locality/GC win of structs does not show here because class elements were allocated sequentially (allocation order == access order); in aged heaps class arrays scatter
- Structural: 1024 elements = 16 KB contiguous (struct) vs ~40 KB objects + 8 KB reference array (class) - less memory, fewer GC-tracked references

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ClassArray      | 401.6 ns | 0.64 ns | 0.89 ns | 399.9 ns | 403.1 ns | 402.8 ns |  1.00 |      52 B |         - |          NA |
| StructArrayCopy | 414.7 ns | 1.67 ns | 2.39 ns | 411.4 ns | 420.9 ns | 417.3 ns |  1.03 |      70 B |         - |          NA |
| StructArrayRef  | 412.9 ns | 3.45 ns | 4.94 ns | 408.3 ns | 431.0 ns | 419.1 ns |  1.03 |      64 B |         - |          NA |
