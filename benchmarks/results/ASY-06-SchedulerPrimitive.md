# ASY-06: Scheduler primitives (Timer per job vs single-loop TCS wakeup)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. TCS swap + notify is 0.50x of a Timer per job on net10 and 0.52x on net11 (17.98 vs 34.41 ns)
- The Timer path is 0.95x of net10 on net11 with half the code (5,337 → 2,724 B); allocation identical

### SchedulerPrimitiveBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| TimerPerJob | 36.12 ns | 34.41 ns | 0.95 | 120 B → 120 B | 5,337 B → 2,724 B |
| TcsSwapNotify | 18.00 ns | 17.98 ns | 1.00 | 88 B → 88 B | 2,813 B → 5,651 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.11 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|----------:|-------:|----------:|------------:|
| TimerPerJob   | .NET 10.0           | .NET 10.0 | 36.12 ns | 0.097 ns | 0.142 ns | 35.86 ns | 36.52 ns | 36.30 ns |  1.00 |   5,337 B | 0.0143 |     120 B |        1.00 |
| TcsSwapNotify | .NET 10.0           | .NET 10.0 | 18.00 ns | 0.037 ns | 0.055 ns | 17.89 ns | 18.09 ns | 18.06 ns |  0.50 |   2,813 B | 0.0105 |      88 B |        0.73 |
|               |                     |           |          |          |          |          |          |          |       |           |        |           |             |
| TimerPerJob   | MediumRun-.NET 11.0 | .NET 11.0 | 34.41 ns | 0.114 ns | 0.163 ns | 34.12 ns | 34.78 ns | 34.66 ns |  1.00 |   2,724 B | 0.0143 |     120 B |        1.00 |
| TcsSwapNotify | MediumRun-.NET 11.0 | .NET 11.0 | 17.98 ns | 0.044 ns | 0.066 ns | 17.88 ns | 18.13 ns | 18.06 ns |  0.52 |   5,651 B | 0.0105 |      88 B |        0.73 |

## Earlier record

- Verdict: adopted (primitive-level evidence)
- Timer create+dispose per job: 36.0 ns + 120 B (includes global timer-queue registration)
- TCS swap + TrySetResult notify: 20.3 ns + 88 B (0.56x)
- Measures registration/notification primitives only; end-to-end scheduler behavior under load is not covered

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| TimerPerJob   | 36.00 ns | 0.704 ns | 0.964 ns | 34.49 ns | 39.03 ns | 37.44 ns |  1.00 |    0.04 | 0.0143 |   3,580 B |     120 B |        1.00 |
| TcsSwapNotify | 20.28 ns | 0.120 ns | 0.172 ns | 19.66 ns | 20.62 ns | 20.44 ns |  0.56 |    0.02 | 0.0105 |     288 B |      88 B |        0.73 |
