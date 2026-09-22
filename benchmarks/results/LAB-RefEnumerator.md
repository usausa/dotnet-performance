# LAB: ref readonly Current on a struct enumerator (STK-03 follow-up, Gravell 2022)

- Verdict: when the loop body hands the element to a method (here a non-inlined `Consume(in Entry)`), a struct enumerator whose `Current` returns `Entry` by value copies the 64-byte element on every iteration; `ref readonly Entry Current` passes the array slot's address (`lea rcx,[rsi+rcx+10]`, 81 B) and compiles to the same shape as `foreach (ref readonly var e in span)` and `for` + `ref readonly` local. The copy is the machine-independent fact; its time cost is not
- x86-64-v4 (this run): the copy is a single AVX-512 pair (`vmovdqu32 zmm0,[rsi+rcx+10]` / `vmovdqu32 [rsp+20],zmm0`, plus a zeroing store, 121 B) and hides under the `Consume` call. By value 1.210 ns; the three ref forms 1.188 / 1.312 / 1.315 ns — a 10% placement spread across identical instruction sequences that brackets the by-value figure, so **no time difference** on this core (RefCurrent is nominally 1.08x, CIs disjoint, but SpanRefForeach with the same body is 0.98x)
- x86-64-v3 (B550H provisional run): the same copy was four `vmovdqu` ymm pairs (124 B) and the ref form measured **0.80x** (1.274 vs 1.616 ns). The gain exists only where the copy is not free
- R-04 stays true for the other shape: when the body only reads a field, the JIT loads the field directly and no copy exists either way, so this is only worth doing when elements are passed on by reference or are large and multi-field
- API consequence for STK-03: a struct enumerator over struct elements should expose `ref readonly T Current` (C# 7.3+) on the codegen axis (no per-iteration copy, 81 vs 121 B); callers write `foreach (ref readonly var x in ...)`, and a plain `foreach (var x in ...)` still compiles (it copies at the call site, which is the caller's choice)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method         | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ValueCurrent   | 1.210 ns | 0.0109 ns | 0.0160 ns | 1.164 ns | 1.231 ns | 1.228 ns |  1.00 |    0.02 |     121 B |         - |          NA |
| RefCurrent     | 1.312 ns | 0.0286 ns | 0.0428 ns | 1.215 ns | 1.382 ns | 1.352 ns |  1.08 |    0.04 |      81 B |         - |          NA |
| SpanRefForeach | 1.188 ns | 0.0107 ns | 0.0159 ns | 1.149 ns | 1.208 ns | 1.203 ns |  0.98 |    0.02 |      79 B |         - |          NA |
| ForRef         | 1.315 ns | 0.0406 ns | 0.0608 ns | 1.172 ns | 1.394 ns | 1.380 ns |  1.09 |    0.05 |      63 B |         - |          NA |
