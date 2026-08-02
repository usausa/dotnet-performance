# MEM-01: Dual-span walk - indexed vs pre-sliced vs manual ref

- Verdict: REVERSED on net10 for vectorizable bodies - manual ref walk is 1.46x SLOWER
- Indexed 367.7 ns == PreSliced 368.6 ns (0.36 ns/elem: the JIT auto-vectorizes the indexed dual-span loop)
- RefWalk 537.7 ns (1.46x): Unsafe.Add form defeats auto-vectorization despite smallest code (88 B)
- Manual ref walking remains for non-vectorizable per-element work and sampling access (e.g. SampledNameTable); for plain element-wise loops write the indexed form

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| Indexed          | 367.7 ns |  5.86 ns |  8.78 ns | 353.9 ns | 382.0 ns | 379.2 ns |  1.00 |    0.03 |     149 B |         - |          NA |
| IndexedPreSliced | 368.6 ns |  8.30 ns | 12.42 ns | 352.4 ns | 399.1 ns | 383.2 ns |  1.00 |    0.04 |     159 B |         - |          NA |
| RefWalk          | 537.7 ns | 12.60 ns | 18.86 ns | 505.6 ns | 593.3 ns | 556.8 ns |  1.46 |    0.06 |      88 B |         - |          NA |
