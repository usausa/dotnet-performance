# DSP-05: Pipeline pre-composition

- Verdict: adopted
- Compose-per-call: 19.7 ns + 264 B (3 middleware closures + delegates every call)
- Pre-composed: 1.27 ns / 0 B (0.064x, ~16x faster); direct terminal call: ~0 ns
- Compose once at startup; bypass the chain entirely when it is empty

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|----------:|------------:|
| ComposeEveryCall | 19.6591 ns | 0.3916 ns | 0.5360 ns | 19.8265 ns | 18.5229 ns | 21.0007 ns | 20.0116 ns | 1.001 |    0.04 | 0.0316 |     846 B |     264 B |        1.00 |
| PreComposed      |  1.2668 ns | 0.0213 ns | 0.0312 ns |  1.2561 ns |  1.2284 ns |  1.3543 ns |  1.3015 ns | 0.064 |    0.00 |      - |     330 B |         - |        0.00 |
| TerminalDirect   |  0.0028 ns | 0.0034 ns | 0.0047 ns |  0.0007 ns |  0.0000 ns |  0.0159 ns |  0.0100 ns | 0.000 |    0.00 |      - |       6 B |         - |        0.00 |
