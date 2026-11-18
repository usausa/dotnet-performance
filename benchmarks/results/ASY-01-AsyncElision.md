# ASY-01: Async state machine elision

## net11 re-verification with runtime-async off and on (2026-11-18, HX 370)

- Verdict: unchanged with the default compiler settings. With `<Features>runtime-async=on</Features>` the await
  wrapper no longer allocates, so eliding it is a time saving only
- runtime-async off (the default on net11): `TaskAwaitForward` 6.77 ns / 73 B against `TaskDirectForward`
  1.00 ns / 1 B (0.15x); ValueTask 4.10 ns against 0.83 ns
- runtime-async on: `TaskAwaitForward` 2.49 ns / 0 B against `TaskDirectForward` 0.38 ns (0.15x); ValueTask 1.15 ns
  against 0.17 ns
- How it was run: the environment variable `Features=runtime-async=on` becomes the MSBuild property of BDN's child
  build. net10 cannot build with the flag (SYSLIB5007, the API is preview there)
- Caveats: the generated harness is compiled with runtime-async too, so the benchmark call is async-to-async; the
  disassembly diagnoser cannot locate runtime-async methods, so `Code Size` is empty on that job

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
| Method                 | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|-------:|----------:|------------:|
| TaskAwaitForward       | 6.7729 ns | 0.0567 ns | 0.0849 ns | 6.7578 ns | 6.6625 ns | 7.0381 ns | 6.8655 ns |  1.00 |   3,578 B | 0.0087 |      73 B |        1.00 |
| TaskDirectForward      | 1.0044 ns | 0.0056 ns | 0.0078 ns | 1.0032 ns | 0.9932 ns | 1.0250 ns | 1.0139 ns |  0.15 |   2,827 B | 0.0001 |       1 B |        0.01 |
| ValueTaskAwaitForward  | 4.0954 ns | 0.0295 ns | 0.0432 ns | 4.0908 ns | 4.0348 ns | 4.1980 ns | 4.1458 ns |  0.60 |   3,965 B | 0.0001 |       1 B |        0.01 |
| ValueTaskDirectForward | 0.8293 ns | 0.0132 ns | 0.0189 ns | 0.8190 ns | 0.8070 ns | 0.8539 ns | 0.8517 ns |  0.12 |   3,123 B | 0.0001 |       1 B |        0.01 |

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
| Method                 | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| TaskAwaitForward       | 2.4856 ns | 0.0092 ns | 0.0129 ns | 2.4586 ns | 2.5168 ns | 2.5007 ns |  1.00 |    0.00 |         - |         - |          NA |
| TaskDirectForward      | 0.3769 ns | 0.0293 ns | 0.0400 ns | 0.3347 ns | 0.4182 ns | 0.4169 ns |  0.15 |    0.02 |         - |         - |          NA |
| ValueTaskAwaitForward  | 1.1468 ns | 0.0265 ns | 0.0396 ns | 1.0558 ns | 1.2313 ns | 1.1892 ns |  0.46 |    0.02 |         - |         - |          NA |
| ValueTaskDirectForward | 0.1679 ns | 0.0012 ns | 0.0017 ns | 0.1654 ns | 0.1713 ns | 0.1706 ns |  0.07 |    0.00 |         - |         - |          NA |

## Earlier record

- Verdict: adopted
- Task direct return 0.16x, 73 B -> ~0 B (async wrapper re-wraps even a cached completed Task)
- ValueTask direct return 0.13x vs await wrapper 0.67x (both 0 B)
- Apply only to pure forwards (single await, no try/using/lock across it)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                 | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| TaskAwaitForward       | 6.3668 ns | 0.1941 ns | 0.2721 ns | 6.1188 ns | 7.2739 ns | 6.5284 ns |  1.00 |    0.06 | 0.0087 |   2,041 B |      73 B |        1.00 |
| TaskDirectForward      | 0.9974 ns | 0.0322 ns | 0.0462 ns | 0.9715 ns | 1.1464 ns | 1.0639 ns |  0.16 |    0.01 | 0.0001 |   1,248 B |       1 B |        0.01 |
| ValueTaskAwaitForward  | 4.2302 ns | 0.0575 ns | 0.0825 ns | 4.1295 ns | 4.3567 ns | 4.3309 ns |  0.67 |    0.03 | 0.0001 |   3,761 B |       1 B |        0.01 |
| ValueTaskDirectForward | 0.8312 ns | 0.0119 ns | 0.0166 ns | 0.7965 ns | 0.8764 ns | 0.8514 ns |  0.13 |    0.01 | 0.0001 |   2,228 B |       1 B |        0.01 |
