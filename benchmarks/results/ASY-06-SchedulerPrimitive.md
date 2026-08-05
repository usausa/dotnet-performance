# ASY-06: Scheduler primitives (Timer per job vs single-loop TCS wakeup)

- Verdict: adopted (primitive-level evidence)
- Timer create+dispose per job: 41.0 ns + 120 B (includes global timer-queue registration)
- TCS swap + TrySetResult notify: 11.2 ns + 88 B (0.27x)
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
