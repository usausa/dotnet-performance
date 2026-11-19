# DSP-06: Index-forwarding pipelines (4 pass-through hops)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: **changed in degree on net11 - index forwarding and the precomposed chain are now equal**
- net11: closure compose 29.35 ns → cached continuation 0.45x → precomposed chain **0.34x (9.99 ns)** → index forward **0.33x (9.65 ns)**; on net10 the order of the earlier record holds (0.51x / 0.39x / 0.32x)
- The cached continuation and the precomposed chain got 0.84-0.85x faster on net11; index forward did not move. Per-hop closure allocation 488 → 456 B (#99200)
- Index forward remains the best shape, but its lead over the simpler build-once chain is 0.3 ns on net11

### IndexForwardBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ClosureCompose | 30.350 ns | 29.352 ns | 0.97 | 488 B → 456 B | 3,215 B → 4,110 B |
| CachedContinuation | 15.613 ns | 13.094 ns | 0.84 | 72 B → 72 B | 3,025 B → 3,916 B |
| PrecomposedChain | 11.764 ns | 9.991 ns | 0.85 | 72 B → 72 B | 2,620 B → 3,490 B |
| IndexForward | 9.630 ns | 9.652 ns | 1.00 | 72 B → 72 B | 2,706 B → 3,656 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.19 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| ClosureCompose     | .NET 10.0           | .NET 10.0 | 30.350 ns | 0.1812 ns | 0.2480 ns | 29.855 ns | 30.834 ns | 30.637 ns |  1.00 |    0.00 | 0.0583 |   3,215 B |     488 B |        1.00 |
| CachedContinuation | .NET 10.0           | .NET 10.0 | 15.613 ns | 1.6898 ns | 2.3130 ns | 13.257 ns | 18.129 ns | 17.923 ns |  0.51 |    0.07 | 0.0086 |   3,025 B |      72 B |        0.15 |
| PrecomposedChain   | .NET 10.0           | .NET 10.0 | 11.764 ns | 0.0666 ns | 0.0976 ns | 11.642 ns | 12.030 ns | 11.892 ns |  0.39 |    0.00 | 0.0086 |   2,620 B |      72 B |        0.15 |
| IndexForward       | .NET 10.0           | .NET 10.0 |  9.630 ns | 0.1102 ns | 0.1649 ns |  9.372 ns |  9.867 ns |  9.854 ns |  0.32 |    0.01 | 0.0086 |   2,706 B |      72 B |        0.15 |
|                    |                     |           |           |           |           |           |           |           |       |         |        |           |           |             |
| ClosureCompose     | MediumRun-.NET 11.0 | .NET 11.0 | 29.352 ns | 0.1620 ns | 0.2323 ns | 29.019 ns | 30.123 ns | 29.557 ns |  1.00 |    0.00 | 0.0545 |   4,110 B |     456 B |        1.00 |
| CachedContinuation | MediumRun-.NET 11.0 | .NET 11.0 | 13.094 ns | 0.0630 ns | 0.0884 ns | 12.916 ns | 13.319 ns | 13.165 ns |  0.45 |    0.00 | 0.0086 |   3,916 B |      72 B |        0.16 |
| PrecomposedChain   | MediumRun-.NET 11.0 | .NET 11.0 |  9.991 ns | 0.0443 ns | 0.0649 ns |  9.880 ns | 10.107 ns | 10.076 ns |  0.34 |    0.00 | 0.0086 |   3,490 B |      72 B |        0.16 |
| IndexForward       | MediumRun-.NET 11.0 | .NET 11.0 |  9.652 ns | 0.0902 ns | 0.1294 ns |  9.492 ns | 10.024 ns |  9.748 ns |  0.33 |    0.01 | 0.0086 |   3,656 B |      72 B |        0.16 |

## Earlier record

- Use case: any "N interceptors wrap a terminal handler" pipeline invoked per request or per message - middleware stacks, HTTP/RPC client policy chains (retry, logging, auth), in-process message bus interceptors. The cost that matters is per invocation, not per build
- Verdict: adopted - **index forwarding is the best shape**; any of the three beats per-hop closure allocation
- Per-hop closure compose 32.5 ns / 488 B -> cached continuation 13.6 ns (0.42x) -> precomposed chain 11.1 ns (0.34x) -> **index forward 8.85 ns (0.27x)**
- Allocation collapses 488 B -> 72 B (**0.15x**) for all three. The residual 72 B is the benchmark's own `async Task<long>` return, so none of the three allocates per hop
- Index forwarding (Azure.Core `HttpPipeline` style: policy array + advancing index) wins because there is no delegate at all - the hop is a virtual call with two extra arguments, and it stays stateless and reentrant
- The precomposed chain (ASP.NET Core middleware style) is within 2.3 ns and is the simpler build-once shape; its closures are allocated at setup, not per publish
- The cached continuation needs a per-invocation context object to hold the advancing index, which drags a pool in behind it - it is the weakest of the three and buys nothing over the other two
- The precomposed chain is [DSP-05](DSP-05-PipelineCompose.md) measured against the alternatives; this entry exists because index forwarding beats it
- Code size ranks the same way: 1,809 / 1,900 B for the two winners vs 2,214 / 2,409 B

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| ClosureCompose     | 32.514 ns | 0.7779 ns | 1.0905 ns | 30.292 ns | 35.175 ns | 33.578 ns |  1.00 |    0.05 |   2,409 B | 0.0583 |     488 B |        1.00 |
| CachedContinuation | 13.591 ns | 0.3683 ns | 0.5282 ns | 12.766 ns | 14.892 ns | 14.179 ns |  0.42 |    0.02 |   2,214 B | 0.0086 |      72 B |        0.15 |
| PrecomposedChain   | 11.130 ns | 0.1970 ns | 0.2825 ns | 10.563 ns | 11.695 ns | 11.513 ns |  0.34 |    0.01 |   1,809 B | 0.0086 |      72 B |        0.15 |
| IndexForward       |  8.853 ns | 0.1355 ns | 0.1900 ns |  8.490 ns |  9.119 ns |  9.083 ns |  0.27 |    0.01 |   1,900 B | 0.0086 |      72 B |        0.15 |
