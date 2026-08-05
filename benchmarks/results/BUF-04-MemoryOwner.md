# BUF-04: MemoryOwner (scoped pool ownership)

- Verdict: adopted (safety at ~zero cost)
- 4 KB fill+sum lifecycle: raw Rent/Return 1.633 us, MemoryOwner 1.649 us, TemporaryBuffer 1.640 us - ranges overlap, the wrapper cost is below measurement resolution
- Allocation: new byte[] 4,120 B / raw pool 0 B / MemoryOwner 32 B (owner instance only) / TemporaryBuffer 0 B
- Value = using-enforced return + exact-length Span/Memory + double-dispose guard (CON-01); prefer TemporaryBuffer (BUF-05) inside sync scopes, MemoryOwner across async boundaries

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|-------:|----------:|----------:|------------:|
| NewArray              | 1.705 μs | 0.0128 μs | 0.0176 μs | 1.686 μs | 1.748 μs | 1.738 μs |  1.00 | 0.4921 |      90 B |    4120 B |       1.000 |
| ArrayPoolRaw          | 1.633 μs | 0.0036 μs | 0.0050 μs | 1.624 μs | 1.641 μs | 1.639 μs |  0.96 |      - |   2,564 B |         - |       0.000 |
| MemoryOwnerAllocate   | 1.649 μs | 0.0106 μs | 0.0149 μs | 1.628 μs | 1.684 μs | 1.672 μs |  0.97 | 0.0038 |   2,643 B |      32 B |       0.008 |
| TemporaryBufferPooled | 1.640 μs | 0.0088 μs | 0.0120 μs | 1.623 μs | 1.672 μs | 1.659 μs |  0.96 |      - |   2,544 B |         - |       0.000 |
