# GEN-01: Emit factory target strategies

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged. Holder-field target 1.27x, closure-array target 1.30x, chained factories 1.88-1.89x of a compiled lambda on net11 (1.26x / 1.28x / 1.84-1.86x on net10)
- Call vs Callvirt stays within the noise (6.94 vs 6.96 ns)

### EmitStrategyBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| DirectLambda | 3.707 ns | 3.689 ns | 1.00 | 32 B → 32 B | 165 B → 165 B |
| EmitHolderField | 4.658 ns | 4.682 ns | 1.01 | 32 B → 32 B | 21 B → 21 B |
| EmitClosureArray | 4.736 ns | 4.808 ns | 1.02 | 32 B → 32 B | 21 B → 21 B |
| EmitChainedCallvirt | 6.913 ns | 6.964 ns | 1.01 | 32 B → 32 B | 21 B → 21 B |
| EmitChainedCall | 6.805 ns | 6.942 ns | 1.02 | 32 B → 32 B | 21 B → 21 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.04 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| DirectLambda        | .NET 10.0           | .NET 10.0 | 3.707 ns | 0.0153 ns | 0.0228 ns | 3.657 ns | 3.762 ns | 3.732 ns |  1.00 |    0.00 |     165 B | 0.0038 |      32 B |        1.00 |
| EmitHolderField     | .NET 10.0           | .NET 10.0 | 4.658 ns | 0.0178 ns | 0.0249 ns | 4.614 ns | 4.706 ns | 4.696 ns |  1.26 |    0.01 |      21 B | 0.0038 |      32 B |        1.00 |
| EmitClosureArray    | .NET 10.0           | .NET 10.0 | 4.736 ns | 0.0266 ns | 0.0399 ns | 4.682 ns | 4.872 ns | 4.778 ns |  1.28 |    0.01 |      21 B | 0.0038 |      32 B |        1.00 |
| EmitChainedCallvirt | .NET 10.0           | .NET 10.0 | 6.913 ns | 0.0452 ns | 0.0676 ns | 6.790 ns | 7.067 ns | 6.997 ns |  1.86 |    0.02 |      21 B | 0.0038 |      32 B |        1.00 |
| EmitChainedCall     | .NET 10.0           | .NET 10.0 | 6.805 ns | 0.0333 ns | 0.0478 ns | 6.739 ns | 6.926 ns | 6.871 ns |  1.84 |    0.02 |      21 B | 0.0038 |      32 B |        1.00 |
|                     |                     |           |          |           |           |          |          |          |       |         |           |        |           |             |
| DirectLambda        | MediumRun-.NET 11.0 | .NET 11.0 | 3.689 ns | 0.0116 ns | 0.0171 ns | 3.661 ns | 3.718 ns | 3.714 ns |  1.00 |    0.00 |     165 B | 0.0038 |      32 B |        1.00 |
| EmitHolderField     | MediumRun-.NET 11.0 | .NET 11.0 | 4.682 ns | 0.0185 ns | 0.0277 ns | 4.640 ns | 4.729 ns | 4.722 ns |  1.27 |    0.01 |      21 B | 0.0038 |      32 B |        1.00 |
| EmitClosureArray    | MediumRun-.NET 11.0 | .NET 11.0 | 4.808 ns | 0.0670 ns | 0.0983 ns | 4.667 ns | 5.048 ns | 4.959 ns |  1.30 |    0.03 |      21 B | 0.0038 |      32 B |        1.00 |
| EmitChainedCallvirt | MediumRun-.NET 11.0 | .NET 11.0 | 6.964 ns | 0.1087 ns | 0.1559 ns | 6.712 ns | 7.294 ns | 7.100 ns |  1.89 |    0.04 |      21 B | 0.0038 |      32 B |        1.00 |
| EmitChainedCall     | MediumRun-.NET 11.0 | .NET 11.0 | 6.942 ns | 0.0797 ns | 0.1143 ns | 6.784 ns | 7.135 ns | 7.081 ns |  1.88 |    0.03 |      21 B | 0.0038 |      32 B |        1.00 |

## Earlier record

- Verdict: adopted (holder-field target + inline expansion); Call-vs-Callvirt: NO EFFECT (JIT-verified identical codegen)
- Holder-field target: 4.23 ns, near a compiled C# closure lambda (3.77 ns) - same ldfld shape
- Closure-array target (ldelem + castclass): 4.61 ns (1.22x vs lambda) - avoid object[] targets
- Chained child factories: 6.36-6.46 ns (1.7x) - inline children into the parent IL instead
- Call vs Callvirt on Func<object>.Invoke: 6.46 vs 6.36 ns (CIs overlap) -> JitDisasm comparison: 68 instructions / 229 bytes IDENTICAL. The delegate target-field load doubles as the null check, so the JIT drops callvirt's check. Verdict: no effect on net10 (not noise - verified at codegen level)
- JIT-only technique: Reflection.Emit throws on Native AOT (see docs/aot-compatibility.md AOTP-01)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| DirectLambda        | 3.773 ns | 0.0399 ns | 0.0573 ns | 3.648 ns | 3.889 ns | 3.832 ns |  1.00 |    0.02 | 0.0038 |     167 B |      32 B |        1.00 |
| EmitHolderField     | 4.226 ns | 0.0386 ns | 0.0565 ns | 4.128 ns | 4.343 ns | 4.295 ns |  1.12 |    0.02 | 0.0038 |      12 B |      32 B |        1.00 |
| EmitClosureArray    | 4.614 ns | 0.3331 ns | 0.4986 ns | 4.205 ns | 6.438 ns | 5.348 ns |  1.22 |    0.13 | 0.0038 |      12 B |      32 B |        1.00 |
| EmitChainedCallvirt | 6.363 ns | 0.0643 ns | 0.0922 ns | 6.191 ns | 6.536 ns | 6.491 ns |  1.69 |    0.03 | 0.0038 |      12 B |      32 B |        1.00 |
| EmitChainedCall     | 6.459 ns | 0.0720 ns | 0.1056 ns | 6.307 ns | 6.703 ns | 6.628 ns |  1.71 |    0.04 | 0.0038 |      12 B |      32 B |        1.00 |
