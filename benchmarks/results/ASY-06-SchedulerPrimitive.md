# ASY-06: Scheduler primitives (Timer per job vs single-loop TCS wakeup)

- Verdict: adopted (primitive-level evidence)
- Timer create+dispose per job: 41.0 ns + 120 B (includes global timer-queue registration)
- TCS swap + TrySetResult notify: 11.2 ns + 88 B (0.27x)
- Measures registration/notification primitives only; end-to-end scheduler behavior under load is not covered

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| TimerPerJob   | 41.03 ns | 0.537 ns | 0.787 ns | 39.58 ns | 42.56 ns | 42.04 ns |  1.00 |    0.03 | 0.0072 |   3,580 B |     120 B |        1.00 |
| TcsSwapNotify | 11.18 ns | 0.566 ns | 0.794 ns | 10.31 ns | 13.16 ns | 12.36 ns |  0.27 |    0.02 | 0.0053 |     288 B |      88 B |        0.73 |
