# TYP-08: Put the cache lookup first (CacheLookupOrder / TypeMetadataLookup)

- Verdict: **adopted** - look the cache up first, build on a miss, and pass state to a `static` lambda. Two of
  the supporting claims do not hold on .NET 11 and are corrected in the catalog
- First measurement in this repository, on the reference environment (HX 370, x86-64-v4, .NET 11.0.0 JIT)
- Hit path: 33.44 ns / 368 B → 4.15 ns / 0 B (0.12x)
- Lookup first with a capturing lambda still allocates **32 B on every hit** (5.44 ns): the display class is
  allocated at method entry and Tier1 does not remove it, so no `NoInlining` variant was needed to separate it
- Per-type caching of `GetInterfaces` (42.75 ns / 120 B → 2.99 ns / 0 B) and `GetConstructor` (36.21 ns /
  128 B → 2.99 ns / 0 B) pays
- **`Nullable.GetUnderlyingType` no longer allocates on .NET 11** (2.66 ns / 0 B; .NET 10: 29.11 ns / 32 B).
  Caching it is a gain on .NET 10 (0.12x) and a loss on .NET 11 (1.13x)
- Reusing the `GetConstructor` signature array saves 32 B (128 → 96 B) but no time (36.21 vs 36.44 ns,
  overlapping intervals): `GetConstructor` itself allocates the remaining 96 B

## CacheLookupOrderBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.24 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|-------:|----------:|------------:|
| ReflectThenLookup          | 33.442 ns | 0.2118 ns | 0.2969 ns | 33.032 ns | 34.301 ns | 33.752 ns |  1.00 |  10,137 B | 0.0440 |     368 B |        1.00 |
| LookupFirstCapturingLambda |  5.439 ns | 0.0575 ns | 0.0748 ns |  5.352 ns |  5.620 ns |  5.543 ns |  0.16 |   3,774 B | 0.0038 |      32 B |        0.09 |
| LookupFirstStaticLambda    |  4.154 ns | 0.2072 ns | 0.2837 ns |  3.855 ns |  4.778 ns |  4.502 ns |  0.12 |   2,717 B |      - |         - |        0.00 |

## TypeMetadataLookupBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.24 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|--------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| GetUnderlyingTypeEveryCall |  2.653 ns | 0.0129 ns | 0.0177 ns |  2.622 ns |  2.696 ns |  2.673 ns |  1.00 |    0.00 |      - |   2,578 B |         - |          NA |
| CachedUnderlyingType       |  3.008 ns | 0.0308 ns | 0.0431 ns |  2.962 ns |  3.136 ns |  3.062 ns |  1.13 |    0.02 |      - |   3,824 B |         - |          NA |
| GetElementTypeEveryCall    | 42.754 ns | 1.0025 ns | 1.5005 ns | 40.985 ns | 46.988 ns | 44.523 ns | 16.12 |    0.57 | 0.0143 |   5,123 B |     120 B |          NA |
| CachedElementType          |  2.994 ns | 0.0153 ns | 0.0204 ns |  2.962 ns |  3.046 ns |  3.018 ns |  1.13 |    0.01 |      - |   4,142 B |         - |          NA |
| GetConstructorEveryCall    | 36.214 ns | 0.1692 ns | 0.2258 ns | 35.763 ns | 36.751 ns | 36.498 ns | 13.65 |    0.12 | 0.0153 |  17,797 B |     128 B |          NA |
| ReusedSignatureArray       | 36.436 ns | 0.5800 ns | 0.7743 ns | 35.500 ns | 37.922 ns | 37.461 ns | 13.74 |    0.30 | 0.0114 |  17,773 B |      96 B |          NA |
| CachedConstructor          |  2.985 ns | 0.0289 ns | 0.0405 ns |  2.926 ns |  3.103 ns |  3.034 ns |  1.13 |    0.02 |      - |   3,709 B |         - |          NA |

## Nullable.GetUnderlyingType on .NET 10 and .NET 11

The `MediumRun-.NET 11.0` rows come from the class attribute; the `.NET 10.0` / `.NET 11.0` rows were added
with `--runtimes net10.0 net11.0 --job medium`.

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.39 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  .NET 11.0           : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                     | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|-------:|----------:|------------:|
| GetUnderlyingTypeEveryCall | .NET 10.0           | .NET 10.0 | 29.108 ns | 0.3845 ns | 0.5390 ns | 28.404 ns | 30.311 ns | 29.883 ns |  1.00 |   2,421 B | 0.0038 |      32 B |        1.00 |
| CachedUnderlyingType       | .NET 10.0           | .NET 10.0 |  3.536 ns | 0.0279 ns | 0.0381 ns |  3.467 ns |  3.597 ns |  3.572 ns |  0.12 |   1,065 B |      - |         - |        0.00 |
| GetUnderlyingTypeEveryCall | .NET 11.0           | .NET 11.0 |  2.656 ns | 0.0206 ns | 0.0282 ns |  2.615 ns |  2.702 ns |  2.698 ns |  0.09 |   2,514 B |      - |         - |        0.00 |
| CachedUnderlyingType       | .NET 11.0           | .NET 11.0 |  3.018 ns | 0.0227 ns | 0.0303 ns |  2.970 ns |  3.092 ns |  3.048 ns |  0.10 |   3,824 B |      - |         - |        0.00 |
| GetUnderlyingTypeEveryCall | MediumRun-.NET 11.0 | .NET 11.0 |  2.657 ns | 0.0142 ns | 0.0194 ns |  2.628 ns |  2.710 ns |  2.685 ns |  0.09 |   2,514 B |      - |         - |        0.00 |
| CachedUnderlyingType       | MediumRun-.NET 11.0 | .NET 11.0 |  3.038 ns | 0.0209 ns | 0.0286 ns |  2.990 ns |  3.114 ns |  3.071 ns |  0.10 |   3,937 B |      - |         - |        0.00 |

## Basis for the verdict

1. **Hit path:** reflection in front of the lookup is the whole cost - 368 B and 29 ns of the 33 ns. The
   intervals of the three forms are disjoint
2. **The capturing lambda:** 32 B remains on the hit path and is not removed by escape analysis, which is the
   reason the order fix alone is not enough. The static lambda with state is the only 0 B form
3. **Metadata caching:** decided per API. `GetInterfaces` and `GetConstructor` keep allocating on .NET 11 and
   caching them is 12-14x faster. `Nullable.GetUnderlyingType` changed between generations; on a multi-target
   library the cache still pays on `net10.0` and costs 0.36 ns on `net11.0`
4. **Reusing the signature array:** allocation axis only. The time difference is within the intervals; Code
   Size 17,797 vs 17,773 B differs, so it is recorded as noise rather than no difference
5. **Against the Zen 3 reference:** same direction. The hit path is 33 ns here against 129-146 ns there, and
   the 32 B of the capturing form matches the 32-40 B the first library attempt left behind
