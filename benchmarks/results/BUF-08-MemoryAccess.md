# BUF-08: Memory<T> access cost, MemoryManager backing, and array interop

- Verdict: adopted
- Resolving .Span per element costs 3.67x against hoisting it once (3.783 vs 1.030 us)
- Even at chunk granularity, slicing Memory<T> loses to slicing a hoisted Span<T>: 1.10x (array backed) and
  1.13x (MemoryManager backed), CIs do not overlap
- The backing store does not matter once .Span is hoisted (Manager 1.244 vs array 1.281 us), but the code that
  resolves .Span is 1.7x larger for the MemoryManager case (518 vs 297 B)
- MemoryMarshal.TryGetArray removes the copy entirely when handing a Memory<T> to a byte[] + offset + count API:
  0.90x and 4,120 B -> 0 B allocated, code 889 -> 414 B
- TryGetArray returns false for a MemoryManager backed Memory, so the copy fallback has to stay reachable
  (asserted in Verify)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean     | Error     | StdDev    | Median   | Min       | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |---------:|----------:|----------:|---------:|----------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ArraySpanHoistedChunks     | 1.281 μs | 0.0330 μs | 0.0484 μs | 1.253 μs | 1.2343 μs | 1.389 μs | 1.356 μs |  1.00 |    0.05 |     248 B |         - |          NA |
| ArrayMemorySliceChunks     | 1.409 μs | 0.0345 μs | 0.0516 μs | 1.405 μs | 1.3449 μs | 1.512 μs | 1.483 μs |  1.10 |    0.06 |     268 B |         - |          NA |
| ArraySpanHoistedPerElement | 1.030 μs | 0.0300 μs | 0.0449 μs | 1.037 μs | 0.9669 μs | 1.104 μs | 1.084 μs |  0.81 |    0.05 |     201 B |         - |          NA |
| ArraySpanPerElement        | 3.783 μs | 0.0911 μs | 0.1307 μs | 3.748 μs | 3.6281 μs | 4.017 μs | 3.980 μs |  2.96 |    0.15 |     178 B |         - |          NA |
| ManagerSpanHoistedChunks   | 1.244 μs | 0.0309 μs | 0.0462 μs | 1.239 μs | 1.1765 μs | 1.320 μs | 1.308 μs |  0.97 |    0.05 |     297 B |         - |          NA |
| ManagerMemorySliceChunks   | 1.441 μs | 0.0417 μs | 0.0624 μs | 1.427 μs | 1.3513 μs | 1.570 μs | 1.549 μs |  1.13 |    0.06 |     518 B |         - |          NA |

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| ToArrayCopy         | 1.715 μs | 0.0431 μs | 0.0632 μs | 1.643 μs | 1.836 μs | 1.826 μs |  1.00 |    0.05 |     889 B | 0.2460 |    4120 B |        1.00 |
| TryGetArraySegment  | 1.545 μs | 0.0391 μs | 0.0585 μs | 1.471 μs | 1.671 μs | 1.617 μs |  0.90 |    0.05 |     414 B |      - |         - |        0.00 |
| ManagerFallbackCopy | 1.804 μs | 0.0553 μs | 0.0827 μs | 1.700 μs | 1.997 μs | 1.893 μs |  1.05 |    0.06 |   1,255 B | 0.2460 |    4120 B |        1.00 |

