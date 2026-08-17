# JIT-01: AggressiveInlining on a loop-containing helper

- Verdict: attribute had no effect in this shape (default policy already inlined); NoInlining shows the stake
- Default 0.943 us vs Aggressive 0.959 us (CIs overlap) - call-site codegen IDENTICAL (100 B both): net10 default policy inlines the loop-containing helper
- NoInline 1.180 us: non-overlapping CIs vs default (+25%) - inlining itself matters; the attribute is insurance for shapes the heuristic declines

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DefaultPolicy |   943.1 ns |  4.21 ns |  5.48 ns |   934.2 ns |   955.6 ns |   949.8 ns |  1.00 |    0.01 |     100 B |         - |          NA |
| Aggressive    |   958.9 ns | 18.46 ns | 27.63 ns |   935.9 ns | 1,033.5 ns |   997.3 ns |  1.02 |    0.03 |     100 B |         - |          NA |
| NoInline      | 1,180.1 ns | 13.87 ns | 20.33 ns | 1,142.7 ns | 1,225.2 ns | 1,204.6 ns |  1.25 |    0.02 |     107 B |         - |          NA |

## NativeAOT comparison (net10.0 vs NativeAOT 10.0, same MediumRun settings)

- **Prediction confirmed, and it is the strongest of the AOT re-measurements.** Under JIT the attribute does nothing because the default policy already inlines the loop-containing helper. Under AOT the default policy does **not** inline it, and the attribute is worth **0.69x**
- The giveaway: under AOT, `DefaultPolicy` (1,391.9 ns) and `NoInline` (1,395.3 ns) are **identical** - the default heuristic declined the inline on its own, so opting out changes nothing
- `Aggressive` under AOT (954.5 ns) is faster than anything the JIT produced here, including the JIT's own inlined default (988.3 ns). The static compiler generates better code once it is told to inline
- Practical rule for AOT targets: **spell out `AggressiveInlining` on small hot helpers the heuristic may decline** (anything containing a loop is a prime candidate). Under JIT it costs nothing to add; under AOT it is the difference between inlined and not

Ratios below are BDN's, all against `DefaultPolicy` on **.NET 10.0**. Within NativeAOT, against its own `DefaultPolicy` (1,391.9 ns): Aggressive **0.69x**, NoInline 1.00x.

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
| Method        | Job            | Runtime        | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------- |--------------- |--------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| DefaultPolicy | .NET 10.0      | .NET 10.0      |   988.3 ns |  3.62 ns |  5.30 ns |   977.1 ns | 1,003.4 ns |   994.0 ns |  1.00 |    0.01 |         - |          NA |
| Aggressive    | .NET 10.0      | .NET 10.0      |   991.8 ns |  4.15 ns |  5.95 ns |   981.5 ns | 1,005.7 ns |   998.5 ns |  1.00 |    0.01 |         - |          NA |
| NoInline      | .NET 10.0      | .NET 10.0      | 1,209.0 ns | 13.64 ns | 18.66 ns | 1,175.9 ns | 1,239.9 ns | 1,231.0 ns |  1.22 |    0.02 |         - |          NA |
| DefaultPolicy | NativeAOT 10.0 | NativeAOT 10.0 | 1,391.9 ns |  6.49 ns |  9.51 ns | 1,375.2 ns | 1,416.3 ns | 1,404.8 ns |  1.41 |    0.01 |         - |          NA |
| Aggressive    | NativeAOT 10.0 | NativeAOT 10.0 |   954.5 ns |  8.45 ns | 12.39 ns |   942.1 ns |   988.7 ns |   975.5 ns |  0.97 |    0.01 |         - |          NA |
| NoInline      | NativeAOT 10.0 | NativeAOT 10.0 | 1,395.3 ns |  5.80 ns |  8.13 ns | 1,376.9 ns | 1,412.4 ns | 1,402.3 ns |  1.41 |    0.01 |         - |          NA |
