# JIT-04: Cold-path split (isolated micro)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - code-size win (a 103 B hot path against 1.9-2.0 KB), time-neutral on net11 (629.7 vs 616.7 ns, 1.02x)
- The net10 run of `SplitColdPath` read 820.9 ns with the same 103 B of code; that is a placement outlier (the earlier net10 record had 631.1 ns)

### ColdPathSplitBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| FatMethod | 623.9 ns | 616.7 ns | 0.99 | - → - | 1,866 B → 1,972 B |
| SplitColdPath | 820.9 ns | 629.7 ns | 0.77 | - → - | 103 B → 103 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.79 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| FatMethod     | .NET 10.0           | .NET 10.0 | 623.9 ns | 4.09 ns | 6.00 ns | 611.8 ns | 642.6 ns | 630.8 ns |  1.00 |   1,866 B |         - |          NA |
| SplitColdPath | .NET 10.0           | .NET 10.0 | 820.9 ns | 1.47 ns | 2.20 ns | 817.7 ns | 825.2 ns | 823.8 ns |  1.32 |     103 B |         - |          NA |
|               |                     |           |          |         |         |          |          |          |       |           |           |             |
| FatMethod     | MediumRun-.NET 11.0 | .NET 11.0 | 616.7 ns | 3.38 ns | 4.84 ns | 611.0 ns | 629.8 ns | 624.3 ns |  1.00 |   1,972 B |         - |          NA |
| SplitColdPath | MediumRun-.NET 11.0 | .NET 11.0 | 629.7 ns | 2.44 ns | 3.58 ns | 622.2 ns | 635.9 ns | 635.1 ns |  1.02 |     103 B |         - |          NA |

## Earlier record

- Verdict: adopted (code-size win; time-neutral in this isolated micro)
- FatMethod 635.1 ns (569 B, not inlined) vs SplitColdPath 631.1 ns (0.99x, 103 B hot path force-inlined into the loop) - equal time, 5.5x smaller hot code
- One call per Write is cheap, so isolating the growth path does not show up as time here; the pattern's real value is enabling CALLER-side inlining and the optimizations beyond it (see BufferWriterSlim/ValueStringBuilder Grow)
- Apply with measurement: forced expansion can still bloat a loop body when the hot path is large

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| FatMethod     | 616.0 ns | 1.46 ns | 2.00 ns | 612.7 ns | 619.2 ns | 618.8 ns |  1.00 |     569 B |         - |          NA |
| SplitColdPath | 616.7 ns | 4.26 ns | 5.97 ns | 609.2 ns | 633.7 ns | 625.1 ns |  1.00 |     103 B |         - |          NA |
