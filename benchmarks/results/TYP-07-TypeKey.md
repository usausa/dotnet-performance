# TYP-07 follow-up: key type for a Type-keyed Dictionary (Type vs RuntimeTypeHandle)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.12). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*TypeKeyBenchmark*"` and replace the tables below. The codegen findings (guard counts, spills, call sites) do not depend on the machine. Also re-run under NativeAOT: TYP-07's ranking flipped there.
- Verdict (Dictionary): keying the BCL Dictionary by `RuntimeTypeHandle` instead of `Type` is 0.70x on hit / 0.77x on miss in the first run (4.576 -> 3.188 ns, 3.567 -> 2.749 ns; CIs disjoint) with 1,037 -> 797 B of code, at no change in hash quality (all three of `Type.GetHashCode`, `RuntimeHelpers.GetHashCode` and `RuntimeTypeHandle.GetHashCode` return the same identity hash)
- Verdict (ConcurrentDictionary, second run): `ConcurrentDictionary<RuntimeTypeHandle,_>` is **0.80x on hit** (3.156 -> 2.531 ns, StdDev <= 0.06) and **no better on miss** (2.050 vs 2.565 ns with StdDev 0.67, min 1.960 -- the handle run was noisy, treat as equal). The gain sits in the per-node compare, which only runs on hit
- ⏳❗ **Run 3 reverses it for the registry shape:** when the looked-up value is a factory delegate that is invoked and several types alternate (`TypeKeyDispatchBenchmark`, the shape of `ServiceRegistry.Activate`), `ConcurrentDictionary<RuntimeTypeHandle,_>` is **2.24x slower** than `<Type,_>` (15.64 vs 6.98 ns) although it wins 0.62x when a single type repeats. An `IntPtr` key over `TypeHandle.Value` is 0.69x with no penalty and a `Type` key with a `TypeHandle.Value` comparer is neutral. Details in Run 3 below
- ⏳❗ Unexpected: on this machine `ConcurrentDictionary<Type,_>` **beats** `Dictionary<Type,_>` on hit (3.156 vs 4.6-5.5 ns). The disassembly explains it -- ConcurrentDictionary's `_comparerIsDefaultForClasses` flag lets it hash with `key.GetHashCode()` directly (one GDV guard to `RuntimeType`) instead of the `comparer.GetHashCode` interface call, so only its `Equals` still goes through the comparer. Confirm on HX 370 before quoting it
- Forcing TYP-07's hash source (`TypeHandle.Value`) through Dictionary's comparer slot lands in the same place on hit (0.73x) and slightly ahead on miss (0.66x) -- the class comparer costs an interface dispatch per call, which eats what the cheaper hash saves
- The hand-written table reading `TypeHandle.Value` directly stays 2.4x ahead of the best Dictionary option (0.29x / 0.32x, 195 B): it never computes the identity hash at all
- Why (DisassemblyDiagnoser): `Dictionary<Type,_>` carries three guarded-devirtualization type checks per lookup (comparer type, `GetHashCode` receiver, `Equals` receiver) and spills the key to the stack for the constrained `GetHashCode` call; `Dictionary<RuntimeTypeHandle,_>` drops the comparer guard and the spills and compares the entry with a plain `cmp`; the custom table replaces the `TryGetHashCode` call with one field load (`mov rax,[rcx+18]`)
- Why (ConcurrentDictionary): `<Type,_>` = flag test + GDV(`RuntimeType`) for the hash + `call TryGetHashCode` + hash spill `[rsp+24]` + per node GDV(`ObjectEqualityComparer<Type>`) + GDV(`RuntimeType`) before the reference compare, 580 B. `<RuntimeTypeHandle,_>` = GDV(`RuntimeType`) for `type.TypeHandle` + GDV for `m_type.GetHashCode()` + `call TryGetHashCode`, then the per-node compare is a single `cmp r13,rdx` (`EqualityComparer<RuntimeTypeHandle>.Default.Equals` inlined to a reference compare of `m_type`), 434 B. Both still pay the identity-hash call
- So the two decisions are independent: on Dictionary / ConcurrentDictionary, change the key type; the larger win needs the custom table
- Run-to-run variance on this machine: the `Dictionary*Hit` rows moved 30% between launches (4.576 / 6.487 / 5.483 ns for the baseline across three runs, StdDev up to 0.68 ns in the second run below) while the `ConcurrentDictionary*` and `Custom*` rows stayed within 0.06 ns. Read the Dictionary hit ratios as +-0.15 until HX 370 confirms them

## Run 1 (Dictionary variants only)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
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

## Run 2 (all variants, including ConcurrentDictionary)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                         | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| DictionaryTypeHit              | 5.483 ns | 0.4612 ns | 0.6760 ns | 4.701 ns | 6.637 ns | 6.444 ns |  1.01 |    0.17 |   1,037 B |         - |          NA |
| DictionaryHandleHit            | 4.268 ns | 0.4010 ns | 0.6002 ns | 3.335 ns | 5.283 ns | 5.002 ns |  0.79 |    0.14 |     797 B |         - |          NA |
| DictionaryTypeComparerHit      | 3.347 ns | 0.3986 ns | 0.5717 ns | 2.969 ns | 4.900 ns | 4.360 ns |  0.62 |    0.13 |     593 B |         - |          NA |
| CustomHandleValueHit           | 1.328 ns | 0.1213 ns | 0.1816 ns | 1.147 ns | 1.752 ns | 1.573 ns |  0.25 |    0.04 |     195 B |         - |          NA |
| ConcurrentDictionaryTypeHit    | 3.156 ns | 0.0429 ns | 0.0588 ns | 3.046 ns | 3.249 ns | 3.229 ns |  0.58 |    0.07 |     787 B |         - |          NA |
| ConcurrentDictionaryHandleHit  | 2.531 ns | 0.0434 ns | 0.0623 ns | 2.434 ns | 2.659 ns | 2.630 ns |  0.47 |    0.06 |     641 B |         - |          NA |
| DictionaryTypeMiss             | 3.804 ns | 0.1337 ns | 0.1917 ns | 3.459 ns | 4.324 ns | 3.998 ns |  0.70 |    0.09 |     771 B |         - |          NA |
| DictionaryHandleMiss           | 2.653 ns | 0.0583 ns | 0.0837 ns | 2.510 ns | 2.895 ns | 2.746 ns |  0.49 |    0.06 |     806 B |         - |          NA |
| DictionaryTypeComparerMiss     | 2.307 ns | 0.0286 ns | 0.0410 ns | 2.249 ns | 2.399 ns | 2.354 ns |  0.43 |    0.05 |     479 B |         - |          NA |
| CustomHandleValueMiss          | 1.179 ns | 0.0577 ns | 0.0863 ns | 1.088 ns | 1.389 ns | 1.289 ns |  0.22 |    0.03 |     205 B |         - |          NA |
| ConcurrentDictionaryTypeMiss   | 2.050 ns | 0.0586 ns | 0.0860 ns | 1.931 ns | 2.265 ns | 2.171 ns |  0.38 |    0.05 |     566 B |         - |          NA |
| ConcurrentDictionaryHandleMiss | 2.565 ns | 0.4582 ns | 0.6716 ns | 1.960 ns | 3.962 ns | 3.863 ns |  0.47 |    0.13 |     674 B |         - |          NA |

## Run 3: lookup followed by a dispatch on the result (`TypeKeyDispatchBenchmark`)

> ⏳❗ **Unexpected, provisional (B550H / Zen 3).** Found while verifying `ServiceRegistry.Activate` in BunnyTail.DependencyInjection: the `RuntimeTypeHandle` key that wins the lookup-only benchmark above **loses 2.2x** once the looked-up value is a factory delegate that gets invoked and several types alternate. Confirm on HX 370 (Zen 5) before the catalog states anything general.

- Shape: `ConcurrentDictionary<K, Func<object>>`, 5 target types; Rotating = the 5 types in sequence (5 delegate targets), Single = one type 5 times
- `RuntimeTypeHandle` key: Single 0.62x (4.33 vs 5.70 ns, the lookup gain), **Rotating 2.24x (15.64 vs 6.98 ns)**, StdDev 1.2 ns and bimodal between launches
- `IntPtr` key (`TypeHandle.Value`, arithmetic hash, no identity hash at all): Rotating **0.69x** (4.78 ns) with no penalty; note the table then holds no reference to the `Type` (collectible AssemblyLoadContext caveat)
- `Type` key + class comparer over `TypeHandle.Value >> 3`: Rotating 1.00x (7.00 ns), no penalty; in the real `ServiceRegistry.Activate` the same comparer measured 0.87x rotating / 0.85x single
- Isolation (throwaway probes, same process): hash acquisition alone is 1.2-1.5 ns on every path (no `GetHashCodeWorker` QCall involved); the lookup alone in rotation is 0.72x for the handle key; only lookup + delegate invoke + `new` flips
- Disassembly: `TypeRotating` hashes through a true virtual call to `RuntimeType.GetHashCode` (`call [rax+18]`, not GDV'd here) and compares with two GDV guards; `HandleRotating` calls the native `TryGetHashCode` FCall directly (`call 00007FF..`, `GetHashCodeWorker` fallback) and compares with one `cmp`; `PtrRotating` has no hash call. The slow variant has the shortest lookup code, so the penalty is a run-time effect (branch prediction / speculation around the delegate call whose target follows the key), not instruction count. With `DOTNET_JitEnableGuardedDevirtualization=0` the penalty shrinks to +3 ns but does not vanish
- End-to-end in BunnyTail.DependencyInjection (`ActivateBenchmark`, same machine): current `ConcurrentDictionary<Type,_>` 9.79 ns rotating / 7.88 single; `RuntimeTypeHandle` key 18.39 / 6.86; `Type` + `TypeHandle.Value >> 4` comparer 7.82 / 6.71; the fixed table used by `ResolveType` (`FixedTypeServiceTable`, TYP-07) 7.21 for the same construction cost
- Consequence for the guidance: on `ConcurrentDictionary`, prefer a key or comparer that avoids the identity hash (`TypeHandle.Value`) over `RuntimeTypeHandle` when the lookup feeds a polymorphic call; when the result is a plain value (the table above), `RuntimeTypeHandle` still measured 0.80x

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| TypeRotating         |  6.979 ns | 0.1890 ns | 0.2770 ns |  6.603 ns |  7.524 ns |  7.322 ns |  1.00 |    0.05 |     548 B | 0.0014 |      24 B |        1.00 |
| HandleRotating       | 15.641 ns | 0.8093 ns | 1.2113 ns | 13.797 ns | 17.829 ns | 17.320 ns |  2.24 |    0.19 |     669 B | 0.0014 |      24 B |        1.00 |
| PtrRotating          |  4.775 ns | 0.1071 ns | 0.1603 ns |  4.455 ns |  5.144 ns |  4.988 ns |  0.69 |    0.03 |     409 B | 0.0014 |      24 B |        1.00 |
| TypeComparerRotating |  7.000 ns | 0.1880 ns | 0.2756 ns |  6.558 ns |  7.612 ns |  7.348 ns |  1.00 |    0.05 |     470 B | 0.0014 |      24 B |        1.00 |
| TypeSingle           |  5.702 ns | 0.1379 ns | 0.2063 ns |  5.420 ns |  6.357 ns |  5.936 ns |  0.82 |    0.04 |     589 B | 0.0014 |      24 B |        1.00 |
| HandleSingle         |  4.333 ns | 0.0913 ns | 0.1280 ns |  3.975 ns |  4.641 ns |  4.470 ns |  0.62 |    0.03 |   1,168 B | 0.0014 |      24 B |        1.00 |
