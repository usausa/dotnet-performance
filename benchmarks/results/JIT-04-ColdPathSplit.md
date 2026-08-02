# JIT-04: Cold-path split (isolated micro)

- Verdict: conditional - in this isolated tight loop the split+AggressiveInlining form measured 1.09x SLOWER
- FatMethod 1.253 us (569 B, not inlined) vs SplitColdPath 1.361 us (103 B hot path, force-inlined into the loop)
- Interpretation: one call per Write is cheap; forced expansion enlarged the loop body (consistent with JIT-01's code-bloat caution)
- The pattern's real value is enabling CALLER-side inlining and downstream optimizations (see BufferWriterSlim/ValueStringBuilder Grow); apply with measurement, not by default

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| FatMethod     | 1.253 μs | 0.0264 μs | 0.0387 μs | 1.213 μs | 1.377 μs | 1.293 μs |  1.00 |    0.04 |     569 B |         - |          NA |
| SplitColdPath | 1.361 μs | 0.0253 μs | 0.0371 μs | 1.308 μs | 1.446 μs | 1.421 μs |  1.09 |    0.04 |     103 B |         - |          NA |
