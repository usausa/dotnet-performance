# STK-09: params ReadOnlySpan<T> vs params T[]

- Verdict: adopted
- 0.29x with 3 args, allocation 48 B -> 0 B; same call-site syntax

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method      | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| ParamsArray | 6.262 ns | 0.1118 ns | 0.1604 ns | 6.042 ns | 6.649 ns | 6.463 ns |  1.00 |    0.04 | 0.0029 |     115 B |      48 B |        1.00 |
| ParamsSpan  | 1.841 ns | 0.0314 ns | 0.0470 ns | 1.762 ns | 1.938 ns | 1.904 ns |  0.29 |    0.01 |      - |     115 B |         - |        0.00 |
