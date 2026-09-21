# LAB: ref readonly Current on a struct enumerator (STK-03 follow-up, Gravell 2022)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.12). Re-run on HX 370 with `--filter "*RefEnumeratorBenchmark*"`. The codegen fact (a 64-byte copy per iteration in the by-value form, none in the ref forms) does not depend on the machine.

- Verdict: when the loop body hands the element to a method (here a non-inlined `Consume(in Entry)`), a struct enumerator whose `Current` returns `Entry` by value copies the 64-byte element on every iteration (two `vmovdqu` load/store pairs into `[rsp+20..60]`, 124 B of code); `ref readonly Entry Current` passes the array slot's address (`lea rcx,[rsi+rcx+10]`, 81 B) and lands within noise of `foreach (ref readonly var e in span)` and of `for` + `ref readonly` local: **0.80x** (1.27 vs 1.62 ns per element)
- R-04 stays true for the other shape: when the body only reads a field, the JIT loads the field directly and no copy exists either way, so this is only worth doing when elements are passed on by reference or are large and multi-field
- API consequence for STK-03: a struct enumerator over struct elements should expose `ref readonly T Current` (C# 7.3+); callers write `foreach (ref readonly var x in ...)`, and a plain `foreach (var x in ...)` still compiles (it copies at the call site, which is the caller's choice)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method         | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ValueCurrent   | 1.616 ns | 0.1231 ns | 0.1725 ns | 1.661 ns | 1.383 ns | 1.876 ns | 1.835 ns |  1.01 |    0.15 |     124 B |         - |          NA |
| RefCurrent     | 1.274 ns | 0.0979 ns | 0.1465 ns | 1.200 ns | 1.112 ns | 1.547 ns | 1.484 ns |  0.80 |    0.12 |      81 B |         - |          NA |
| SpanRefForeach | 1.221 ns | 0.0710 ns | 0.1019 ns | 1.228 ns | 1.108 ns | 1.376 ns | 1.365 ns |  0.76 |    0.10 |      79 B |         - |          NA |
| ForRef         | 1.222 ns | 0.0141 ns | 0.0207 ns | 1.223 ns | 1.179 ns | 1.258 ns | 1.249 ns |  0.76 |    0.08 |      63 B |         - |          NA |
