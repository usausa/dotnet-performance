# TYP-07 follow-up: key type for a Type-keyed Dictionary (Type vs RuntimeTypeHandle)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*TypeKeyBenchmark*"` and replace the table below. The codegen findings (guard counts, spills, call sites) do not depend on the machine. Also re-run under NativeAOT: TYP-07's ranking flipped there.

- Verdict: keying the BCL Dictionary by `RuntimeTypeHandle` instead of `Type` is 0.70x on hit / 0.77x on miss (CIs disjoint) with 1,037 -> 797 B of code, at no change in hash quality (all three of `Type.GetHashCode`, `RuntimeHelpers.GetHashCode` and `RuntimeTypeHandle.GetHashCode` return the same identity hash)
- Forcing TYP-07's hash source (`TypeHandle.Value`) through Dictionary's comparer slot lands in the same place on hit (0.73x) and slightly ahead on miss (0.66x) — the class comparer costs an interface dispatch per call, which eats what the cheaper hash saves
- The hand-written table reading `TypeHandle.Value` directly stays 2.4x ahead of the best Dictionary option (0.29x / 0.32x, 195 B): it never computes the identity hash at all
- Why (DisassemblyDiagnoser): `Dictionary<Type,_>` carries three guarded-devirtualization type checks per lookup (comparer type, `GetHashCode` receiver, `Equals` receiver) and spills the key to the stack for the constrained `GetHashCode` call; `Dictionary<RuntimeTypeHandle,_>` drops the comparer guard and the spills and compares the entry with a plain `cmp`; the custom table replaces the `TryGetHashCode` call with one field load (`mov rax,[rcx+18]`)
- So the two decisions are independent: on Dictionary, change the key type; the larger win needs the custom table

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| DictionaryTypeHit          | 4.576 ns | 0.0930 ns | 0.1363 ns | 4.560 ns | 4.323 ns | 4.898 ns | 4.745 ns |  1.00 |    0.04 |   1,037 B |         - |          NA |
| DictionaryHandleHit        | 3.188 ns | 0.0552 ns | 0.0826 ns | 3.210 ns | 2.981 ns | 3.352 ns | 3.274 ns |  0.70 |    0.03 |     797 B |         - |          NA |
| DictionaryTypeComparerHit  | 3.349 ns | 0.2584 ns | 0.3868 ns | 3.224 ns | 2.864 ns | 3.980 ns | 3.822 ns |  0.73 |    0.09 |     593 B |         - |          NA |
| CustomHandleValueHit       | 1.307 ns | 0.1172 ns | 0.1643 ns | 1.208 ns | 1.160 ns | 1.731 ns | 1.516 ns |  0.29 |    0.04 |     195 B |         - |          NA |
| DictionaryTypeMiss         | 3.567 ns | 0.0657 ns | 0.0984 ns | 3.600 ns | 3.377 ns | 3.694 ns | 3.664 ns |  0.78 |    0.03 |     771 B |         - |          NA |
| DictionaryHandleMiss       | 2.749 ns | 0.0552 ns | 0.0810 ns | 2.741 ns | 2.634 ns | 2.924 ns | 2.863 ns |  0.60 |    0.02 |     806 B |         - |          NA |
| DictionaryTypeComparerMiss | 2.342 ns | 0.0398 ns | 0.0596 ns | 2.341 ns | 2.254 ns | 2.441 ns | 2.438 ns |  0.51 |    0.02 |     479 B |         - |          NA |
| CustomHandleValueMiss      | 1.125 ns | 0.0235 ns | 0.0352 ns | 1.119 ns | 1.071 ns | 1.197 ns | 1.178 ns |  0.25 |    0.01 |     193 B |         - |          NA |
