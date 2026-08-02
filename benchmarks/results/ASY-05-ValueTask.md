# ASY-05: ValueTask vs Task on the synchronous completion path

- Verdict: adopted
- Task.FromResult: 6.21 ns + 72 B/call (value 12345 is outside the BCL Task cache) vs new ValueTask<int>: 1.93 ns / 0 B (0.31x)
- async method sync path: Task 11.1 ns + 72 B vs ValueTask 6.60 ns / 0 B
- ValueTask removes the per-call Task allocation whenever completion is synchronous

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| TaskFromResult       |  6.211 ns | 0.4996 ns | 0.7323 ns |  5.141 ns |  7.840 ns |  7.159 ns |  1.01 |    0.17 | 0.0043 |   1,331 B |      73 B |        1.00 |
| ValueTaskDirect      |  1.928 ns | 0.0257 ns | 0.0376 ns |  1.863 ns |  2.018 ns |  1.976 ns |  0.31 |    0.04 | 0.0000 |   2,223 B |       1 B |        0.01 |
| AsyncMethodTask      | 11.142 ns | 0.2550 ns | 0.3738 ns | 10.387 ns | 11.747 ns | 11.517 ns |  1.82 |    0.22 | 0.0043 |   2,065 B |      73 B |        1.00 |
| AsyncMethodValueTask |  6.595 ns | 0.1387 ns | 0.2033 ns |  6.324 ns |  7.186 ns |  6.882 ns |  1.08 |    0.13 | 0.0000 |   3,028 B |       1 B |        0.01 |
