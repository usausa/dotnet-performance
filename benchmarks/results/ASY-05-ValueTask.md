# ASY-05: ValueTask vs Task on the synchronous completion path

## net11 re-verification with runtime-async off and on (2026-11-18, HX 370)

- Verdict: unchanged with the default compiler settings. With runtime-async on, the allocation difference between
  `Task` and `ValueTask` on the synchronous path disappears
- runtime-async off: `TaskFromResult` 2.97 ns / 73 B against `ValueTaskDirect` 0.86 ns; async method `Task`
  6.88 ns / 73 B against `ValueTask` 4.20 ns
- runtime-async on: every row is 0 B; `TaskFromResult` 0.175 ns against `ValueTaskDirect` 0.177 ns, and the async
  methods 0.815 ns for both return types
- The same run conditions and caveats as ASY-01 apply (`Features=runtime-async=on`, async-to-async harness, no
  disassembly on that job, net11 only)

### runtime-async off

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.41 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| TaskFromResult       | 2.9713 ns | 0.0198 ns | 0.0284 ns | 2.9197 ns | 3.0151 ns | 3.0045 ns |  1.00 |    0.00 |   3,170 B | 0.0087 |      73 B |        1.00 |
| ValueTaskDirect      | 0.8578 ns | 0.0035 ns | 0.0052 ns | 0.8498 ns | 0.8692 ns | 0.8659 ns |  0.29 |    0.00 |   3,282 B | 0.0001 |       1 B |        0.01 |
| AsyncMethodTask      | 6.8815 ns | 0.0674 ns | 0.1008 ns | 6.7499 ns | 7.0516 ns | 7.0195 ns |  2.32 |    0.04 |   3,770 B | 0.0087 |      73 B |        1.00 |
| AsyncMethodValueTask | 4.1999 ns | 0.0271 ns | 0.0397 ns | 4.1300 ns | 4.3062 ns | 4.2483 ns |  1.41 |    0.02 |   3,939 B | 0.0001 |       1 B |        0.01 |

### runtime-async on

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.39 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| TaskFromResult       | 0.1750 ns | 0.0020 ns | 0.0029 ns | 0.1725 ns | 0.1847 ns | 0.1787 ns |  1.00 |    0.00 |         - |         - |          NA |
| ValueTaskDirect      | 0.1768 ns | 0.0009 ns | 0.0012 ns | 0.1747 ns | 0.1787 ns | 0.1782 ns |  1.01 |    0.02 |         - |         - |          NA |
| AsyncMethodTask      | 0.8152 ns | 0.0021 ns | 0.0032 ns | 0.8104 ns | 0.8234 ns | 0.8185 ns |  4.66 |    0.08 |         - |         - |          NA |
| AsyncMethodValueTask | 0.8146 ns | 0.0017 ns | 0.0024 ns | 0.8113 ns | 0.8202 ns | 0.8169 ns |  4.66 |    0.07 |         - |         - |          NA |

## Earlier record

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
