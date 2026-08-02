# DSP-02: Call abstraction overhead (1024 calls, net10)

- Verdict: hold the concrete sealed type; function pointers are NOT the fast option on net10
- DirectSealed 265.4 ns (1.00, 27 B - inlined) | ViaAbstract 463.4 ns (1.75) | ViaDelegate 461.7 ns (1.74) | ViaInterface 549.0 ns (2.07) | ViaFunctionPointer 1,601.7 ns (6.04x SLOWEST)
- Why the function pointer loses: calli cannot be inlined and PGO cannot speculate on it, while a delegate's Invoke gets guarded devirtualization + inlining of the target
- Delegate ~= abstract < interface: the old 'delegates are heavier than interfaces' rule does not hold
- Function pointers remain for interop/AOT boundaries and megamorphic targets where PGO speculation would fail anyway - not as a general speed tool

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DirectSealed       |   265.4 ns |  5.67 ns |  8.13 ns |   253.0 ns |   286.1 ns |   275.4 ns |  1.00 |    0.04 |      27 B |         - |          NA |
| ViaInterface       |   549.0 ns | 24.54 ns | 35.20 ns |   513.2 ns |   642.7 ns |   597.3 ns |  2.07 |    0.14 |      84 B |         - |          NA |
| ViaAbstract        |   463.4 ns |  7.56 ns | 10.84 ns |   450.8 ns |   488.4 ns |   477.8 ns |  1.75 |    0.07 |      81 B |         - |          NA |
| ViaDelegate        |   461.7 ns |  6.88 ns | 10.29 ns |   450.1 ns |   478.6 ns |   477.5 ns |  1.74 |    0.06 |      85 B |         - |          NA |
| ViaFunctionPointer | 1,601.7 ns | 37.17 ns | 55.63 ns | 1,550.4 ns | 1,744.6 ns | 1,696.1 ns |  6.04 |    0.27 |      42 B |         - |          NA |
