# DSP-01: sealed devirtualization (single-impl interface call)

- Verdict: conditional - and on x86-64-v4 the remaining time win is nearly gone too
- Via interface reference, sealed vs open still measures equal (220.7 vs 221.9 ns, CIs overlap, code size 84 B both)
- **SealedConcrete (typed reference) is now only 0.98x** (215.2 vs 220.7 ns - non-overlapping CIs, so a real but ~2% difference). It was 0.44x on the previous Ryzen 9 5900X baseline: the newer core predicts the indirect branch well enough that devirtualization buys almost no time
- What survives is **code size: 27 B vs 84 B**. That is where the value now sits - inlining headroom at the call site, AOT (no dynamic PGO), and smaller hot code
- sealed remains free, so keep it as the default; just do not sell it as a speed optimization on this class of hardware

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
