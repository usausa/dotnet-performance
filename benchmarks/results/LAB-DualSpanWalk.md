# R-02 (was MEM-01): Dual-span walk - indexed vs pre-sliced vs manual ref

- Verdict: REVERSED on net10 for vectorizable bodies - manual ref walk is 1.46x SLOWER
- Indexed 367.7 ns == PreSliced 368.6 ns (0.36 ns/elem: the JIT auto-vectorizes the indexed dual-span loop)
- RefWalk 537.7 ns (1.46x): Unsafe.Add form defeats auto-vectorization despite smallest code (88 B)
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
