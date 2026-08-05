# LAB: Pinned (POH) buffer for speed (rejected, R-13)

- Verdict: rejected (for performance purposes)
- POH allocation 961 ns vs normal 49.9 ns (19.3x) + Gen1/Gen2 collections - never allocate POH buffers per operation
- fixed 0.118 ns vs pre-pinned pointer 0.015 ns (code 56 vs 33 B): a real but ~0.1 ns-per-pin difference, visible only in pin-per-iteration hot loops
- POH is a fragmentation countermeasure for long-lived I/O buffers allocated once at startup, not a speed tool

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean        | Error      | StdDev     | Min         | Max           | P90         | Ratio    | RatioSD | Gen0   | Code Size | Gen1   | Gen2   | Allocated | Alloc Ratio |
|-------------------- |------------:|-----------:|-----------:|------------:|--------------:|------------:|---------:|--------:|-------:|----------:|-------:|-------:|----------:|------------:|
| PinWithFixed        |   0.1184 ns |  0.0060 ns |  0.0090 ns |   0.1024 ns |     0.1393 ns |   0.1307 ns |     1.01 |    0.11 |      - |      56 B |      - |      - |         - |          NA |
| PinnedPointerDirect |   0.0149 ns |  0.0047 ns |  0.0067 ns |   0.0013 ns |     0.0241 ns |   0.0227 ns |     0.13 |    0.06 |      - |      33 B |      - |      - |         - |          NA |
| AllocateNormal      |  49.9137 ns |  1.2972 ns |  1.8604 ns |  42.8221 ns |    52.3023 ns |  51.6976 ns |   423.97 |   34.84 | 0.4923 |      46 B |      - |      - |    4120 B |          NA |
| AllocatePinned      | 961.0727 ns | 23.9547 ns | 32.7895 ns | 938.1147 ns | 1,089.3459 ns | 996.6645 ns | 8,163.46 |  659.81 | 1.3084 |     217 B | 1.3084 | 1.3084 |    4120 B |          NA |
