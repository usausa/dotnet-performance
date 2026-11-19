# DSP-05: Pipeline pre-composition

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. The pre-composed chain is 0.10x of compose-per-call on net10 and 0.11x on net11 (2.09 vs 19.03 ns)
- Compose-per-call allocates 264 → 240 B on net11 (three delegates, 8 B each smaller, #99200)

### PipelineComposeBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ComposeEveryCall | 20.0737 ns | 19.0253 ns | 0.95 | 264 B → 240 B | 826 B → 823 B |
| PreComposed | 2.0365 ns | 2.0877 ns | 1.03 | - → - | 326 B → 326 B |
| TerminalDirect | 0.7920 ns | 0.7981 ns | 1.01 | - → - | 6 B → 6 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.33 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|-------:|----------:|----------:|------------:|
| ComposeEveryCall | .NET 10.0           | .NET 10.0 | 20.0737 ns | 0.0993 ns | 0.1486 ns | 19.7742 ns | 20.2957 ns | 20.2476 ns |  1.00 | 0.0316 |     826 B |     264 B |        1.00 |
| PreComposed      | .NET 10.0           | .NET 10.0 |  2.0365 ns | 0.0066 ns | 0.0095 ns |  2.0198 ns |  2.0560 ns |  2.0488 ns |  0.10 |      - |     326 B |         - |        0.00 |
| TerminalDirect   | .NET 10.0           | .NET 10.0 |  0.7920 ns | 0.0010 ns | 0.0014 ns |  0.7893 ns |  0.7944 ns |  0.7936 ns |  0.04 |      - |       6 B |         - |        0.00 |
|                  |                     |           |            |           |           |            |            |            |       |        |           |           |             |
| ComposeEveryCall | MediumRun-.NET 11.0 | .NET 11.0 | 19.0253 ns | 0.0981 ns | 0.1438 ns | 18.7865 ns | 19.4186 ns | 19.1965 ns |  1.00 | 0.0287 |     823 B |     240 B |        1.00 |
| PreComposed      | MediumRun-.NET 11.0 | .NET 11.0 |  2.0877 ns | 0.0305 ns | 0.0418 ns |  2.0346 ns |  2.1454 ns |  2.1325 ns |  0.11 |      - |     326 B |         - |        0.00 |
| TerminalDirect   | MediumRun-.NET 11.0 | .NET 11.0 |  0.7981 ns | 0.0038 ns | 0.0058 ns |  0.7904 ns |  0.8060 ns |  0.8049 ns |  0.04 |      - |       6 B |         - |        0.00 |

## Earlier record

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
