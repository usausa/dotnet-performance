# DSP-01: sealed devirtualization (single-impl interface call)

- Verdict: conditional - via interface reference, sealed vs open measured equal on net10 (CIs overlap, code size 84 B both)
- SealedConcrete (typed reference) 233.1 ns / 27 B = 0.44x - holding the concrete sealed type is where the win is (direct call + inlining)
- sealed remains free and helps AOT/no-PGO and concrete-typed fields; do not expect interface-typed call sites to speed up from sealing alone on net10

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| OpenInterface   | 536.8 ns | 61.20 ns | 91.60 ns | 453.7 ns | 746.0 ns | 710.5 ns |  1.02 |    0.23 |      84 B |         - |          NA |
| SealedInterface | 509.1 ns | 31.69 ns | 47.42 ns | 450.3 ns | 610.7 ns | 570.6 ns |  0.97 |    0.17 |      84 B |         - |          NA |
| SealedConcrete  | 233.1 ns |  4.51 ns |  6.61 ns | 223.9 ns | 245.6 ns | 242.3 ns |  0.44 |    0.07 |      27 B |         - |          NA |
