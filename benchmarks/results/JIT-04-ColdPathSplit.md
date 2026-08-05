# JIT-04: Cold-path split (isolated micro)

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
