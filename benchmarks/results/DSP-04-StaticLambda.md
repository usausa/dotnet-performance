# DSP-04: Static lambda + TState vs capturing lambda

- Verdict: adopted
- Capturing a per-iteration local: 11.2 ns + 88 B per call (closure + delegate)
- static lambda + TState: 4.66 ns / 0 B (0.42x) - the compiler caches the delegate

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| CaptureLocal    | 11.197 ns | 0.4690 ns | 0.6874 ns | 10.050 ns | 12.720 ns | 12.032 ns |  1.00 |    0.08 | 0.0052 |     300 B |      88 B |        1.00 |
| StaticWithState |  4.664 ns | 0.0555 ns | 0.0814 ns |  4.556 ns |  4.888 ns |  4.800 ns |  0.42 |    0.03 |      - |     329 B |         - |        0.00 |
