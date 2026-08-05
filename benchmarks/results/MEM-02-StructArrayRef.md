# MEM-02: Class-element array vs struct-element array (+ref access)

- Verdict: adopted (ref access; structural memory win)
- StructArrayRef 483.0 ns == ClassArray 493.5 ns (CIs overlap) in this sequential-allocation micro; StructArrayCopy 595.7 ns (1.21x - the 16-byte copy penalty ref access removes)
- The locality/GC win of structs does not show here because class elements were allocated sequentially (allocation order == access order); in aged heaps class arrays scatter
- Structural: 1024 elements = 16 KB contiguous (struct) vs ~40 KB objects + 8 KB reference array (class)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ClassArray      | 401.6 ns | 0.64 ns | 0.89 ns | 399.9 ns | 403.1 ns | 402.8 ns |  1.00 |      52 B |         - |          NA |
| StructArrayCopy | 414.7 ns | 1.67 ns | 2.39 ns | 411.4 ns | 420.9 ns | 417.3 ns |  1.03 |      70 B |         - |          NA |
| StructArrayRef  | 412.9 ns | 3.45 ns | 4.94 ns | 408.3 ns | 431.0 ns | 419.1 ns |  1.03 |      64 B |         - |          NA |
