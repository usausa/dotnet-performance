# DSP-02: Call abstraction overhead (1024 calls, net10)

- Verdict: hold the concrete sealed type where possible; function pointers are NOT the fast option on net10
- DirectSealed 215.8 ns (1.00, 27 B - inlined) | ViaAbstract 223.6 ns (1.04) | ViaInterface 224.3 ns (1.04) | ViaDelegate 254.6 ns (1.18) | ViaFunctionPointer 1,250.7 ns (5.80x SLOWEST)
- Interface and abstract dispatch cost only ~4% here - a monomorphic virtual call predicts and speculates well; the delegate's 1.18x is the largest of the ordinary options
- Why the function pointer loses: calli cannot be inlined and PGO cannot speculate on it, while a delegate's Invoke gets guarded devirtualization + inlining of the target - the one real cliff in this table
- Delegate ~= abstract ~= interface: the old 'delegates are heavier than interfaces' rule does not hold
- Function pointers remain for interop/AOT boundaries and megamorphic targets where PGO speculation would fail anyway - not as a general speed tool

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DirectSealed       |   215.8 ns |  0.99 ns |  1.39 ns |   214.4 ns |   219.9 ns |   217.8 ns |  1.00 |    0.01 |      27 B |         - |          NA |
| ViaInterface       |   224.3 ns |  2.22 ns |  3.26 ns |   219.5 ns |   233.7 ns |   228.9 ns |  1.04 |    0.02 |      84 B |         - |          NA |
| ViaAbstract        |   223.6 ns |  1.18 ns |  1.54 ns |   220.5 ns |   225.9 ns |   225.7 ns |  1.04 |    0.01 |      81 B |         - |          NA |
| ViaDelegate        |   254.6 ns |  2.16 ns |  2.95 ns |   251.6 ns |   263.5 ns |   258.8 ns |  1.18 |    0.02 |      85 B |         - |          NA |
| ViaFunctionPointer | 1,250.7 ns | 30.21 ns | 42.36 ns | 1,225.8 ns | 1,379.3 ns | 1,302.3 ns |  5.80 |    0.20 |      42 B |         - |          NA |
