# LAB: Int32 decoding — byte-by-byte vs Span vs pointer (R-09 follow-up)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged. `MemoryMarshal.Cast` + loop 0.30x, pointer 0.35x, per-element BinaryPrimitives 0.58x of byte-by-byte on net11 (0.31x / 0.35x / 0.58x on net10)

### Int32ParseBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ByteByByte | 718.0 ns | 709.1 ns | 0.99 | - → - | 200 B → 200 B |
| SpanPerElement | 413.0 ns | 414.4 ns | 1.00 | - → - | 85 B → 85 B |
| SpanCast | 219.9 ns | 216.0 ns | 0.98 | - → - | 54 B → 54 B |
| Pointer | 247.8 ns | 247.6 ns | 1.00 | - → - | 97 B → 97 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.33 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method         | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|--------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ByteByByte     | .NET 10.0           | .NET 10.0 | 718.0 ns | 4.28 ns | 6.27 ns | 705.2 ns | 729.4 ns | 727.1 ns |  1.00 |     200 B |         - |          NA |
| SpanPerElement | .NET 10.0           | .NET 10.0 | 413.0 ns | 0.78 ns | 1.09 ns | 411.4 ns | 416.1 ns | 414.4 ns |  0.58 |      85 B |         - |          NA |
| SpanCast       | .NET 10.0           | .NET 10.0 | 219.9 ns | 0.54 ns | 0.79 ns | 218.3 ns | 221.5 ns | 221.0 ns |  0.31 |      54 B |         - |          NA |
| Pointer        | .NET 10.0           | .NET 10.0 | 247.8 ns | 0.61 ns | 0.87 ns | 246.4 ns | 250.0 ns | 249.0 ns |  0.35 |      97 B |         - |          NA |
|                |                     |           |          |         |         |          |          |          |       |           |           |             |
| ByteByByte     | MediumRun-.NET 11.0 | .NET 11.0 | 709.1 ns | 1.60 ns | 2.25 ns | 705.9 ns | 713.7 ns | 711.9 ns |  1.00 |     200 B |         - |          NA |
| SpanPerElement | MediumRun-.NET 11.0 | .NET 11.0 | 414.4 ns | 2.31 ns | 3.17 ns | 411.2 ns | 425.1 ns | 418.2 ns |  0.58 |      85 B |         - |          NA |
| SpanCast       | MediumRun-.NET 11.0 | .NET 11.0 | 216.0 ns | 0.38 ns | 0.54 ns | 215.1 ns | 217.1 ns | 216.7 ns |  0.30 |      54 B |         - |          NA |
| Pointer        | MediumRun-.NET 11.0 | .NET 11.0 | 247.6 ns | 0.38 ns | 0.55 ns | 246.2 ns | 248.2 ns | 248.2 ns |  0.35 |      97 B |         - |          NA |

## Earlier record

- Verdict: MemoryMarshal.Cast<byte, int> + plain loop is the fastest form: 0.30x of byte-by-byte (215.2 ns, 54 B). fixed + int* is 0.35x (249.0 ns, 97 B) — the pointer is 1.16x slower than Cast, CIs disjoint (the B550H x86-64-v3 run gave 0.26x vs 0.52x, i.e. 2.0x)
- Why: the Cast inner loop is 5 instructions (`movsxd` load by byte offset, `add`, `add 4`, `dec`, `jne`); the pointer loop is 6 (`movsxd` of the int index on the dependent chain, `movsxd` load, `add`, `inc`, `cmp`, `jl`) plus the `fixed` stack frame. Neither is vectorized. The code sizes are identical on both machines
- BinaryPrimitives.ReadInt32LittleEndian per element is 0.51x (364.0 ns, 85 B). The NDepend article's "Span 0.26x" corresponds to the Cast form (0.26-0.30x across the two machines); its "pointer 0.19x" did not reproduce on either (0.35x / 0.52x)
- R-09 stands and is reinforced: reinterpret once with Cast, then index; do not reach for pointers

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method         | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|--------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ByteByByte     | 718.2 ns | 4.44 ns | 6.08 ns | 710.4 ns | 734.1 ns | 726.6 ns |  1.00 |     200 B |         - |          NA |
| SpanPerElement | 364.0 ns | 1.11 ns | 1.44 ns | 361.6 ns | 366.7 ns | 365.7 ns |  0.51 |      85 B |         - |          NA |
| SpanCast       | 215.2 ns | 0.64 ns | 0.88 ns | 214.4 ns | 218.2 ns | 216.1 ns |  0.30 |      54 B |         - |          NA |
| Pointer        | 249.0 ns | 1.13 ns | 1.54 ns | 247.1 ns | 253.1 ns | 251.1 ns |  0.35 |      97 B |         - |          NA |
