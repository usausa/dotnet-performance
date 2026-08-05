# STK-09: params ReadOnlySpan<T> vs params T[]

- Verdict: adopted
- 0.25x with 3 args (4.46 -> 1.10 ns), allocation 48 B -> 0 B; same call-site syntax

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method      | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-------:|----------:|----------:|------------:|
| ParamsArray | 4.464 ns | 0.0321 ns | 0.0461 ns | 4.351 ns | 4.529 ns | 4.513 ns |  1.00 | 0.0057 |     115 B |      48 B |        1.00 |
| ParamsSpan  | 1.100 ns | 0.0297 ns | 0.0425 ns | 1.045 ns | 1.195 ns | 1.163 ns |  0.25 |      - |     115 B |         - |        0.00 |
