# MEM-02: Class-element array vs struct-element array (+ref access)

- Verdict: adopted (ref access; structural memory win)
- StructArrayRef 483.0 ns == ClassArray 493.5 ns (CIs overlap) in this sequential-allocation micro; StructArrayCopy 595.7 ns (1.21x - the 16-byte copy penalty ref access removes)
- The locality/GC win of structs does not show here because class elements were allocated sequentially (allocation order == access order); in aged heaps class arrays scatter
- Structural: 1024 elements = 16 KB contiguous (struct) vs ~40 KB objects + 8 KB reference array (class)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ClassArray      | 493.5 ns | 14.21 ns | 21.27 ns | 459.0 ns | 530.7 ns | 524.5 ns |  1.00 |    0.06 |      52 B |         - |          NA |
| StructArrayCopy | 595.7 ns | 12.57 ns | 18.81 ns | 564.2 ns | 623.7 ns | 617.3 ns |  1.21 |    0.06 |      70 B |         - |          NA |
| StructArrayRef  | 483.0 ns | 10.13 ns | 15.17 ns | 455.6 ns | 512.3 ns | 499.8 ns |  0.98 |    0.05 |      64 B |         - |          NA |
