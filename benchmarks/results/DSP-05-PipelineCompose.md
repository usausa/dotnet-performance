# DSP-05: Pipeline pre-composition

- Verdict: adopted
- Compose-per-call: 42.6 ns + 264 B (3 middleware closures + delegates every call)
- Pre-composed: 2.62 ns / 0 B (0.063x, ~16x faster); direct terminal call: 0.10 ns
- Compose once at startup; bypass the chain entirely when it is empty

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|-------:|----------:|------------:|
| ComposeEveryCall | 42.6082 ns | 4.6746 ns | 6.9967 ns | 42.4810 ns | 31.5560 ns | 53.4023 ns | 50.6225 ns | 1.027 |    0.24 |     842 B | 0.0157 |     264 B |        1.00 |
| PreComposed      |  2.6183 ns | 0.3726 ns | 0.5576 ns |  2.4392 ns |  2.0173 ns |  3.4808 ns |  3.3778 ns | 0.063 |    0.02 |     330 B |      - |         - |        0.00 |
| TerminalDirect   |  0.1022 ns | 0.1121 ns | 0.1678 ns |  0.0200 ns |  0.0000 ns |  0.5035 ns |  0.4299 ns | 0.002 |    0.00 |       6 B |      - |         - |        0.00 |
