# JIT-05: RuntimeHelpers.IsReferenceOrContainsReferences branch

- Verdict: adopted
- Clear skipped for int[1024]: 40.9 ns -> 0.19 ns (code 510 B -> 28 B)
- Zero measured overhead when clear is still required (string[]: 184.5 vs 189.5 ns, same code size)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean        | Error     | StdDev    | Min         | Max         | P90         | Code Size | Allocated |
|-------------------- |------------:|----------:|----------:|------------:|------------:|------------:|----------:|----------:|
| ClearAlwaysInt      |  40.9204 ns | 0.8301 ns | 1.2167 ns |  38.2679 ns |  43.2945 ns |  42.3548 ns |     510 B |         - |
| ClearIfNeededInt    |   0.1911 ns | 0.0661 ns | 0.0990 ns |   0.0000 ns |   0.3908 ns |   0.3108 ns |      28 B |         - |
| ClearAlwaysString   | 189.5381 ns | 4.7505 ns | 7.1103 ns | 175.0344 ns | 202.0500 ns | 197.9281 ns |     543 B |         - |
| ClearIfNeededString | 184.5136 ns | 5.4611 ns | 8.1740 ns | 164.4971 ns | 201.3768 ns | 192.6909 ns |     543 B |         - |
