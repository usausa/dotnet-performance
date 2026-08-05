# ASY-05: ValueTask vs Task on the synchronous completion path

- Verdict: adopted
- Task.FromResult: 2.87 ns + 72 B/call (value 12345 is outside the BCL Task cache) vs new ValueTask<int>: 0.93 ns / 0 B (0.33x)
- async method sync path: Task 6.45 ns + 72 B vs ValueTask 4.23 ns / 0 B
- ValueTask removes the per-call Task allocation whenever completion is synchronous

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| TaskFromResult       | 2.8719 ns | 0.0519 ns | 0.0710 ns | 2.8542 ns | 2.7474 ns | 3.0782 ns | 2.9773 ns |  1.00 |    0.03 | 0.0087 |   1,331 B |      73 B |        1.00 |
| ValueTaskDirect      | 0.9344 ns | 0.0264 ns | 0.0353 ns | 0.9563 ns | 0.8828 ns | 0.9793 ns | 0.9727 ns |  0.33 |    0.01 | 0.0001 |   2,223 B |       1 B |        0.01 |
| AsyncMethodTask      | 6.4548 ns | 0.1022 ns | 0.1432 ns | 6.4008 ns | 6.1487 ns | 6.8401 ns | 6.6405 ns |  2.25 |    0.07 | 0.0087 |   2,065 B |      73 B |        1.00 |
| AsyncMethodValueTask | 4.2314 ns | 0.0333 ns | 0.0445 ns | 4.2155 ns | 4.1740 ns | 4.3646 ns | 4.2769 ns |  1.47 |    0.04 | 0.0001 |   3,028 B |       1 B |        0.01 |
