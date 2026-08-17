# DSP-01: sealed devirtualization (single-impl interface call)

- Verdict: conditional
- Via interface reference, sealed vs open measures equal (220.7 vs 221.9 ns, CIs overlap, code size 84 B both)
- Disassembly shows why: **PGO's guarded devirtualization already inlines the body behind a type guard on the interface path** - each iteration reloads the field and compares the method table (`cmp [rcx], MT`), and on match runs the inlined add; the guard predicts perfectly, so sealed adds nothing on top
- Concrete sealed reference (27 B): **the guard disappears entirely** - no per-iteration MT compare or field reload, one hoisted null check before the loop, and the body collapses to a 6-instruction tight loop. The measured ~2% (0.98x) is exactly the cost of that per-iteration guard
- Consequence under JIT: with dynamic PGO the interface path is nearly free. The follow-up prediction - that the concrete/sealed form "matters much more" without PGO - was later **measured under NativeAOT and not supported** (see the AOT section below); the concrete advantage shrinks here, and it is [DSP-02](DSP-02-CallAbstraction.md) that shows interface dispatch actually getting more expensive
- sealed remains free - keep it as the default, but do not expect interface-typed call sites to get faster from sealing alone on a PGO-enabled runtime

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| OpenInterface   | 220.7 ns | 1.43 ns | 1.96 ns | 218.0 ns | 224.8 ns | 223.5 ns |  1.00 |    0.01 |      84 B |         - |          NA |
| SealedInterface | 221.9 ns | 2.27 ns | 3.18 ns | 218.3 ns | 230.2 ns | 226.1 ns |  1.01 |    0.02 |      84 B |         - |          NA |
| SealedConcrete  | 215.2 ns | 0.53 ns | 0.75 ns | 214.1 ns | 217.3 ns | 215.8 ns |  0.98 |    0.01 |      27 B |         - |          NA |

## NativeAOT comparison (net10.0 vs NativeAOT 10.0, same MediumRun settings)

- **The prediction in the bullets above is not supported by measurement.** It said the concrete/sealed form "matters much more" under AOT. Measured, the concrete advantage **shrinks**: 0.92x under JIT, 0.96x under AOT
- Sealed *via an interface reference* remains worth nothing on both runtimes (0.99x JIT, 1.00x AOT). That part of the original conclusion stands
- Caveat on this specific benchmark: every `IAccumulator` implementation here has the **identical body** (`total + value`), and `DerivedAccumulator` does not override `Add`. ILC deduplicates identical method bodies, so the AOT interface path here may resolve to a single shared target and be unrepresentatively cheap
- [DSP-02](DSP-02-CallAbstraction.md) measures the same question with implementations whose bodies genuinely differ, and there interface dispatch costs **1.30x under AOT vs 1.06x under JIT** - i.e. it does get materially worse. The two benchmarks disagree (4.3% vs 30% AOT interface overhead), and `DisassemblyDiagnoser` has no NativeAOT support, so the disagreement could not be resolved from generated code
- **Trust DSP-02's number for "what does interface dispatch cost under AOT".** Treat this file's AOT row as the special case of an interface whose implementations are indistinguishable after compilation

Ratios below are BDN's, all against `OpenInterface` on **.NET 10.0**. Within NativeAOT, against its own `OpenInterface` (237.2 ns): SealedInterface 1.00x, SealedConcrete **0.96x**.

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
| Method          | Job            | Runtime        | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------- |--------------- |--------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| OpenInterface   | .NET 10.0      | .NET 10.0      | 238.6 ns | 3.38 ns | 4.96 ns | 232.5 ns | 252.1 ns | 243.9 ns |  1.00 |    0.03 |         - |          NA |
| SealedInterface | .NET 10.0      | .NET 10.0      | 236.5 ns | 2.79 ns | 4.09 ns | 231.9 ns | 246.4 ns | 242.7 ns |  0.99 |    0.03 |         - |          NA |
| SealedConcrete  | .NET 10.0      | .NET 10.0      | 219.3 ns | 1.43 ns | 2.10 ns | 216.6 ns | 223.2 ns | 221.7 ns |  0.92 |    0.02 |         - |          NA |
| OpenInterface   | NativeAOT 10.0 | NativeAOT 10.0 | 237.2 ns | 1.74 ns | 2.49 ns | 229.6 ns | 241.0 ns | 239.5 ns |  0.99 |    0.02 |         - |          NA |
| SealedInterface | NativeAOT 10.0 | NativeAOT 10.0 | 237.4 ns | 0.93 ns | 1.30 ns | 235.1 ns | 239.7 ns | 239.3 ns |  1.00 |    0.02 |         - |          NA |
| SealedConcrete  | NativeAOT 10.0 | NativeAOT 10.0 | 227.0 ns | 1.10 ns | 1.61 ns | 223.7 ns | 230.5 ns | 228.9 ns |  0.95 |    0.02 |         - |          NA |
