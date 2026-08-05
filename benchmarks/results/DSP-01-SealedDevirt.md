# DSP-01: sealed devirtualization (single-impl interface call)

- Verdict: conditional
- Via interface reference, sealed vs open measures equal (220.7 vs 221.9 ns, CIs overlap, code size 84 B both)
- Concrete sealed reference: 0.98x time (a real but ~2% difference), code 27 B vs 84 B - the payoff is the direct call's code size and inlining headroom, not wall-clock time; the indirect call itself predicts well on modern cores
- The code-size/devirtualization win matters most under AOT or without dynamic PGO, where the runtime cannot despeculate the call site
- sealed remains free - keep it as the default, but do not expect interface-typed call sites to get faster from sealing alone

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
