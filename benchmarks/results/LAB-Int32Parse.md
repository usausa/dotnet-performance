# LAB: Int32 decoding — byte-by-byte vs Span vs pointer (R-09 follow-up)

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
