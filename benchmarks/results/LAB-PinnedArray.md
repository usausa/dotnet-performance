# LAB: Pinned (POH) buffer for speed (rejected, R-13)

- Verdict: rejected (for performance purposes)
- fixed pinning is ~free (0.74 ns); POH pointer access not faster (0.85 ns)
- POH allocation 17.5x vs normal + Gen2 collections
- POH is for long-lived I/O buffers (fragmentation avoidance), allocate once at startup

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean          | Error      | StdDev     | Min           | Max           | P90           | Ratio    | RatioSD | Gen0   | Code Size | Gen1   | Gen2   | Allocated | Alloc Ratio |
|-------------------- |--------------:|-----------:|-----------:|--------------:|--------------:|--------------:|---------:|--------:|-------:|----------:|-------:|-------:|----------:|------------:|
| PinWithFixed        |     0.7394 ns |  0.0089 ns |  0.0118 ns |     0.7221 ns |     0.7698 ns |     0.7550 ns |     1.00 |    0.02 |      - |      56 B |      - |      - |         - |          NA |
| PinnedPointerDirect |     0.8514 ns |  0.0067 ns |  0.0089 ns |     0.8382 ns |     0.8719 ns |     0.8630 ns |     1.15 |    0.02 |      - |      33 B |      - |      - |         - |          NA |
| AllocateNormal      |    79.0096 ns |  3.2665 ns |  4.4712 ns |    72.0148 ns |    88.0862 ns |    86.3869 ns |   106.88 |    6.16 | 0.2462 |      46 B |      - |      - |    4120 B |          NA |
| AllocatePinned      | 1,380.9222 ns | 14.3044 ns | 20.0528 ns | 1,342.7143 ns | 1,429.7455 ns | 1,399.9497 ns | 1,868.01 |   39.32 | 1.3084 |     217 B | 1.3084 | 1.3084 |    4120 B |          NA |
