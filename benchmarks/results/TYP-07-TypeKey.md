# TYP-07 follow-up: key type for a Type-keyed Dictionary (Type vs RuntimeTypeHandle)

- Verdict (Dictionary, JIT): keying the BCL Dictionary by `RuntimeTypeHandle` instead of `Type` is 0.72x on hit / 0.81x on miss (2.905 -> 2.101 ns, 2.292 -> 1.861 ns; CIs disjoint) with 1,037 -> 797 B of code, at no change in hash quality (all three of `Type.GetHashCode`, `RuntimeHelpers.GetHashCode` and `RuntimeTypeHandle.GetHashCode` return the same identity hash). A class comparer over `TypeHandle.Value` lands on the same numbers (2.098 / 1.867 ns, 593 B): the interface dispatch it adds costs what the cheaper hash saves. The B550H (x86-64-v3) provisional run agreed (0.70x / 0.77x)
- Verdict (ConcurrentDictionary, JIT): `ConcurrentDictionary<RuntimeTypeHandle,_>` is **0.85x on hit** (2.221 -> 1.884 ns; medians 2.071 -> 1.715) and **1.07x on miss** (1.465 -> 1.569 ns). The gain sits in the per-node compare, which only runs on hit. Both ConcurrentDictionary hit rows are bimodal between launches on this machine (2.03-2.89 / 1.68-2.11 ns), hence the medians; the `Dictionary` rows that moved 30% between launches on B550H are stable here (StdDev 0.03 ns)
- Confirmed on both machines (was flagged unexpected on B550H): `ConcurrentDictionary<Type,_>` **beats** `Dictionary<Type,_>` on hit (2.22 vs 2.91 ns) and on miss (1.47 vs 2.29 ns). ConcurrentDictionary's `_comparerIsDefaultForClasses` flag lets it hash with `key.GetHashCode()` directly (one GDV guard to `RuntimeType`) instead of the `comparer.GetHashCode` interface call, so only its `Equals` still goes through the comparer
- **Run 2 (lookup + dispatch): the Zen 3 flip does not reproduce on Zen 5.** With five factory delegates alternating, `ConcurrentDictionary<RuntimeTypeHandle,_>` is **0.82x** of `<Type,_>` (4.02 vs 4.89 ns), the `IntPtr` key 0.64x (3.11 ns), the `Type` key with a `TypeHandle.Value` comparer 0.88x, and the single-type handle case 0.57x. On B550H (Zen 3) the same code put the handle key at 2.24x (15.64 vs 6.98 ns). The lookup code is the same on both machines (`TypeRotating`: GDV guards on `ObjectEqualityComparer<Type>` / `RuntimeType` and a virtual `call [rax+18]` for the hash; `HandleRotating`: a direct call to the `TryGetHashCode` FCall with the `GetHashCodeWorker` fallback and a single `cmp` per node). The code sizes differ only in PGO's delegate-target guess: HX 370's `HandleRotating` and `PtrRotating` carry a one-target guard (`Target2` / `Target1`) with the `new` inlined, 759 / 480 B against 669 / 409 B on B550H, while `TypeRotating`, the comparer variant and both single-type variants have byte-identical sizes. So the Zen 3 penalty is a run-time (branch prediction / speculation) effect around that guard and the indirect delegate call, not a property of the lookup code. Hypothesis, not yet confirmed: Zen 3's predictor stops learning the period-5 target pattern once the guard, the native FCall and the indirect call are all in the history, while Zen 5's longer history does; the residual +3 ns with `DOTNET_JitEnableGuardedDevirtualization=0` on B550H fits the indirect call alone mispredicting. `DOTNET_TieredPGO=0` (no delegate guess at all), the type-count sweep in Run 4a (`TypeKeyDispatchSweepBenchmark`, 1 / 2 / 3 / 5 distinct targets in the same five-call sequence — no flip at any count on Zen 5: handle 0.77-0.88x, `IntPtr` 0.64-0.72x) and the branch-miss counters on B550H would settle it; `docs/verification-handoff.ja.md` carries that procedure. The real `ServiceRegistry.Activate` in BunnyTail.DependencyInjection agrees with the benchmark on both cores: handle key 0.83x on Zen 5 against 1.88x on Zen 3
- The hand-written table reading `TypeHandle.Value` directly stays 2.8x ahead of the best Dictionary option (0.26x / 0.30x, 195 B): it never computes the identity hash at all
- Why (DisassemblyDiagnoser): `Dictionary<Type,_>` carries three guarded-devirtualization type checks per lookup (comparer type, `GetHashCode` receiver, `Equals` receiver) and spills the key to the stack for the constrained `GetHashCode` call; `Dictionary<RuntimeTypeHandle,_>` drops the comparer guard and the spills and compares the entry with a plain `cmp`; the custom table replaces the `TryGetHashCode` call with one field load (`mov rax,[rcx+18]`)
- Why (ConcurrentDictionary): `<Type,_>` = flag test + GDV(`RuntimeType`) for the hash + `call TryGetHashCode` + hash spill + per node GDV(`ObjectEqualityComparer<Type>`) + GDV(`RuntimeType`) before the reference compare, 580 B. `<RuntimeTypeHandle,_>` = GDV(`RuntimeType`) for `type.TypeHandle` + GDV for `m_type.GetHashCode()` + `call TryGetHashCode`, then the per-node compare is a single `cmp` (`EqualityComparer<RuntimeTypeHandle>.Default.Equals` inlined to a reference compare of `m_type`), 434 B. Both still pay the identity-hash call
- **Run 3 (NativeAOT) changes the picture for the comparer route.** Every `Type`-keyed BCL path runs about 2x slower than under the JIT because the interface calls into the comparer are not devirtualized, so the `RuntimeTypeHandle` key (value-type-key path, no comparer call) gains more: `Dictionary` hit 0.50x / miss 0.62x, `ConcurrentDictionary` hit 0.61x, dispatch shape 0.79x. The `TypeHandle.Value` class comparer gains nothing there (0.94x / 1.03x on the lookup, 1.11x in the dispatch shape). The `IntPtr` key is the only option that wins on JIT, AOT and both cores (0.64-0.78x), at the price of a table that no longer references the `Type`
- So the two decisions are independent: on Dictionary / ConcurrentDictionary, change the key type (or the comparer, JIT only); the larger win needs the custom table

## Run 1: lookup alone (`TypeKeyBenchmark`, x86-64-v4, JIT, DisassemblyDiagnoser)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                         | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| DictionaryTypeHit              | 2.9053 ns | 0.0222 ns | 0.0318 ns | 2.9027 ns | 2.8597 ns | 2.9696 ns | 2.9513 ns |  1.00 |    0.02 |   1,037 B |         - |          NA |
| DictionaryHandleHit            | 2.1009 ns | 0.0097 ns | 0.0136 ns | 2.1006 ns | 2.0812 ns | 2.1379 ns | 2.1174 ns |  0.72 |    0.01 |     797 B |         - |          NA |
| DictionaryTypeComparerHit      | 2.0983 ns | 0.0211 ns | 0.0303 ns | 2.0906 ns | 2.0674 ns | 2.1884 ns | 2.1297 ns |  0.72 |    0.01 |     593 B |         - |          NA |
| CustomHandleValueHit           | 0.7598 ns | 0.0073 ns | 0.0102 ns | 0.7573 ns | 0.7432 ns | 0.7848 ns | 0.7718 ns |  0.26 |    0.00 |     195 B |         - |          NA |
| ConcurrentDictionaryTypeHit    | 2.2214 ns | 0.2016 ns | 0.2827 ns | 2.0713 ns | 2.0333 ns | 2.8921 ns | 2.7487 ns |  0.76 |    0.10 |     787 B |         - |          NA |
| ConcurrentDictionaryHandleHit  | 1.8836 ns | 0.1495 ns | 0.2047 ns | 1.7148 ns | 1.6750 ns | 2.1093 ns | 2.1068 ns |  0.65 |    0.07 |     641 B |         - |          NA |
| DictionaryTypeMiss             | 2.2918 ns | 0.0213 ns | 0.0291 ns | 2.2938 ns | 2.2416 ns | 2.3518 ns | 2.3267 ns |  0.79 |    0.01 |     771 B |         - |          NA |
| DictionaryHandleMiss           | 1.8605 ns | 0.0094 ns | 0.0129 ns | 1.8603 ns | 1.8401 ns | 1.8923 ns | 1.8753 ns |  0.64 |    0.01 |     806 B |         - |          NA |
| DictionaryTypeComparerMiss     | 1.8670 ns | 0.0121 ns | 0.0169 ns | 1.8668 ns | 1.8285 ns | 1.9080 ns | 1.8873 ns |  0.64 |    0.01 |     478 B |         - |          NA |
| CustomHandleValueMiss          | 0.6789 ns | 0.0058 ns | 0.0085 ns | 0.6754 ns | 0.6674 ns | 0.7031 ns | 0.6910 ns |  0.23 |    0.00 |     205 B |         - |          NA |
| ConcurrentDictionaryTypeMiss   | 1.4653 ns | 0.0138 ns | 0.0197 ns | 1.4591 ns | 1.4446 ns | 1.5336 ns | 1.4867 ns |  0.50 |    0.01 |     566 B |         - |          NA |
| ConcurrentDictionaryHandleMiss | 1.5693 ns | 0.0442 ns | 0.0575 ns | 1.5705 ns | 1.5049 ns | 1.6352 ns | 1.6323 ns |  0.54 |    0.02 |     669 B |         - |          NA |

## Run 2: lookup followed by a dispatch on the result (`TypeKeyDispatchBenchmark`, x86-64-v4, JIT, DisassemblyDiagnoser)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| TypeRotating         | 4.886 ns | 0.0594 ns | 0.0889 ns | 4.644 ns | 5.067 ns | 4.987 ns |  1.00 |    0.03 |     548 B | 0.0029 |      24 B |        1.00 |
| HandleRotating       | 4.016 ns | 0.0294 ns | 0.0431 ns | 3.958 ns | 4.119 ns | 4.072 ns |  0.82 |    0.02 |     759 B | 0.0029 |      24 B |        1.00 |
| PtrRotating          | 3.108 ns | 0.0493 ns | 0.0739 ns | 3.009 ns | 3.221 ns | 3.195 ns |  0.64 |    0.02 |     480 B | 0.0029 |      24 B |        1.00 |
| TypeComparerRotating | 4.315 ns | 0.0658 ns | 0.0984 ns | 4.184 ns | 4.570 ns | 4.459 ns |  0.88 |    0.03 |     470 B | 0.0029 |      24 B |        1.00 |
| TypeSingle           | 3.389 ns | 0.0305 ns | 0.0457 ns | 3.298 ns | 3.487 ns | 3.441 ns |  0.69 |    0.02 |     589 B | 0.0029 |      24 B |        1.00 |
| HandleSingle         | 2.795 ns | 0.0247 ns | 0.0361 ns | 2.717 ns | 2.870 ns | 2.832 ns |  0.57 |    0.01 |   1,168 B | 0.0029 |      24 B |        1.00 |

## Run 3a: lookup alone, JIT vs NativeAOT, same MediumRun settings (no DisassemblyDiagnoser, so no Code Size column)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]         : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 10.0      : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  NativeAOT 10.0 : .NET 10.0.12, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                         | Job            | Runtime        | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |--------------- |--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|------------:|
| DictionaryTypeHit              | .NET 10.0      | .NET 10.0      | 2.9171 ns | 0.0196 ns | 0.0282 ns | 2.8793 ns | 2.9912 ns | 2.9475 ns |  1.00 |    0.01 |         - |          NA |
| DictionaryHandleHit            | .NET 10.0      | .NET 10.0      | 2.1149 ns | 0.0067 ns | 0.0094 ns | 2.0965 ns | 2.1360 ns | 2.1236 ns |  0.73 |    0.01 |         - |          NA |
| DictionaryTypeComparerHit      | .NET 10.0      | .NET 10.0      | 1.9999 ns | 0.0253 ns | 0.0355 ns | 1.9418 ns | 2.0727 ns | 2.0475 ns |  0.69 |    0.01 |         - |          NA |
| CustomHandleValueHit           | .NET 10.0      | .NET 10.0      | 0.7452 ns | 0.0082 ns | 0.0115 ns | 0.7350 ns | 0.7820 ns | 0.7569 ns |  0.26 |    0.00 |         - |          NA |
| ConcurrentDictionaryTypeHit    | .NET 10.0      | .NET 10.0      | 2.0634 ns | 0.0170 ns | 0.0244 ns | 2.0335 ns | 2.1336 ns | 2.0904 ns |  0.71 |    0.01 |         - |          NA |
| ConcurrentDictionaryHandleHit  | .NET 10.0      | .NET 10.0      | 1.6905 ns | 0.0055 ns | 0.0080 ns | 1.6745 ns | 1.7044 ns | 1.7012 ns |  0.58 |    0.01 |         - |          NA |
| DictionaryTypeMiss             | .NET 10.0      | .NET 10.0      | 2.2930 ns | 0.0142 ns | 0.0194 ns | 2.2647 ns | 2.3511 ns | 2.3177 ns |  0.79 |    0.01 |         - |          NA |
| DictionaryHandleMiss           | .NET 10.0      | .NET 10.0      | 1.8728 ns | 0.0096 ns | 0.0140 ns | 1.8445 ns | 1.9072 ns | 1.8924 ns |  0.64 |    0.01 |         - |          NA |
| DictionaryTypeComparerMiss     | .NET 10.0      | .NET 10.0      | 1.4347 ns | 0.0235 ns | 0.0338 ns | 1.3902 ns | 1.5194 ns | 1.4771 ns |  0.49 |    0.01 |         - |          NA |
| CustomHandleValueMiss          | .NET 10.0      | .NET 10.0      | 0.6664 ns | 0.0024 ns | 0.0036 ns | 0.6589 ns | 0.6737 ns | 0.6714 ns |  0.23 |    0.00 |         - |          NA |
| ConcurrentDictionaryTypeMiss   | .NET 10.0      | .NET 10.0      | 1.4817 ns | 0.0185 ns | 0.0271 ns | 1.4451 ns | 1.5252 ns | 1.5148 ns |  0.51 |    0.01 |         - |          NA |
| ConcurrentDictionaryHandleMiss | .NET 10.0      | .NET 10.0      | 1.5180 ns | 0.0069 ns | 0.0099 ns | 1.5003 ns | 1.5467 ns | 1.5307 ns |  0.52 |    0.01 |         - |          NA |
| DictionaryTypeHit              | NativeAOT 10.0 | NativeAOT 10.0 | 6.3453 ns | 0.1035 ns | 0.1450 ns | 6.2429 ns | 6.7509 ns | 6.5549 ns |  2.18 |    0.05 |         - |          NA |
| DictionaryHandleHit            | NativeAOT 10.0 | NativeAOT 10.0 | 3.1486 ns | 0.0066 ns | 0.0097 ns | 3.1336 ns | 3.1739 ns | 3.1613 ns |  1.08 |    0.01 |         - |          NA |
| DictionaryTypeComparerHit      | NativeAOT 10.0 | NativeAOT 10.0 | 5.9664 ns | 0.0168 ns | 0.0230 ns | 5.9370 ns | 6.0420 ns | 5.9906 ns |  2.05 |    0.02 |         - |          NA |
| CustomHandleValueHit           | NativeAOT 10.0 | NativeAOT 10.0 | 1.7141 ns | 0.0042 ns | 0.0058 ns | 1.7036 ns | 1.7324 ns | 1.7198 ns |  0.59 |    0.01 |         - |          NA |
| ConcurrentDictionaryTypeHit    | NativeAOT 10.0 | NativeAOT 10.0 | 4.7269 ns | 0.0206 ns | 0.0295 ns | 4.6770 ns | 4.8233 ns | 4.7579 ns |  1.62 |    0.02 |         - |          NA |
| ConcurrentDictionaryHandleHit  | NativeAOT 10.0 | NativeAOT 10.0 | 2.8936 ns | 0.0109 ns | 0.0160 ns | 2.8756 ns | 2.9280 ns | 2.9199 ns |  0.99 |    0.01 |         - |          NA |
| DictionaryTypeMiss             | NativeAOT 10.0 | NativeAOT 10.0 | 4.7040 ns | 0.0532 ns | 0.0746 ns | 4.6108 ns | 4.8267 ns | 4.7875 ns |  1.61 |    0.03 |         - |          NA |
| DictionaryHandleMiss           | NativeAOT 10.0 | NativeAOT 10.0 | 2.9286 ns | 0.0103 ns | 0.0148 ns | 2.9026 ns | 2.9692 ns | 2.9469 ns |  1.00 |    0.01 |         - |          NA |
| DictionaryTypeComparerMiss     | NativeAOT 10.0 | NativeAOT 10.0 | 4.8224 ns | 0.0138 ns | 0.0188 ns | 4.7975 ns | 4.8764 ns | 4.8403 ns |  1.65 |    0.02 |         - |          NA |
| CustomHandleValueMiss          | NativeAOT 10.0 | NativeAOT 10.0 | 1.3776 ns | 0.0076 ns | 0.0111 ns | 1.3654 ns | 1.4050 ns | 1.3924 ns |  0.47 |    0.01 |         - |          NA |
| ConcurrentDictionaryTypeMiss   | NativeAOT 10.0 | NativeAOT 10.0 | 2.9341 ns | 0.0108 ns | 0.0161 ns | 2.9096 ns | 2.9711 ns | 2.9509 ns |  1.01 |    0.01 |         - |          NA |
| ConcurrentDictionaryHandleMiss | NativeAOT 10.0 | NativeAOT 10.0 | 2.7739 ns | 0.0119 ns | 0.0167 ns | 2.7503 ns | 2.8302 ns | 2.7948 ns |  0.95 |    0.01 |         - |          NA |

Ratio is against `DictionaryTypeHit` on .NET 10.0 for every row. Read within each runtime instead — hit rows against that runtime's `Dictionary<Type,_>` hit, miss rows against its `Dictionary<Type,_>` miss:

| Path | JIT hit | JIT miss | AOT hit | AOT miss |
|---|---:|---:|---:|---:|
| `Dictionary<Type,_>` | 2.917 ns (1.00) | 2.293 ns (1.00) | 6.345 ns (1.00) | 4.704 ns (1.00) |
| `Dictionary<RuntimeTypeHandle,_>` | 2.115 ns (0.73) | 1.873 ns (0.82) | **3.149 ns (0.50)** | **2.929 ns (0.62)** |
| `Dictionary<Type,_>` + `TypeHandle.Value` class comparer | 2.000 ns (0.69) | 1.435 ns (0.63) | 5.966 ns (**0.94**) | 4.822 ns (**1.03**) |
| `ConcurrentDictionary<Type,_>` | 2.063 ns (0.71) | 1.482 ns (0.65) | 4.727 ns (0.74) | 2.934 ns (0.62) |
| `ConcurrentDictionary<RuntimeTypeHandle,_>` | 1.691 ns (0.58; 0.82 of the row above) | 1.518 ns (0.66; 1.02) | **2.894 ns (0.46; 0.61)** | 2.774 ns (0.59; 0.95) |
| Hand-written table + `TypeHandle.Value` | 0.745 ns (0.26) | 0.666 ns (0.29) | 1.714 ns (0.27) | 1.378 ns (0.29) |

Under NativeAOT every `Type`-keyed BCL path is about 2x slower than under the JIT: the comparer's `GetHashCode` / `Equals` are interface calls that the JIT devirtualizes with PGO guards and AOT cannot, so the `Dictionary<Type,_>` lookup pays two interface dispatches per probe. The `RuntimeTypeHandle` key sidesteps both (`EqualityComparer<RuntimeTypeHandle>.Default` is an exact value-type instantiation and compiles to a field compare), which is why its gain grows from 0.73x to 0.50x. The class comparer over `TypeHandle.Value` gains nothing under AOT (0.94x / 1.03x) — it replaces the virtual hash with a cheaper one but keeps the interface dispatch to the comparer, and AOT does not inline it. `ConcurrentDictionary<Type,_>` keeps its JIT advantage over `Dictionary<Type,_>` under AOT as well (0.74x), for the same reason (no interface call on its hash side).

CIs do not overlap on any of the AOT pairs quoted above (for example `Dictionary` hit [6.24, 6.45] vs handle [3.14, 3.16] vs comparer [5.95, 5.98]).

## Run 3b: lookup + dispatch, JIT vs NativeAOT

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]         : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  .NET 10.0      : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  NativeAOT 10.0 : .NET 10.0.12, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job            | Runtime        | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------- |--------------- |--------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| TypeRotating         | .NET 10.0      | .NET 10.0      | 4.899 ns | 0.0491 ns | 0.0688 ns | 4.763 ns | 5.013 ns | 4.966 ns |  1.00 |    0.02 | 0.0029 |      24 B |        1.00 |
| HandleRotating       | .NET 10.0      | .NET 10.0      | 3.947 ns | 0.0490 ns | 0.0687 ns | 3.853 ns | 4.071 ns | 4.029 ns |  0.81 |    0.02 | 0.0029 |      24 B |        1.00 |
| PtrRotating          | .NET 10.0      | .NET 10.0      | 3.115 ns | 0.0475 ns | 0.0681 ns | 3.017 ns | 3.272 ns | 3.189 ns |  0.64 |    0.02 | 0.0029 |      24 B |        1.00 |
| TypeComparerRotating | .NET 10.0      | .NET 10.0      | 4.467 ns | 0.0482 ns | 0.0691 ns | 4.271 ns | 4.616 ns | 4.558 ns |  0.91 |    0.02 | 0.0029 |      24 B |        1.00 |
| TypeSingle           | .NET 10.0      | .NET 10.0      | 3.392 ns | 0.0651 ns | 0.0975 ns | 3.245 ns | 3.530 ns | 3.503 ns |  0.69 |    0.02 | 0.0029 |      24 B |        1.00 |
| HandleSingle         | .NET 10.0      | .NET 10.0      | 2.766 ns | 0.0357 ns | 0.0501 ns | 2.573 ns | 2.825 ns | 2.802 ns |  0.56 |    0.01 | 0.0029 |      24 B |        1.00 |
| TypeRotating         | NativeAOT 10.0 | NativeAOT 10.0 | 7.751 ns | 0.1047 ns | 0.1501 ns | 7.620 ns | 8.205 ns | 7.997 ns |  1.58 |    0.04 | 0.0029 |      24 B |        1.00 |
| HandleRotating       | NativeAOT 10.0 | NativeAOT 10.0 | 6.110 ns | 0.0702 ns | 0.1007 ns | 6.017 ns | 6.401 ns | 6.291 ns |  1.25 |    0.03 | 0.0029 |      24 B |        1.00 |
| PtrRotating          | NativeAOT 10.0 | NativeAOT 10.0 | 6.015 ns | 0.0178 ns | 0.0244 ns | 5.969 ns | 6.081 ns | 6.046 ns |  1.23 |    0.02 | 0.0029 |      24 B |        1.00 |
| TypeComparerRotating | NativeAOT 10.0 | NativeAOT 10.0 | 8.617 ns | 0.0473 ns | 0.0679 ns | 8.516 ns | 8.784 ns | 8.707 ns |  1.76 |    0.03 | 0.0029 |      24 B |        1.00 |
| TypeSingle           | NativeAOT 10.0 | NativeAOT 10.0 | 7.526 ns | 0.0340 ns | 0.0477 ns | 7.456 ns | 7.645 ns | 7.572 ns |  1.54 |    0.02 | 0.0029 |      24 B |        1.00 |
| HandleSingle         | NativeAOT 10.0 | NativeAOT 10.0 | 6.290 ns | 0.2499 ns | 0.3741 ns | 5.988 ns | 7.463 ns | 6.792 ns |  1.28 |    0.08 | 0.0029 |      24 B |        1.00 |

Ratio is against `TypeRotating` on .NET 10.0 for every row. Read within each runtime instead:

| Shape | JIT | vs JIT `TypeRotating` | NativeAOT | vs AOT `TypeRotating` |
|---|---:|---:|---:|---:|
| `Type` key, 5 types rotating | 4.899 ns | 1.00 | 7.751 ns | 1.00 |
| `RuntimeTypeHandle` key, rotating | 3.947 ns | **0.81** | 6.110 ns | **0.79** |
| `IntPtr` key (`TypeHandle.Value`), rotating | 3.115 ns | 0.64 | 6.015 ns | 0.78 |
| `Type` key + `TypeHandle.Value` comparer, rotating | 4.467 ns | 0.91 | 8.617 ns | **1.11** |
| `Type` key, one type | 3.392 ns | 0.69 | 7.526 ns | 0.97 |
| `RuntimeTypeHandle` key, one type | 2.766 ns | 0.56 | 6.290 ns | 0.81 |

The JIT rows of this run match the DisassemblyDiagnoser run above within 0.1 ns. B550H (Zen 3, JIT) for comparison: `Type` 6.98 / handle 15.64 (2.24x) / `IntPtr` 4.78 (0.69x) / comparer 7.00 (1.00x) ns rotating; `Type` 5.70 / handle 4.33 ns single.

## Run 4a (HX 370): number of distinct delegate targets (`TypeKeyDispatchSweepBenchmark`, x86-64-v4, JIT)

Five calls per invoke; `TargetCount` is how many distinct types the sequence cycles through (1 = the Single rows above, 5 = the Rotating rows). On Zen 5 the handle key wins at every count, so there is no target count at which this core flips; the B550H run of the same benchmark (see `docs/verification-handoff.ja.md`) is what locates the Zen 3 flip.

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method         | TargetCount | Mean     | Error     | StdDev    | Median   | Min      | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------------- |------------ |---------:|----------:|----------:|---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| **TypeRotating**   | **1**           | **3.609 ns** | **0.0467 ns** | **0.0684 ns** | **3.614 ns** | **3.474 ns** | **3.772 ns** | **3.700 ns** |  **1.00** |    **0.03** | **0.0029** |     **608 B** |      **24 B** |        **1.00** |
| HandleRotating | 1           | 3.176 ns | 0.0400 ns | 0.0561 ns | 3.190 ns | 3.081 ns | 3.277 ns | 3.245 ns |  0.88 |    0.02 | 0.0029 |     720 B |      24 B |        1.00 |
| PtrRotating    | 1           | 2.604 ns | 0.0436 ns | 0.0639 ns | 2.596 ns | 2.427 ns | 2.736 ns | 2.676 ns |  0.72 |    0.02 | 0.0029 |     455 B |      24 B |        1.00 |
|                |             |          |           |           |          |          |          |          |       |         |        |           |           |             |
| **TypeRotating**   | **2**           | **4.090 ns** | **0.0293 ns** | **0.0429 ns** | **4.089 ns** | **3.975 ns** | **4.145 ns** | **4.139 ns** |  **1.00** |    **0.01** | **0.0029** |     **590 B** |      **24 B** |        **1.00** |
| HandleRotating | 2           | 3.165 ns | 0.0510 ns | 0.0764 ns | 3.166 ns | 3.057 ns | 3.306 ns | 3.288 ns |  0.77 |    0.02 | 0.0029 |     720 B |      24 B |        1.00 |
| PtrRotating    | 2           | 2.682 ns | 0.0242 ns | 0.0362 ns | 2.690 ns | 2.549 ns | 2.735 ns | 2.716 ns |  0.66 |    0.01 | 0.0029 |     459 B |      24 B |        1.00 |
|                |             |          |           |           |          |          |          |          |       |         |        |           |           |             |
| **TypeRotating**   | **3**           | **4.531 ns** | **0.0519 ns** | **0.0760 ns** | **4.522 ns** | **4.359 ns** | **4.651 ns** | **4.619 ns** |  **1.00** |    **0.02** | **0.0029** |     **594 B** |      **24 B** |        **1.00** |
| HandleRotating | 3           | 3.801 ns | 0.0343 ns | 0.0513 ns | 3.800 ns | 3.681 ns | 3.884 ns | 3.858 ns |  0.84 |    0.02 | 0.0029 |     724 B |      24 B |        1.00 |
| PtrRotating    | 3           | 2.897 ns | 0.0455 ns | 0.0667 ns | 2.891 ns | 2.791 ns | 3.030 ns | 2.980 ns |  0.64 |    0.02 | 0.0029 |     459 B |      24 B |        1.00 |
|                |             |          |           |           |          |          |          |          |       |         |        |           |           |             |
| **TypeRotating**   | **5**           | **4.777 ns** | **0.0455 ns** | **0.0681 ns** | **4.770 ns** | **4.643 ns** | **4.863 ns** | **4.856 ns** |  **1.00** |    **0.02** | **0.0029** |     **587 B** |      **24 B** |        **1.00** |
| HandleRotating | 5           | 4.004 ns | 0.0614 ns | 0.0901 ns | 3.975 ns | 3.775 ns | 4.145 ns | 4.121 ns |  0.84 |    0.02 | 0.0029 |     724 B |      24 B |        1.00 |
| PtrRotating    | 5           | 3.230 ns | 0.0910 ns | 0.1246 ns | 3.154 ns | 3.075 ns | 3.380 ns | 3.371 ns |  0.68 |    0.03 | 0.0029 |     387 B |      24 B |        1.00 |

## Reproducing

The repository's `BenchmarkConfig` carries `DisassemblyDiagnoser`, which NativeAOT does not support, so the two-runtime comparison is run from `benchmarks/PerformancePatterns.AotHarness`, which links these classes and substitutes a diagnoser-free config; the linked classes' MediumRun attribute provides the JIT job and the NativeAOT job is added on the command line (`vswhere.exe` on `PATH` for the ILCompiler link step):

```
cd benchmarks/PerformancePatterns.AotHarness
dotnet run -c Release -- --filter "*TypeKeyBenchmark*" "*TypeKeyDispatchBenchmark*" --runtimes nativeaot10.0 --job medium
```
