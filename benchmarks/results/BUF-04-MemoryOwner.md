# BUF-04: MemoryOwner (scoped pool ownership)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the four lifecycle forms stay within 1.632-1.703 μs on both generations; allocation identical (4,120 / 0 / 32 / 0 B)
- The pool-based forms carry about 1.1 KB more Tier1 code on net11 (4.1 → 5.3 KB) with no time change

### MemoryOwnerBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| NewArray | 1.706 μs | 1.703 μs | 1.00 | 4120 B → 4120 B | 90 B → 90 B |
| ArrayPoolRaw | 1.638 μs | 1.636 μs | 1.00 | - → - | 4,140 B → 5,285 B |
| MemoryOwnerAllocate | 1.639 μs | 1.645 μs | 1.00 | 32 B → 32 B | 4,219 B → 5,364 B |
| TemporaryBufferPooled | 1.637 μs | 1.632 μs | 1.00 | - → - | 4,120 B → 5,281 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.42 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|-------:|----------:|----------:|------------:|
| NewArray              | .NET 10.0           | .NET 10.0 | 1.706 μs | 0.0057 μs | 0.0084 μs | 1.694 μs | 1.726 μs | 1.718 μs |  1.00 | 0.4921 |      90 B |    4120 B |       1.000 |
| ArrayPoolRaw          | .NET 10.0           | .NET 10.0 | 1.638 μs | 0.0053 μs | 0.0078 μs | 1.626 μs | 1.657 μs | 1.647 μs |  0.96 |      - |   4,140 B |         - |       0.000 |
| MemoryOwnerAllocate   | .NET 10.0           | .NET 10.0 | 1.639 μs | 0.0027 μs | 0.0037 μs | 1.631 μs | 1.647 μs | 1.643 μs |  0.96 | 0.0038 |   4,219 B |      32 B |       0.008 |
| TemporaryBufferPooled | .NET 10.0           | .NET 10.0 | 1.637 μs | 0.0055 μs | 0.0077 μs | 1.625 μs | 1.663 μs | 1.645 μs |  0.96 |      - |   4,120 B |         - |       0.000 |
|                       |                     |           |          |           |           |          |          |          |       |        |           |           |             |
| NewArray              | MediumRun-.NET 11.0 | .NET 11.0 | 1.703 μs | 0.0045 μs | 0.0065 μs | 1.691 μs | 1.714 μs | 1.713 μs |  1.00 | 0.4921 |      90 B |    4120 B |       1.000 |
| ArrayPoolRaw          | MediumRun-.NET 11.0 | .NET 11.0 | 1.636 μs | 0.0050 μs | 0.0070 μs | 1.628 μs | 1.656 μs | 1.643 μs |  0.96 |      - |   5,285 B |         - |       0.000 |
| MemoryOwnerAllocate   | MediumRun-.NET 11.0 | .NET 11.0 | 1.645 μs | 0.0105 μs | 0.0154 μs | 1.631 μs | 1.686 μs | 1.673 μs |  0.97 | 0.0038 |   5,364 B |      32 B |       0.008 |
| TemporaryBufferPooled | MediumRun-.NET 11.0 | .NET 11.0 | 1.632 μs | 0.0029 μs | 0.0042 μs | 1.624 μs | 1.642 μs | 1.636 μs |  0.96 |      - |   5,281 B |         - |       0.000 |

## Earlier record

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
