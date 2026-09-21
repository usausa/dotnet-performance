# JIT-06: static abstract interface members through a generic type parameter

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.12). Re-run on HX 370 (x86-64-v4) with `--filter "*StaticAbstractCallBenchmark*"` and replace the table. The codegen facts below (dictionary slot load + indirect `call rax` in the shared body, PGO devirtualization of the instance call) do not depend on the machine. Also run under NativeAOT: there is no dynamic PGO there, so the instance-call rows lose their guarded devirtualization and the ranking may change.

- Verdict: the cost of `T.Method()` on a `static abstract` member is decided by **what T is at the call site**, not by the member being static. Struct T or a class T that reaches the JIT with its exact type (the generic method inlined into a caller that names the type): same code as a direct call (19 B, 1.03x). Class T executing the **shared canonical body**: 8.4x (3.80 vs 0.45 ns, 354 B) — a generic-dictionary slot load and an indirect `call rax`, with a `GenericsHelpers.Method` slow path on the first call
- The shared body cannot be rescued by PGO: there is no receiver object to guard on. The instance interface call in the same benchmark **is** rescued — `Apply(IOp)` compiles to `cmp [rcx], MT_OpA` + inlined `lea eax,[rdx+1]` — so on .NET 10 the monomorphic instance call (1.16 ns) beats the shared static call by 3.3x, and even the polymorphic one (2.44 ns, two guards) beats it. Coanet measured the opposite order on .NET 7 before dynamic PGO; that is the part to confirm under NativeAOT, which has no PGO either
- Alternating two class instantiations of the shared body costs nothing extra (3.79 vs 3.80 ns): each instantiation has its own dictionary, the code is the same
- Guidance: keep `static abstract` calls where T is a struct, or where the generic method is small enough to be inlined into a caller that knows the exact T (`AggressiveInlining` on the generic helper, exact type arguments at the call site — the BunnyTail.DependencyInjection / Smart.Data.Accessor shape). A generic method that is itself called from shared code over a class T, or too large to inline, pays the dictionary path on every call; for that shape an instance interface call on a cached implementation object is cheaper on the JIT

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| Direct              | 0.4502 ns | 0.0043 ns | 0.0064 ns | 0.4389 ns | 0.4650 ns | 0.4580 ns |  1.00 |    0.02 |      19 B |         - |          NA |
| StructSaim          | 1.2002 ns | 0.0415 ns | 0.0622 ns | 1.1041 ns | 1.3405 ns | 1.2630 ns |  2.67 |    0.14 |      43 B |         - |          NA |
| ClassSaimInlined    | 0.4653 ns | 0.0242 ns | 0.0347 ns | 0.4268 ns | 0.5237 ns | 0.5155 ns |  1.03 |    0.08 |      19 B |         - |          NA |
| ClassSaimShared     | 3.7985 ns | 0.1404 ns | 0.2102 ns | 3.2986 ns | 4.1105 ns | 4.0351 ns |  8.44 |    0.47 |     354 B |         - |          NA |
| ClassSaimSharedPoly | 3.7894 ns | 0.2832 ns | 0.4239 ns | 3.1108 ns | 4.4180 ns | 4.3349 ns |  8.42 |    0.93 |     382 B |         - |          NA |
| InstanceMono        | 1.1575 ns | 0.0204 ns | 0.0305 ns | 1.1168 ns | 1.2144 ns | 1.2062 ns |  2.57 |    0.08 |      90 B |         - |          NA |
| InstancePoly        | 2.4359 ns | 0.1379 ns | 0.2021 ns | 2.1113 ns | 2.7364 ns | 2.6701 ns |  5.41 |    0.45 |     109 B |         - |          NA |
