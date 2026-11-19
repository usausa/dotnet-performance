# LAB: ref readonly Current on a struct enumerator (STK-03 follow-up, Gravell 2022)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged on net10, slightly stronger on net11. By-value and `ref` Current are equal on net10 (1.326 / 1.325 ns); on net11 the `ref` form is 0.92x (1.095 vs 1.190 ns)
- Code sizes keep their gap (127 vs 87 B on net11)

### RefEnumeratorBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ValueCurrent | 1.326 ns | 1.190 ns | 0.90 | - → - | 121 B → 127 B |
| RefCurrent | 1.325 ns | 1.095 ns | 0.83 | - → - | 81 B → 87 B |
| SpanRefForeach | 1.185 ns | 1.184 ns | 1.00 | - → - | 79 B → 85 B |
| ForRef | 1.247 ns | 1.228 ns | 0.98 | - → - | 63 B → 69 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.32 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method         | Job                 | Runtime   | Mean     | Error     | StdDev    | Median   | Min       | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------- |-------------------- |---------- |---------:|----------:|----------:|---------:|----------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ValueCurrent   | .NET 10.0           | .NET 10.0 | 1.326 ns | 0.0295 ns | 0.0442 ns | 1.328 ns | 1.2488 ns | 1.409 ns | 1.386 ns |  1.00 |    0.00 |     121 B |         - |          NA |
| RefCurrent     | .NET 10.0           | .NET 10.0 | 1.325 ns | 0.0281 ns | 0.0420 ns | 1.336 ns | 1.2395 ns | 1.405 ns | 1.367 ns |  1.00 |    0.05 |      81 B |         - |          NA |
| SpanRefForeach | .NET 10.0           | .NET 10.0 | 1.185 ns | 0.0097 ns | 0.0146 ns | 1.186 ns | 1.1546 ns | 1.213 ns | 1.200 ns |  0.89 |    0.03 |      79 B |         - |          NA |
| ForRef         | .NET 10.0           | .NET 10.0 | 1.247 ns | 0.0472 ns | 0.0707 ns | 1.196 ns | 1.1714 ns | 1.381 ns | 1.349 ns |  0.94 |    0.06 |      63 B |         - |          NA |
|                |                     |           |          |           |           |          |           |          |          |       |         |           |           |             |
| ValueCurrent   | MediumRun-.NET 11.0 | .NET 11.0 | 1.190 ns | 0.1038 ns | 0.1522 ns | 1.216 ns | 0.9669 ns | 1.405 ns | 1.373 ns |  1.00 |    0.00 |     127 B |         - |          NA |
| RefCurrent     | MediumRun-.NET 11.0 | .NET 11.0 | 1.095 ns | 0.0378 ns | 0.0566 ns | 1.103 ns | 0.9770 ns | 1.203 ns | 1.157 ns |  0.94 |    0.13 |      87 B |         - |          NA |
| SpanRefForeach | MediumRun-.NET 11.0 | .NET 11.0 | 1.184 ns | 0.0093 ns | 0.0137 ns | 1.185 ns | 1.1447 ns | 1.204 ns | 1.196 ns |  1.01 |    0.13 |      85 B |         - |          NA |
| ForRef         | MediumRun-.NET 11.0 | .NET 11.0 | 1.228 ns | 0.0419 ns | 0.0627 ns | 1.199 ns | 1.1180 ns | 1.393 ns | 1.321 ns |  1.05 |    0.14 |      69 B |         - |          NA |

## Earlier record

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
