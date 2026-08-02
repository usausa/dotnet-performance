# GEN-01: Emit factory target strategies

- Verdict: adopted (holder-field target + inline expansion); Call-vs-Callvirt substitution not confirmed
- Holder-field target: 6.55 ns == compiled C# closure lambda (6.23 ns) - same ldfld shape
- Closure-array target (ldelem + castclass): 9.34 ns (1.51x vs lambda) - avoid object[] targets
- Chained child factories: 14.2-14.6 ns (2.3x) - inline children into the parent IL instead
- Call vs Callvirt on Func<object>.Invoke: 14.20 vs 14.65 ns - within noise on net10; do not count on it
- JIT-only technique: Reflection.Emit throws on Native AOT (see docs/aot-compatibility.md AOTP-01)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| DirectLambda        |  6.227 ns | 0.3559 ns | 0.5216 ns |  5.665 ns |  7.667 ns |  6.879 ns |  1.01 |    0.11 | 0.0019 |     167 B |      32 B |        1.00 |
| EmitHolderField     |  6.548 ns | 0.1233 ns | 0.1729 ns |  6.165 ns |  6.972 ns |  6.719 ns |  1.06 |    0.08 | 0.0019 |      12 B |      32 B |        1.00 |
| EmitClosureArray    |  9.344 ns | 0.7477 ns | 1.1192 ns |  6.529 ns | 11.309 ns | 10.548 ns |  1.51 |    0.21 | 0.0019 |      12 B |      32 B |        1.00 |
| EmitChainedCallvirt | 14.648 ns | 0.7866 ns | 1.1773 ns | 12.583 ns | 17.356 ns | 16.152 ns |  2.37 |    0.26 | 0.0019 |      12 B |      32 B |        1.00 |
| EmitChainedCall     | 14.201 ns | 0.5584 ns | 0.8358 ns | 12.248 ns | 15.498 ns | 15.154 ns |  2.29 |    0.22 | 0.0019 |      12 B |      32 B |        1.00 |
