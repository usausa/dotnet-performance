# JIT-06: static abstract interface members through a generic type parameter

- Verdict: the cost of `T.Method()` on a `static abstract` member is decided by **what T is at the call site**, not by the member being static. Struct T or a class T that reaches the JIT with its exact type (the generic method inlined into a caller that names the type): same code as a direct call (19 B, 1.00x). Class T executing the **shared canonical body**: 4.75x (1.83 vs 0.39 ns, 354 B) — a generic-dictionary slot load and an indirect `call rax`, with a `GenericsHelpers.Method` slow path on the first call. The B550H (x86-64-v3) provisional run had the same order with a larger gap (8.44x)
- The shared body cannot be rescued by PGO: there is no receiver object to guard on. The instance interface call in the same benchmark **is** rescued — `Apply(IOp)` compiles to `cmp [rcx], MT_OpA` + inlined `lea eax,[rdx+1]`, with the other type falling back to the interface call — so on .NET 10 the monomorphic instance call (1.17 ns) beats the shared static call by 1.6x (3.3x on x86-64-v3), and the polymorphic one (1.73 ns) beats it too (1.06x, CIs disjoint). Coanet measured the opposite order on .NET 7 before dynamic PGO
- Alternating two class instantiations of the shared body costs 1.23x on this core (2.25 vs 1.83 ns; no difference on x86-64-v3): the indirect-call target changes every call. Each instantiation has its own dictionary, the code is the same (382 vs 354 B)
- **NativeAOT does not restore the .NET 7 order.** Within the AOT run (its own `Direct` baseline, 0.386 ns): shared SAIM 5.27x (2.04 ns), monomorphic interface call 2.67x (1.03 ns), polymorphic 2.89x (1.12 ns). With no GDV the interface dispatch costs the same whether it is monomorphic or polymorphic, and the shared generic body still pays the dictionary lookup plus the indirect call — it is 2.0x behind the monomorphic and 1.8x behind the polymorphic interface call. Struct T (1.01 ns) and the inlined class T (0.386 ns) stay at direct-call speed under AOT as well
- Guidance: keep `static abstract` calls where T is a struct, or where the generic method is small enough to be inlined into a caller that knows the exact T (`AggressiveInlining` on the generic helper, exact type arguments at the call site — the BunnyTail.DependencyInjection / Smart.Data.Accessor shape). A generic method that is itself called from shared code over a class T, or too large to inline, pays the dictionary path on every call on JIT and AOT alike; for that shape an instance interface call on a cached implementation object is cheaper on both runtimes

## x86-64-v4, JIT (DisassemblyDiagnoser)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| Direct              | 0.3855 ns | 0.0007 ns | 0.0010 ns | 0.3843 ns | 0.3883 ns | 0.3871 ns |  1.00 |    0.00 |      19 B |         - |          NA |
| StructSaim          | 1.1820 ns | 0.0062 ns | 0.0093 ns | 1.1610 ns | 1.1963 ns | 1.1935 ns |  3.07 |    0.03 |      43 B |         - |          NA |
| ClassSaimInlined    | 0.3848 ns | 0.0003 ns | 0.0004 ns | 0.3839 ns | 0.3857 ns | 0.3853 ns |  1.00 |    0.00 |      19 B |         - |          NA |
| ClassSaimShared     | 1.8293 ns | 0.0122 ns | 0.0171 ns | 1.8126 ns | 1.8731 ns | 1.8575 ns |  4.75 |    0.05 |     354 B |         - |          NA |
| ClassSaimSharedPoly | 2.2538 ns | 0.0236 ns | 0.0353 ns | 2.1983 ns | 2.3074 ns | 2.2935 ns |  5.85 |    0.09 |     382 B |         - |          NA |
| InstanceMono        | 1.1650 ns | 0.0125 ns | 0.0186 ns | 1.1350 ns | 1.2077 ns | 1.1900 ns |  3.02 |    0.05 |      90 B |         - |          NA |
| InstancePoly        | 1.7256 ns | 0.0156 ns | 0.0223 ns | 1.6925 ns | 1.7832 ns | 1.7503 ns |  4.48 |    0.06 |     109 B |         - |          NA |

## JIT vs NativeAOT, same MediumRun settings (no DisassemblyDiagnoser, so no Code Size column)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]         : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 10.0      : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  NativeAOT 10.0 : .NET 10.0.12, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job            | Runtime        | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------- |--------------- |--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| Direct              | .NET 10.0      | .NET 10.0      | 0.3859 ns | 0.0007 ns | 0.0010 ns | 0.3846 ns | 0.3880 ns | 0.3871 ns |  1.00 |    0.00 |         - |          NA |
| StructSaim          | .NET 10.0      | .NET 10.0      | 1.1871 ns | 0.0072 ns | 0.0107 ns | 1.1657 ns | 1.2068 ns | 1.1988 ns |  3.08 |    0.03 |         - |          NA |
| ClassSaimInlined    | .NET 10.0      | .NET 10.0      | 0.3862 ns | 0.0007 ns | 0.0010 ns | 0.3847 ns | 0.3891 ns | 0.3872 ns |  1.00 |    0.00 |         - |          NA |
| ClassSaimShared     | .NET 10.0      | .NET 10.0      | 1.8408 ns | 0.0084 ns | 0.0120 ns | 1.8206 ns | 1.8659 ns | 1.8585 ns |  4.77 |    0.03 |         - |          NA |
| ClassSaimSharedPoly | .NET 10.0      | .NET 10.0      | 2.2669 ns | 0.0348 ns | 0.0510 ns | 2.1791 ns | 2.3748 ns | 2.3426 ns |  5.87 |    0.13 |         - |          NA |
| InstanceMono        | .NET 10.0      | .NET 10.0      | 1.1567 ns | 0.0191 ns | 0.0285 ns | 1.1134 ns | 1.1991 ns | 1.1940 ns |  3.00 |    0.07 |         - |          NA |
| InstancePoly        | .NET 10.0      | .NET 10.0      | 1.7270 ns | 0.0089 ns | 0.0128 ns | 1.7101 ns | 1.7626 ns | 1.7442 ns |  4.47 |    0.03 |         - |          NA |
| Direct              | NativeAOT 10.0 | NativeAOT 10.0 | 0.3862 ns | 0.0005 ns | 0.0007 ns | 0.3850 ns | 0.3879 ns | 0.3867 ns |  1.00 |    0.00 |         - |          NA |
| StructSaim          | NativeAOT 10.0 | NativeAOT 10.0 | 1.0143 ns | 0.0031 ns | 0.0044 ns | 1.0069 ns | 1.0258 ns | 1.0188 ns |  2.63 |    0.01 |         - |          NA |
| ClassSaimInlined    | NativeAOT 10.0 | NativeAOT 10.0 | 0.3861 ns | 0.0004 ns | 0.0006 ns | 0.3851 ns | 0.3874 ns | 0.3867 ns |  1.00 |    0.00 |         - |          NA |
| ClassSaimShared     | NativeAOT 10.0 | NativeAOT 10.0 | 2.0357 ns | 0.0058 ns | 0.0082 ns | 2.0240 ns | 2.0576 ns | 2.0465 ns |  5.27 |    0.02 |         - |          NA |
| ClassSaimSharedPoly | NativeAOT 10.0 | NativeAOT 10.0 | 2.5458 ns | 0.0339 ns | 0.0486 ns | 2.4444 ns | 2.6748 ns | 2.5895 ns |  6.60 |    0.12 |         - |          NA |
| InstanceMono        | NativeAOT 10.0 | NativeAOT 10.0 | 1.0310 ns | 0.0165 ns | 0.0236 ns | 1.0046 ns | 1.1177 ns | 1.0542 ns |  2.67 |    0.06 |         - |          NA |
| InstancePoly        | NativeAOT 10.0 | NativeAOT 10.0 | 1.1167 ns | 0.0032 ns | 0.0043 ns | 1.1084 ns | 1.1257 ns | 1.1214 ns |  2.89 |    0.01 |         - |          NA |

Ratio is against `Direct` on .NET 10.0 for every row. Read within each runtime instead, against its own `Direct`:

| Shape | JIT | vs JIT direct | NativeAOT | vs AOT direct |
|---|---:|---:|---:|---:|
| Direct call | 0.386 ns | 1.00 | 0.386 ns | 1.00 |
| Struct T (`Saim<AddOp>`) | 1.187 ns | 3.08 | 1.014 ns | 2.63 |
| Class T inlined into the caller | 0.386 ns | 1.00 | 0.386 ns | 1.00 |
| Class T, shared body | 1.841 ns | 4.77 | 2.036 ns | **5.27** |
| Class T, shared body, two instantiations alternating | 2.267 ns | 5.87 | 2.546 ns | 6.59 |
| Instance interface call (monomorphic) | 1.157 ns | 3.00 | 1.031 ns | 2.67 |
| Instance interface call (polymorphic, 2 types) | 1.727 ns | 4.48 | 1.117 ns | **2.89** |

The JIT rows of this run match the DisassemblyDiagnoser run above within 0.02 ns, so the two tables can be read together.

## Reproducing

The repository's `BenchmarkConfig` carries `DisassemblyDiagnoser`, which NativeAOT does not support, so the two-runtime comparison is run from `benchmarks/PerformancePatterns.AotHarness`, which links this class and substitutes a diagnoser-free config; the linked class's MediumRun attribute provides the JIT job and the NativeAOT job is added on the command line (`vswhere.exe` on `PATH` for the ILCompiler link step):

```
cd benchmarks/PerformancePatterns.AotHarness
dotnet run -c Release -- --filter "*StaticAbstractCallBenchmark*" --runtimes nativeaot10.0 --job medium
```
