# TYP-07: Hash source for Type-keyed lookup (virtual / identity hash / TypeHandle)

- Verdict: adopted - **`TypeHandle.Value` is the best hash source for `Type` keys**, not `RuntimeHelpers.GetHashCode`
- Hit: `type.GetHashCode()` 1.362 ns -> `RuntimeHelpers.GetHashCode(type)` 1.116 ns (0.82x) -> `type.TypeHandle.Value` 0.848 ns (**0.62x**)
- Miss: 1.251 -> 1.197 (0.96x) -> 0.812 ns (**0.65x** against the miss baseline)
- Code size is where the gap is clearest: **403 B -> 355 B -> 192 B**. The virtual call keeps the dispatch; `RuntimeHelpers.GetHashCode` inlines but still reads the object header and checks whether a hash has been assigned; `TypeHandle.Value` is a plain field read of the MethodTable pointer with no branch
- The MethodTable pointer is 8-byte aligned, so the low 3 bits are always zero and must be shifted away before masking - without the shift, a power-of-two bucket table uses only 1/8 of its slots
- Bucket placement differs between the identity hash and the handle hash, so the benchmark holds a separate table per path and `Verify()` checks all three agree on hit and miss
- Applies to runtime `Type` keys only. `TypeHandle.Value` is stable for the process lifetime of a loaded type; under a collectible `AssemblyLoadContext` an unloaded type's address can be reused, which is safe here only because the map holds a strong `Type` reference for as long as the entry exists
- **AOT: not verified.** These numbers are from a JIT run. Under NativeAOT `TypeHandle.Value` yields an MethodTable/EEType pointer rather than the JIT runtime's MethodTable, so both the codegen and the low-bit alignment assumption need re-measuring on an AOT publish before the AOT column can claim ✅
- Related: [TYP-01](TYP-01-TypeMap.md) measures the other axis - a generic `TypeSlot<T>` at 0.09x when the type is known statically, and a runtime-`Type` path at 4.22x. When the type is only known at runtime, a node map keyed on the handle is the shape to reach for

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| VirtualHashHit   | 1.3622 ns | 0.0325 ns | 0.0486 ns | 1.2934 ns | 1.4601 ns | 1.4345 ns |  1.00 |    0.05 |     403 B |         - |          NA |
| IdentityHashHit  | 1.1163 ns | 0.0226 ns | 0.0338 ns | 1.0669 ns | 1.1656 ns | 1.1544 ns |  0.82 |    0.04 |     355 B |         - |          NA |
| HandleHashHit    | 0.8483 ns | 0.0166 ns | 0.0232 ns | 0.8111 ns | 0.8773 ns | 0.8723 ns |  0.62 |    0.03 |     192 B |         - |          NA |
| VirtualHashMiss  | 1.2507 ns | 0.0385 ns | 0.0564 ns | 1.1550 ns | 1.3318 ns | 1.3156 ns |  0.92 |    0.05 |     401 B |         - |          NA |
| IdentityHashMiss | 1.1970 ns | 0.0202 ns | 0.0296 ns | 1.1348 ns | 1.2525 ns | 1.2216 ns |  0.88 |    0.04 |     357 B |         - |          NA |
| HandleHashMiss   | 0.8118 ns | 0.0158 ns | 0.0237 ns | 0.7838 ns | 0.8546 ns | 0.8399 ns |  0.60 |    0.03 |     202 B |         - |          NA |

Ratio is against `VirtualHashHit` for every row (single baseline). The miss axis reads against `VirtualHashMiss`: identity 0.96x, handle **0.65x**.
