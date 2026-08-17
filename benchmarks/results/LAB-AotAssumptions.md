# LAB: Re-measuring the "assumptions that change under AOT" table

Investigation backing the two rows of [README's AOT assumptions table](../../README.md) that had no pattern of their own: `AggressiveOptimization`, and the "what AOT makes better instead" claim. The other three rows are re-measured in their own pattern files ([DSP-01](DSP-01-SealedDevirt.md) / [DSP-02](DSP-02-CallAbstraction.md) / [JIT-01](JIT-01-Inlining.md)).

Procedure and pitfalls: [benchmark-methodology.md](../../docs/benchmark-methodology.md).

## AggressiveOptimization

- Verdict: **confirmed, and the JIT penalty is far larger than "can actually be slower" suggested**
- JIT, PGO-sensitive shape (interface dispatch in a loop): 268.8 ns → 1,555.0 ns, **5.79x slower**. The attribute opts the method out of tiered compilation, so it is never instrumented, Dynamic PGO has no data, and the guarded devirtualization that makes the interface call cheap never happens
- JIT, control shape (straight-line arithmetic, nothing profile-dependent): 985.2 → 1,239.2 ns, **1.26x slower**. So it is not only about PGO - tier1-with-profile beats "compile once at tier1" even where there is nothing to speculate on
- **AOT: genuinely inert.** 302.5 → 288.0 ns (0.95x) on the interface shape and 1,397.6 → 1,422.0 ns (1.02x) on the arithmetic one - a few percent in *both* directions, which is what "no tiered compilation to opt out of" should look like
- Guidance unchanged and now quantified: do not use it under JIT; under AOT it does nothing

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]         : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  .NET 10.0      : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  NativeAOT 10.0 : .NET 10.0.10, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job            | Runtime        | Mean       | Error    | StdDev   | Median     | Min        | Max        | P90        | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------ |--------------- |--------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|------------:|
| InterfaceDefault        | .NET 10.0      | .NET 10.0      |   268.8 ns |  3.71 ns |  5.44 ns |   268.0 ns |   260.1 ns |   286.8 ns |   274.5 ns |  1.00 |    0.03 |         - |          NA |
| InterfaceAggressiveOpt  | .NET 10.0      | .NET 10.0      | 1,555.0 ns | 23.84 ns | 35.69 ns | 1,551.6 ns | 1,496.9 ns | 1,630.6 ns | 1,597.7 ns |  5.79 |    0.17 |         - |          NA |
| ArithmeticDefault       | .NET 10.0      | .NET 10.0      |   985.2 ns |  3.16 ns |  4.54 ns |   984.3 ns |   977.4 ns |   994.3 ns |   992.0 ns |  3.67 |    0.07 |         - |          NA |
| ArithmeticAggressiveOpt | .NET 10.0      | .NET 10.0      | 1,239.2 ns | 22.03 ns | 31.59 ns | 1,233.3 ns | 1,135.0 ns | 1,325.6 ns | 1,272.6 ns |  4.61 |    0.15 |         - |          NA |
| InterfaceDefault        | NativeAOT 10.0 | NativeAOT 10.0 |   302.5 ns |  4.72 ns |  6.62 ns |   300.3 ns |   295.1 ns |   317.7 ns |   313.1 ns |  1.13 |    0.03 |         - |          NA |
| InterfaceAggressiveOpt  | NativeAOT 10.0 | NativeAOT 10.0 |   288.0 ns |  7.35 ns | 10.77 ns |   281.1 ns |   279.3 ns |   312.8 ns |   303.2 ns |  1.07 |    0.04 |         - |          NA |
| ArithmeticDefault       | NativeAOT 10.0 | NativeAOT 10.0 | 1,397.6 ns |  8.99 ns | 12.60 ns | 1,397.5 ns | 1,375.4 ns | 1,427.2 ns | 1,411.1 ns |  5.20 |    0.11 |         - |          NA |
| ArithmeticAggressiveOpt | NativeAOT 10.0 | NativeAOT 10.0 | 1,422.0 ns | 15.12 ns | 22.17 ns | 1,415.3 ns | 1,394.1 ns | 1,470.0 ns | 1,456.2 ns |  5.29 |    0.13 |         - |          NA |

Ratios are BDN's, all against `InterfaceDefault` on **.NET 10.0**. Read within each runtime: JIT interface **5.79x** / arithmetic **1.26x**; AOT interface **0.95x** / arithmetic **1.02x**.

## Cold start: "fully optimized from the very first call"

BDN measures steady state, so this needs a separate probe - a fresh process, no warmup, timing successive batches of 2,000 calls. `cachedNs` reads a per-type artifact fixed by a generic type initializer (the TYP-04 / TYP-06 shape); `typeofNs` recomputes the same value through reflection at the call site.

- Verdict on the TYP-04 / TYP-06 half: **confirmed, and the effect is larger than the claim stated**
- Under AOT the cached path is at its steady state of **1.20 ns from batch 1** and never moves again
- Under JIT it takes about **5 batches (~10,000 calls)** to get there, and batch 4 shows a **3,418 ns/call spike** - the tier1 recompilation being observed in flight
- AOT's steady state is also **2.9x faster** than the JIT's (1.20 vs 3.45 ns), so this is not only a startup story
- **New finding not in the original claim:** recomputing the artifact through reflection at the call site costs **~125 ns/call under AOT vs ~4.85 ns under JIT - roughly 26x worse, and it never improves.** Pre-computing per-type artifacts therefore matters *far more* under AOT than under JIT, which is a stronger reason to adopt TYP-04 / TYP-06 than "no startup wait"

```
JIT (tiered)                      NativeAOT
batch  cachedNs   typeofNs        batch  cachedNs   typeofNs
0        193.70      22.95        0         24.90     116.80
1          9.55       9.60        1          1.20     122.40
2          8.25       9.25        2          1.25     127.55
3          8.25       9.25        3          1.20     123.05
4       3418.30       5.20        4          1.20     123.65
5          3.45       4.85        5          1.25     125.00
...                               ...
23         3.40       4.85        23         1.25     133.00
```

**What this probe does _not_ establish:** the original claim also mentioned R-01 (caching `typeof(T)` in a `static readonly` field losing before Tier1 promotion). That is about the `typeof` operator itself, whereas `typeofNs` above measures a full reflection call (`Type.Name`). The R-01 half remains unverified under AOT.
