# GEN-01: Emit factory target strategies

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
