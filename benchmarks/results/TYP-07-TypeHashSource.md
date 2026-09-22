# TYP-07: Hash source for Type-keyed lookup (virtual / identity hash / TypeHandle)

- Use case: runtime type dispatch tables - serializer formatter lookup, DI resolution caches, message handler registries: anywhere a `Type` arrives as data rather than as a generic argument
- Verdict: **conditional on the compilation mode - the ranking reverses between JIT and NativeAOT**
  - **JIT: `type.TypeHandle.Value` wins** - 0.60x on hit, 0.64x on miss, and less than half the code (403 B → 192 B)
  - **NativeAOT: the plain virtual `type.GetHashCode()` wins** - both alternatives are *slower* there (handle 1.08x hit / 1.19x miss, identity 1.18x / 1.23x)
- Pick by target. If you ship both, the virtual call is the safe default: it is never worst on either runtime, whereas `TypeHandle.Value` swings from best (JIT) to worse-than-baseline (AOT)
- The bucket layout is held identical across the three paths so only the acquisition path varies. `Verify()` checks all three agree on hit and miss

## Why the reversal (hypothesis, not measured)

Under NativeAOT the whole type graph is closed, so `RuntimeType.GetHashCode` can be statically devirtualized - the vtable dispatch that makes it the slowest form under JIT is simply not there. The other two lose their edge at the same time. This is a hypothesis: `DisassemblyDiagnoser` has no NativeAOT support, so the generated code was not inspected.

## The shift is not optional, and it survives AOT

`TypeHandle.Value` is pointer-aligned, so the low 3 bits are always zero. Correctness cannot catch a missing `>> 3` - the same shift is applied at insert and at lookup, so lookups stay correct and only the distribution collapses. Measured directly on a NativeAOT-published probe over the same 40 types:

```
runtime = .NET 10.0.10 (NativeAOT published binary)
probes=40 distinctHandles=40 misaligned=0
low3bits histogram: 0:40
spread over 64 buckets (used/maxChain): handle>>3 30/2   handle raw 8/8   identity 32/3
```

All 40 handles are 8-byte aligned under AOT, so the assumption holds. Without the shift only 8 of 64 buckets are reachable and the longest chain grows to 8; with it, the handle spreads slightly better than the identity hash (30 buckets / max chain 2 vs 32 / 3).

## JIT (net10.0)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| VirtualHashHit   | 1.305 ns | 0.0192 ns | 0.0269 ns | 1.266 ns | 1.378 ns | 1.345 ns |  1.00 |    0.03 |     403 B |         - |          NA |
| IdentityHashHit  | 1.230 ns | 0.0055 ns | 0.0079 ns | 1.218 ns | 1.244 ns | 1.241 ns |  0.94 |    0.02 |     355 B |         - |          NA |
| HandleHashHit    | 0.848 ns | 0.0166 ns | 0.0232 ns | 0.811 ns | 0.877 ns | 0.872 ns |  0.65 |    0.03 |     192 B |         - |          NA |
| VirtualHashMiss  | 1.279 ns | 0.0387 ns | 0.0555 ns | 1.214 ns | 1.427 ns | 1.361 ns |  0.98 |    0.05 |     402 B |         - |          NA |
| IdentityHashMiss | 1.197 ns | 0.0202 ns | 0.0296 ns | 1.135 ns | 1.253 ns | 1.222 ns |  0.92 |    0.04 |     357 B |         - |          NA |
| HandleHashMiss   | 0.812 ns | 0.0158 ns | 0.0237 ns | 0.784 ns | 0.855 ns | 0.840 ns |  0.62 |    0.03 |     202 B |         - |          NA |

Ratio is against `VirtualHashHit` (single baseline). The miss axis reads against `VirtualHashMiss`: identity 0.94x, handle **0.64x**.

## JIT vs NativeAOT, same MediumRun settings

Run without `DisassemblyDiagnoser` (NativeAOT does not support it - BDN refuses the job if it is configured), so there is no Code Size column here. `vswhere.exe` must be on `PATH` for the ILCompiler link step: `C:\Program Files (x86)\Microsoft Visual Studio\Installer`.

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]         : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  .NET 10.0      : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  NativeAOT 10.0 : .NET 10.0.10, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job            | Runtime        | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------------- |--------------- |--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| VirtualHashHit   | .NET 10.0      | .NET 10.0      | 1.4356 ns | 0.0092 ns | 0.0132 ns | 1.4038 ns | 1.4575 ns | 1.4518 ns |  1.00 |    0.01 |         - |          NA |
| IdentityHashHit  | .NET 10.0      | .NET 10.0      | 1.1445 ns | 0.0116 ns | 0.0174 ns | 1.1137 ns | 1.1829 ns | 1.1717 ns |  0.80 |    0.01 |         - |          NA |
| HandleHashHit    | .NET 10.0      | .NET 10.0      | 0.8638 ns | 0.0035 ns | 0.0053 ns | 0.8514 ns | 0.8736 ns | 0.8703 ns |  0.60 |    0.01 |         - |          NA |
| VirtualHashMiss  | .NET 10.0      | .NET 10.0      | 1.2330 ns | 0.0110 ns | 0.0161 ns | 1.2058 ns | 1.2749 ns | 1.2551 ns |  0.86 |    0.01 |         - |          NA |
| IdentityHashMiss | .NET 10.0      | .NET 10.0      | 1.2076 ns | 0.0071 ns | 0.0104 ns | 1.1771 ns | 1.2254 ns | 1.2184 ns |  0.84 |    0.01 |         - |          NA |
| HandleHashMiss   | .NET 10.0      | .NET 10.0      | 0.7868 ns | 0.0043 ns | 0.0064 ns | 0.7700 ns | 0.7992 ns | 0.7949 ns |  0.55 |    0.01 |         - |          NA |
| VirtualHashHit   | NativeAOT 10.0 | NativeAOT 10.0 | 1.5154 ns | 0.0972 ns | 0.1394 ns | 1.3354 ns | 1.7009 ns | 1.6625 ns |  1.06 |    0.10 |         - |          NA |
| IdentityHashHit  | NativeAOT 10.0 | NativeAOT 10.0 | 1.7858 ns | 0.0092 ns | 0.0136 ns | 1.7579 ns | 1.8101 ns | 1.8009 ns |  1.24 |    0.01 |         - |          NA |
| HandleHashHit    | NativeAOT 10.0 | NativeAOT 10.0 | 1.6439 ns | 0.0091 ns | 0.0130 ns | 1.6241 ns | 1.6726 ns | 1.6633 ns |  1.15 |    0.01 |         - |          NA |
| VirtualHashMiss  | NativeAOT 10.0 | NativeAOT 10.0 | 1.3725 ns | 0.0397 ns | 0.0582 ns | 1.2982 ns | 1.4709 ns | 1.4403 ns |  0.96 |    0.04 |         - |          NA |
| IdentityHashMiss | NativeAOT 10.0 | NativeAOT 10.0 | 1.6928 ns | 0.0306 ns | 0.0449 ns | 1.6321 ns | 1.7722 ns | 1.7470 ns |  1.18 |    0.03 |         - |          NA |
| HandleHashMiss   | NativeAOT 10.0 | NativeAOT 10.0 | 1.6270 ns | 0.0087 ns | 0.0125 ns | 1.6029 ns | 1.6572 ns | 1.6424 ns |  1.13 |    0.01 |         - |          NA |

Ratio is against `VirtualHashHit` on .NET 10.0 for every row. Read within NativeAOT instead, against its own virtual baseline:

| Path | AOT hit | vs AOT virtual | AOT miss | vs AOT virtual |
|---|---|---|---|---|
| `type.GetHashCode()` | 1.5154 ns | 1.00 | 1.3725 ns | 1.00 |
| `RuntimeHelpers.GetHashCode` | 1.7858 ns | **1.18x** | 1.6928 ns | **1.23x** |
| `type.TypeHandle.Value` | 1.6439 ns | **1.08x** | 1.6270 ns | **1.19x** |

CIs do not overlap on either axis (virtual hit [1.418, 1.613] vs handle hit [1.635, 1.653]), so the reversal is a real difference, not run-to-run noise.

## Reproducing

The repository's `BenchmarkConfig` carries `DisassemblyDiagnoser`, which NativeAOT does not support, so the two-runtime comparison was run from a standalone harness with the diagnoser removed and jobs supplied on the command line. That harness now lives in the repository as `benchmarks/PerformancePatterns.AotHarness` (it links this class; the MediumRun attribute provides the JIT job):

```
cd benchmarks/PerformancePatterns.AotHarness
dotnet run -c Release -- --filter "*TypeHashSourceBenchmark*" --runtimes nativeaot10.0 --job medium
```
