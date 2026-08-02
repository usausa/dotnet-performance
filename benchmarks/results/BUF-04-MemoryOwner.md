# BUF-04: MemoryOwner (scoped pool ownership)

- Verdict: adopted (safety at ~zero cost)
- 4 KB fill+sum lifecycle: 2.5-3.0 us dominated by fill+sum; wrapper vs raw Rent/Return CIs overlap (recorded as measurement-noise) - wrapper cost is below measurement resolution
- Allocation: new byte[] 4,120 B / raw pool 0 B / MemoryOwner 32 B (owner instance only) / TemporaryBuffer 0 B
- Value = using-enforced return + exact-length Span/Memory + double-dispose guard (CON-02); prefer TemporaryBuffer (BUF-05) inside sync scopes, MemoryOwner across async boundaries

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| NewArray              | 2.490 μs | 0.3145 μs | 0.4708 μs | 2.097 μs | 3.439 μs | 3.370 μs |  1.03 |    0.25 | 0.2441 |      90 B |    4120 B |       1.000 |
| ArrayPoolRaw          | 3.042 μs | 0.1845 μs | 0.2647 μs | 2.623 μs | 3.528 μs | 3.403 μs |  1.26 |    0.22 |      - |   2,564 B |         - |       0.000 |
| MemoryOwnerAllocate   | 2.836 μs | 0.2792 μs | 0.4179 μs | 1.941 μs | 3.334 μs | 3.221 μs |  1.17 |    0.25 |      - |   2,643 B |      32 B |       0.008 |
| TemporaryBufferPooled | 2.950 μs | 0.1267 μs | 0.1897 μs | 2.379 μs | 3.194 μs | 3.150 μs |  1.22 |    0.20 |      - |   2,544 B |         - |       0.000 |
