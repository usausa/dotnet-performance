# STK-11: The return type of a using scope

- Verdict: **adopted** for every shape in which the scope escapes; **no difference** for the synchronous `using`
  whose helper is inlined
- First measurement in this repository, on the reference environment (HX 370, x86-64-v4, .NET 11.0.0 JIT)
- Inlined synchronous `using`: 0.808 vs 0.803 ns, both 0 B and 19 B of code - the instruction sequences match
  apart from a branch target address, because Tier1 escape analysis removes the box
- Non-inlined helper: 3.857 ns / 24 B → 1.603 ns / 0 B (0.42x)
- `async` method: 9.623 ns / 96 B → 8.090 ns / 72 B (0.84x); the 24 B difference is the box, the 72 B left on
  both sides is the `Task<int>` itself (the returned count changes every call, so no cached task is used)
- Delegate-carrying scope: 3.291 ns / 56 B → 0.803 ns / 0 B for the struct scope holding the instance

## ScopeBoxingBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.22 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| BoxedScope          | 0.8084 ns | 0.0059 ns | 0.0084 ns | 0.7988 ns | 0.8292 ns | 0.8209 ns |  1.00 |    0.00 |      19 B |      - |         - |          NA |
| BoxedScopeNoInline  | 3.8569 ns | 0.1458 ns | 0.2091 ns | 3.6644 ns | 4.4577 ns | 4.1748 ns |  4.77 |    0.26 |     130 B | 0.0029 |      24 B |          NA |
| StructScope         | 0.8029 ns | 0.0028 ns | 0.0038 ns | 0.7959 ns | 0.8109 ns | 0.8077 ns |  0.99 |    0.01 |      19 B |      - |         - |          NA |
| StructScopeNoInline | 1.6034 ns | 0.0056 ns | 0.0079 ns | 1.5920 ns | 1.6239 ns | 1.6120 ns |  1.98 |    0.02 |      43 B |      - |         - |          NA |
| DelegateScope       | 3.2909 ns | 0.0348 ns | 0.0476 ns | 3.2227 ns | 3.4128 ns | 3.3415 ns |  4.07 |    0.07 |     106 B | 0.0067 |      56 B |          NA |
| BoxedScopeAsync     | 9.6230 ns | 0.0651 ns | 0.0868 ns | 9.4742 ns | 9.7920 ns | 9.7308 ns | 11.90 |    0.16 |   3,008 B | 0.0115 |      96 B |          NA |
| StructScopeAsync    | 8.0902 ns | 0.1201 ns | 0.1723 ns | 7.8427 ns | 8.5594 ns | 8.2656 ns | 10.01 |    0.23 |   2,871 B | 0.0086 |      72 B |          NA |

## Basis for the verdict

1. **The expectation held before comparing:** `BoxedScope` reads 0 B, while `BoxedScopeNoInline` and the
   24 B gap between `BoxedScopeAsync` and `StructScopeAsync` show the box wherever the scope escapes
2. **Escaping shapes:** disjoint intervals and a different allocation in both the non-inlined and the async
   shape; the non-inlined code shrinks from 130 B to 43 B
3. **Inlined synchronous shape:** intervals overlap and the generated code is identical, so it is recorded as
   no difference - the change is not needed for that shape, but it is not harmful either
4. **Against the Zen 3 reference:** same direction. The reference reports `UsingAsync` at 24 B → 0 B; this
   benchmark returns a fresh `Task<int>`, so its async rows carry 72 B on both sides and only the box differs.
   The delegate-carrying scope is 3.29 ns / 56 B here against 10.3 ns / 64 B there
