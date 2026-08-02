# SEQ-05: Incremental delimiter search + deferred compaction

- Verdict: adopted
- 0.58x vs naive full-rescan + per-line compaction (2 KB lines fed in 256 B chunks, 32 KB total)
- Both zero-alloc; the win comes from not re-scanning already-searched bytes and moving data once instead of per line
- Ring wrap-around (two-segment IndexOf) not exercised; measured in flat-buffer form

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| NaiveRescanCompact         | 3.107 μs | 0.0285 μs | 0.0418 μs | 3.004 μs | 3.182 μs | 3.162 μs |  1.00 |    0.02 |   1,629 B |         - |          NA |
| IncrementalDeferredCompact | 1.800 μs | 0.0196 μs | 0.0294 μs | 1.754 μs | 1.865 μs | 1.843 μs |  0.58 |    0.01 |   1,706 B |         - |          NA |
