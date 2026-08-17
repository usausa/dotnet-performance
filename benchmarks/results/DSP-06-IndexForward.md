# DSP-06: Index-forwarding pipelines (4 pass-through hops)

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
