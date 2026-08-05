# JIT-05: RuntimeHelpers.IsReferenceOrContainsReferences branch

- Verdict: adopted
- Clear skipped for int[1024]: 19.0 ns -> 0.008 ns (code 510 B -> 28 B)
- The skip path is now indistinguishable from an empty loop (0.19 ns on the previous Ryzen 9 5900X baseline): the JIT folds the intrinsic to a constant and the whole branch disappears
- Zero measured overhead when clear is still required (string[]: 101.5 vs 102.4 ns, same code size)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean        | Error     | StdDev    | Min         | Max         | P90         | Code Size | Allocated |
|-------------------- |------------:|----------:|----------:|------------:|------------:|------------:|----------:|----------:|
| ClearAlwaysInt      |  18.9921 ns | 0.1033 ns | 0.1515 ns |  18.7426 ns |  19.2092 ns |  19.1336 ns |     504 B |         - |
| ClearIfNeededInt    |   0.0082 ns | 0.0025 ns | 0.0036 ns |   0.0010 ns |   0.0165 ns |   0.0121 ns |      28 B |         - |
| ClearAlwaysString   | 102.4121 ns | 1.7971 ns | 2.5192 ns | 101.1673 ns | 111.7872 ns | 103.3811 ns |     543 B |         - |
| ClearIfNeededString | 101.5426 ns | 0.2118 ns | 0.3104 ns | 101.1049 ns | 102.3920 ns | 101.9654 ns |     543 B |         - |
