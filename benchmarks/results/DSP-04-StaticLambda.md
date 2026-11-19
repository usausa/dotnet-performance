# DSP-04: Static lambda + TState vs capturing lambda

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted
- The capturing form allocates 80 B on net11 instead of 88 B (the 8 B delegate shrink, #99200); static + state stays 0 B at 0.42x of the time

### StaticLambdaBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| CaptureLocal | 6.782 ns | 6.846 ns | 1.01 | 88 B → 80 B | 300 B → 300 B |
| StaticWithState | 2.892 ns | 2.866 ns | 0.99 | - → - | 329 B → 329 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.83 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method          | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|---------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|----------:|-------:|----------:|------------:|
| CaptureLocal    | .NET 10.0           | .NET 10.0 | 6.782 ns | 0.0384 ns | 0.0563 ns | 6.699 ns | 6.933 ns | 6.852 ns |  1.00 |     300 B | 0.0105 |      88 B |        1.00 |
| StaticWithState | .NET 10.0           | .NET 10.0 | 2.892 ns | 0.0335 ns | 0.0501 ns | 2.807 ns | 3.025 ns | 2.949 ns |  0.43 |     329 B |      - |         - |        0.00 |
|                 |                     |           |          |           |           |          |          |          |       |           |        |           |             |
| CaptureLocal    | MediumRun-.NET 11.0 | .NET 11.0 | 6.846 ns | 0.0564 ns | 0.0826 ns | 6.701 ns | 6.999 ns | 6.933 ns |  1.00 |     300 B | 0.0096 |      80 B |        1.00 |
| StaticWithState | MediumRun-.NET 11.0 | .NET 11.0 | 2.866 ns | 0.0200 ns | 0.0293 ns | 2.821 ns | 2.935 ns | 2.906 ns |  0.42 |     329 B |      - |         - |        0.00 |

## Earlier record

- Verdict: adopted
- Capturing a per-iteration local: 7.09 ns + 88 B per call (closure + delegate)
- static lambda + TState: 2.96 ns / 0 B (0.42x) - the compiler caches the delegate

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| CaptureLocal    | 7.089 ns | 0.1204 ns | 0.1648 ns | 6.650 ns | 7.533 ns | 7.245 ns |  1.00 |    0.03 | 0.0105 |     300 B |      88 B |        1.00 |
| StaticWithState | 2.957 ns | 0.0512 ns | 0.0734 ns | 2.852 ns | 3.127 ns | 3.082 ns |  0.42 |    0.01 |      - |     329 B |         - |        0.00 |
