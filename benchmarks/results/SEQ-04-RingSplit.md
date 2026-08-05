# SEQ-04: Incremental delimiter search + deferred compaction

- Verdict: adopted
- 0.58x vs naive full-rescan + per-line compaction (2 KB lines fed in 256 B chunks, 32 KB total)
- Both zero-alloc; the win comes from not re-scanning already-searched bytes and moving data once instead of per line
- Ring wrap-around (two-segment IndexOf) not exercised; measured in flat-buffer form

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| NaiveRescanCompact         | 1.695 μs | 0.0388 μs | 0.0543 μs | 1.666 μs | 1.904 μs | 1.762 μs |  1.00 |    0.04 |   1,772 B |         - |          NA |
| IncrementalDeferredCompact | 1.128 μs | 0.0054 μs | 0.0079 μs | 1.111 μs | 1.143 μs | 1.138 μs |  0.67 |    0.02 |   1,853 B |         - |          NA |
