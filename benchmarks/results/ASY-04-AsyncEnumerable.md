# ASY-04: IAsyncEnumerable cost

- Verdict: adopted (cost awareness)
- await foreach over sync-completing items: 15.2x per item (0.48 -> 7.3 ns)
- Use IAsyncEnumerable only when element production is truly asynchronous

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| SyncForeach  | 0.4775 ns | 0.0014 ns | 0.0020 ns | 0.4739 ns | 0.4813 ns | 0.4799 ns |  1.00 |    0.01 |     488 B | 0.0000 |         - |          NA |
| AsyncForeach | 7.2553 ns | 0.0167 ns | 0.0250 ns | 7.2124 ns | 7.2945 ns | 7.2887 ns | 15.20 |    0.08 |   3,896 B | 0.0000 |         - |          NA |
