# DSP-02: Call abstraction overhead (1024 calls, net10)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - hold the concrete sealed type; the function pointer stays the slowest option (4.65x on net10, 5.58x on net11)
- ViaInterface reads 1.11x on net11 (221.3 → 245.8 ns) with the same 84 B of code; attributed to placement (pitfall 10). The abstract call is unchanged at 1.00x

### CallAbstractionBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DirectSealed | 221.6 ns | 221.1 ns | 1.00 | - → - | 27 B → 27 B |
| ViaInterface | 221.3 ns | 245.8 ns | 1.11 | - → - | 84 B → 84 B |
| ViaAbstract | 219.9 ns | 220.2 ns | 1.00 | - → - | 81 B → 81 B |
| ViaDelegate | 254.9 ns | 264.8 ns | 1.04 | - → - | 85 B → 85 B |
| ViaFunctionPointer | 1,030.6 ns | 1,233.4 ns | 1.20 | - → - | 42 B → 42 B |

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
| Method             | Job                 | Runtime   | Mean       | Error   | StdDev  | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |-----------:|--------:|--------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DirectSealed       | .NET 10.0           | .NET 10.0 |   221.6 ns | 0.52 ns | 0.76 ns |   221.5 ns |   220.2 ns |   223.9 ns |   222.7 ns |  1.00 |    0.00 |      27 B |         - |          NA |
| ViaInterface       | .NET 10.0           | .NET 10.0 |   221.3 ns | 1.81 ns | 2.47 ns |   220.7 ns |   218.1 ns |   227.9 ns |   224.5 ns |  1.00 |    0.01 |      84 B |         - |          NA |
| ViaAbstract        | .NET 10.0           | .NET 10.0 |   219.9 ns | 0.80 ns | 1.12 ns |   220.0 ns |   217.1 ns |   222.2 ns |   221.2 ns |  0.99 |    0.01 |      81 B |         - |          NA |
| ViaDelegate        | .NET 10.0           | .NET 10.0 |   254.9 ns | 1.18 ns | 1.66 ns |   254.6 ns |   252.6 ns |   259.7 ns |   256.7 ns |  1.15 |    0.01 |      85 B |         - |          NA |
| ViaFunctionPointer | .NET 10.0           | .NET 10.0 | 1,030.6 ns | 2.71 ns | 3.80 ns | 1,030.9 ns | 1,024.7 ns | 1,039.9 ns | 1,035.5 ns |  4.65 |    0.02 |      42 B |         - |          NA |
|                    |                     |           |            |         |         |            |            |            |            |       |         |           |           |             |
| DirectSealed       | MediumRun-.NET 11.0 | .NET 11.0 |   221.1 ns | 0.43 ns | 0.59 ns |   221.0 ns |   220.0 ns |   222.3 ns |   221.7 ns |  1.00 |    0.00 |      27 B |         - |          NA |
| ViaInterface       | MediumRun-.NET 11.0 | .NET 11.0 |   245.8 ns | 6.56 ns | 9.41 ns |   240.4 ns |   234.0 ns |   258.6 ns |   257.2 ns |  1.11 |    0.04 |      84 B |         - |          NA |
| ViaAbstract        | MediumRun-.NET 11.0 | .NET 11.0 |   220.2 ns | 1.44 ns | 2.02 ns |   219.7 ns |   217.7 ns |   225.2 ns |   223.0 ns |  1.00 |    0.01 |      81 B |         - |          NA |
| ViaDelegate        | MediumRun-.NET 11.0 | .NET 11.0 |   264.8 ns | 6.76 ns | 9.91 ns |   270.4 ns |   252.5 ns |   276.8 ns |   276.3 ns |  1.20 |    0.04 |      85 B |         - |          NA |
| ViaFunctionPointer | MediumRun-.NET 11.0 | .NET 11.0 | 1,233.4 ns | 4.93 ns | 6.91 ns | 1,231.4 ns | 1,223.6 ns | 1,254.6 ns | 1,241.2 ns |  5.58 |    0.03 |      42 B |         - |          NA |

## Earlier record

- Verdict: hold the concrete sealed type where possible; function pointers are NOT the fast option on net10
- DirectSealed 215.8 ns (1.00, 27 B - inlined) | ViaAbstract 223.6 ns (1.04) | ViaInterface 224.3 ns (1.04) | ViaDelegate 254.6 ns (1.18) | ViaFunctionPointer 1,250.7 ns (5.80x SLOWEST)
- Interface and abstract dispatch cost only ~4% here - a monomorphic virtual call predicts and speculates well; the delegate's 1.18x is the largest of the ordinary options
- Why the function pointer loses: calli cannot be inlined and PGO cannot speculate on it, while a delegate's Invoke gets guarded devirtualization + inlining of the target - the one real cliff in this table
- Delegate ~= abstract ~= interface: the old 'delegates are heavier than interfaces' rule does not hold
- Function pointers remain for interop/AOT boundaries and megamorphic targets where PGO speculation would fail anyway - not as a general speed tool

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DirectSealed       |   215.8 ns |  0.99 ns |  1.39 ns |   214.4 ns |   219.9 ns |   217.8 ns |  1.00 |    0.01 |      27 B |         - |          NA |
| ViaInterface       |   224.3 ns |  2.22 ns |  3.26 ns |   219.5 ns |   233.7 ns |   228.9 ns |  1.04 |    0.02 |      84 B |         - |          NA |
| ViaAbstract        |   223.6 ns |  1.18 ns |  1.54 ns |   220.5 ns |   225.9 ns |   225.7 ns |  1.04 |    0.01 |      81 B |         - |          NA |
| ViaDelegate        |   254.6 ns |  2.16 ns |  2.95 ns |   251.6 ns |   263.5 ns |   258.8 ns |  1.18 |    0.02 |      85 B |         - |          NA |
| ViaFunctionPointer | 1,250.7 ns | 30.21 ns | 42.36 ns | 1,225.8 ns | 1,379.3 ns | 1,302.3 ns |  5.80 |    0.20 |      42 B |         - |          NA |

## NativeAOT comparison (net10.0 vs NativeAOT 10.0, same MediumRun settings)

- **The delegate / function-pointer ordering reverses.** Under JIT the delegate is 1.21x and the function pointer 5.00x; under AOT the delegate is **5.62x** and the function pointer **4.82x**, so the function pointer becomes the faster of the two
- The function pointer barely moves in absolute terms (1,111 -> 1,090 ns): it never benefited from the JIT in the first place. What changes is the **delegate losing its guarded devirtualization + inlining** - 269.5 -> 1,270.7 ns, a 4.7x absolute regression
- Interface and abstract dispatch roughly double their overhead: 1.06x -> 1.30x / 1.31x
- Holding the concrete sealed type is unchanged and remains the answer on both runtimes (222.0 -> 226.3 ns absolute)
- This revises the JIT-only guidance above: "function pointers are not the fast option" holds **on net10 JIT**. On NativeAOT they are no longer the cliff - the delegate is

Ratios below are BDN's, all against `DirectSealed` on **.NET 10.0**. Within NativeAOT, against its own `DirectSealed` (226.3 ns): ViaInterface 1.30x, ViaAbstract 1.31x, ViaDelegate **5.62x**, ViaFunctionPointer **4.82x**.

Run without `DisassemblyDiagnoser` (unsupported on NativeAOT), so no Code Size column. See [benchmark-methodology.md](../../docs/benchmark-methodology.md) for the procedure.

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]         : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  .NET 10.0      : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  NativeAOT 10.0 : .NET 10.0.10, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job            | Runtime        | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |--------------- |--------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| DirectSealed       | .NET 10.0      | .NET 10.0      |   222.0 ns |  0.60 ns |  0.89 ns |   220.8 ns |   224.2 ns |   222.8 ns |  1.00 |    0.01 |         - |          NA |
| ViaInterface       | .NET 10.0      | .NET 10.0      |   235.2 ns |  1.50 ns |  2.14 ns |   231.0 ns |   240.0 ns |   237.8 ns |  1.06 |    0.01 |         - |          NA |
| ViaAbstract        | .NET 10.0      | .NET 10.0      |   234.8 ns |  1.77 ns |  2.65 ns |   230.1 ns |   239.6 ns |   239.2 ns |  1.06 |    0.01 |         - |          NA |
| ViaDelegate        | .NET 10.0      | .NET 10.0      |   269.5 ns |  1.69 ns |  2.48 ns |   266.1 ns |   275.2 ns |   272.6 ns |  1.21 |    0.01 |         - |          NA |
| ViaFunctionPointer | .NET 10.0      | .NET 10.0      | 1,111.0 ns | 11.63 ns | 17.40 ns | 1,087.4 ns | 1,155.0 ns | 1,133.0 ns |  5.00 |    0.08 |         - |          NA |
| DirectSealed       | NativeAOT 10.0 | NativeAOT 10.0 |   226.3 ns |  0.70 ns |  0.95 ns |   224.4 ns |   229.1 ns |   227.3 ns |  1.02 |    0.01 |         - |          NA |
| ViaInterface       | NativeAOT 10.0 | NativeAOT 10.0 |   294.4 ns |  1.74 ns |  2.61 ns |   289.1 ns |   299.5 ns |   297.4 ns |  1.33 |    0.01 |         - |          NA |
| ViaAbstract        | NativeAOT 10.0 | NativeAOT 10.0 |   296.4 ns |  3.03 ns |  4.25 ns |   291.7 ns |   311.2 ns |   300.7 ns |  1.34 |    0.02 |         - |          NA |
| ViaDelegate        | NativeAOT 10.0 | NativeAOT 10.0 | 1,270.7 ns |  6.27 ns |  9.39 ns | 1,253.5 ns | 1,290.1 ns | 1,281.5 ns |  5.72 |    0.05 |         - |          NA |
| ViaFunctionPointer | NativeAOT 10.0 | NativeAOT 10.0 | 1,090.3 ns |  5.11 ns |  7.33 ns | 1,077.3 ns | 1,105.7 ns | 1,100.3 ns |  4.91 |    0.04 |         - |          NA |
