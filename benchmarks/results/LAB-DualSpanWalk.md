# R-02 (was MEM-01): Dual-span walk - indexed vs pre-sliced vs manual ref

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - the indexed dual-span loop still auto-vectorizes (239 ns) and the manual ref walk stays 1.21x slower on net11 (1.23x on net10)

### DualSpanWalkBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| Indexed | 241.2 ns | 239.4 ns | 0.99 | - → - | 149 B → 149 B |
| IndexedPreSliced | 239.4 ns | 239.1 ns | 1.00 | - → - | 159 B → 159 B |
| RefWalk | 296.5 ns | 290.4 ns | 0.98 | - → - | 88 B → 78 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.58 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method           | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| Indexed          | .NET 10.0           | .NET 10.0 | 241.2 ns | 1.65 ns | 2.31 ns | 238.9 ns | 250.0 ns | 243.2 ns |  1.00 |    0.00 |     149 B |         - |          NA |
| IndexedPreSliced | .NET 10.0           | .NET 10.0 | 239.4 ns | 0.56 ns | 0.80 ns | 238.0 ns | 241.3 ns | 240.4 ns |  0.99 |    0.01 |     159 B |         - |          NA |
| RefWalk          | .NET 10.0           | .NET 10.0 | 296.5 ns | 4.36 ns | 6.12 ns | 290.2 ns | 312.9 ns | 304.1 ns |  1.23 |    0.03 |      88 B |         - |          NA |
|                  |                     |           |          |         |         |          |          |          |       |         |           |           |             |
| Indexed          | MediumRun-.NET 11.0 | .NET 11.0 | 239.4 ns | 0.35 ns | 0.49 ns | 238.6 ns | 240.3 ns | 240.2 ns |  1.00 |    0.00 |     149 B |         - |          NA |
| IndexedPreSliced | MediumRun-.NET 11.0 | .NET 11.0 | 239.1 ns | 0.61 ns | 0.83 ns | 237.6 ns | 240.9 ns | 240.2 ns |  1.00 |    0.00 |     159 B |         - |          NA |
| RefWalk          | MediumRun-.NET 11.0 | .NET 11.0 | 290.4 ns | 0.69 ns | 0.99 ns | 288.6 ns | 292.4 ns | 291.6 ns |  1.21 |    0.00 |      78 B |         - |          NA |

## Earlier record

- Verdict: REVERSED on net10 for vectorizable bodies - manual ref walk is 1.25x SLOWER
- Indexed 239.8 ns == PreSliced 239.3 ns (0.23 ns/elem: the JIT auto-vectorizes the indexed dual-span loop)
- RefWalk 299.1 ns (1.25x): Unsafe.Add form defeats auto-vectorization despite smallest code (88 B)
- Manual ref walking remains for non-vectorizable per-element work and sampling access (e.g. SampledNameTable); for plain element-wise loops write the indexed form

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|----------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| Indexed          | 239.8 ns | 1.22 ns | 1.76 ns | 236.8 ns | 244.1 ns | 241.8 ns |  1.00 |     149 B |         - |          NA |
| IndexedPreSliced | 239.3 ns | 1.42 ns | 2.03 ns | 236.6 ns | 245.1 ns | 241.2 ns |  1.00 |     159 B |         - |          NA |
| RefWalk          | 299.1 ns | 1.90 ns | 2.60 ns | 295.8 ns | 305.8 ns | 302.2 ns |  1.25 |      88 B |         - |          NA |
