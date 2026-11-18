# net11 re-verification: new benchmarks (A2-A6, B2, B3, C1, E1-E5, F2, F3, G2)

- Measured 2026-11-18 on the reference environment (HX 370, x86-64-v4), BenchmarkDotNet 0.16.0-preview.2,
  .NET 10.0.15 and .NET 11.0.0 JIT. Each class runs its `MediumRun` net11 attribute job plus
  `--runtimes net10.0 --job medium`; the net11-only APIs (E1, E3, E4) run on net11 alone
- The items are the rows of "Items to re-verify at net11 GA" in `docs/benchmark-methodology.md`. Items measured with
  existing classes keep their own result files: A1 `LAB-BoundsCheckHint.md`, A7 `R-04-LoopForm.md`,
  B1 `LAB-ReadOnlyCollectionLoop.md` / `LAB-EnumerableEscape.md`, D1 `ASY-01-AsyncElision.md` / `ASY-05-ValueTask.md`,
  F1 `COL-02-FrozenCondition.md`
- Bounds, overflow and covariance checks are read from `DOTNET_TieredCompilation=0` + `DOTNET_JitDisasm` (FullOpts),
  because the BDN disassembly does not name the helpers. `Code Size` in the BDN tables is the Tier1 size and includes
  the callees

## Summary

| Item | net10 | net11 | Verdict |
|---|---|---|---|
| A2 sampled hash, indexed form | 1 bounds check, 1.08x of the manual ref | 1 bounds check, 1.08x | unchanged - keep the manual ref |
| A3 `Slice(Length - n)` tail read | 68 B, 2 throw helpers | 23 B, none (ref form 21 B) | changed - the Slice form is enough |
| A3 slice-advance loop | scalar: 1 bounds check / vector: throw helper | none (130 → 85 B, 119 → 98 B) | changed - the Slice form is enough |
| A4 `LeadingZeroCount` / `PopCount` / OR-composed index | 1 bounds check each | none (75 → 58 B, 97 → 80 B), 0.88-0.91x | changed - no unsafe needed (`Log2` was already clean on net10) |
| A5 `span[i - 1]` after a uint guard | 1 bounds check | none (104 → 85 B) | changed in code; the uint guard is faster on both (0.86x / 0.76x) because `i > 0` is not fused |
| A6 `checked` after a range guard | overflow branch | none; narrowing form identical to `unchecked` | changed - `checked` is free after the guard |
| B2 `Nullable<T>` box | 5.9 ns / 24 B | 0.92 ns / 0 B (interface check and explicit cast) | changed |
| B3 `EqualityComparer<T>.Default` vs `IEquatable<T>` constraint | equal (0.80 vs 0.81 ns); new instance 24 B on both | equal; new instance 0 B on both. NativeAOT: equal (1.81 vs 1.81 ns), new instance 24 B in both | no constraint advantage on either JIT generation or on NativeAOT |
| C1 generic virtual method | 3.34 ns, 272 B; local instance 24 B | 0.80 ns, 4 B, 0 B | changed - devirtualized and inlined |
| E1 whitespace search | - | `IndexOfAnyWhiteSpace` = own `SearchValues` (19.0 ns); leading trim: scalar 2.3x faster | new API, equal to the hand-built set |
| E2 `Span.Sort` with a struct comparer | 4,014 ns / 88 B | 603 ns / 0 B | changed |
| E3 `GZipEncoder` | - | same time; 144 B against 232 B (reused `MemoryStream`) | no new pattern |
| E4 `ZipLower` / `ZipUpper` | - | identical to `Sse2.UnpackLow` / `UnpackHigh` | portable replacement is free |
| E4 `Reverse` twice vs `Shuffle` | - | 101 vs 109 B, overlapping intervals | noise |
| E5 `Unsafe.BitCast` struct ↔ `Vector128<double>` | stack spills, 4,447 ns (7.6x of fieldwise) | no spills, 562 ns (0.96x) | changed |
| F2 generic `Enum.Equals` | 5.42 ns / 48 B | 0.80 ns / 0 B | changed |
| F3 reference-array store | covariance helper on `object[]` / non-sealed element; none on sealed element / local array | same | unchanged (helper 0.72-0.80x faster) |
| G2 `span = span[..N]` before constant indices | identical to the length guard alone | identical | no difference |

## Generated code (JitDisasm, FullOpts)

| Method | net10 | net11 |
|---|---|---|
| `HashManualRef` / `HashIndexed` | 62 B / 76 B + RNGCHKFAIL | 57 B / 76 B + RNGCHKFAIL |
| `ReadTailRef` / `ReadTailSlice` | 24 B / 68 B + 2 throw helpers | 21 B / 23 B |
| `IndexScalar` / `SliceAdvanceScalar` | 137 B / 130 B + RNGCHKFAIL | 197 B + RNGCHKFAIL / 85 B |
| `IndexVector` / `SliceAdvanceVector` | 157 B / 119 B (throw helper each) | 170 B + RNGCHKFAIL / 98 B |
| `Log2Unsafe` / `Log2Indexed` | 63 B / 63 B | 63 B / 63 B |
| `LeadingZeroCountIndexed` / `PopCountIndexed` | 75 B + RNGCHKFAIL each | 58 B each |
| `OrComposedIndexed` | 97 B + RNGCHKFAIL | 80 B |
| `SignedGuard` / `UnsignedGuard` | 85 B / 104 B + RNGCHKFAIL | 85 B / 85 B |
| `CheckedMultiply` / `UncheckedMultiply` | 75 B + OVERFLOW / 51 B | 51 B / 63 B (alignment and one `movsxd`) |
| `CheckedNarrow` / `UncheckedNarrow` | 67 B + OVERFLOW / 60 B | 59 B / 59 B, identical instructions |
| `StoreObjectArray` / `StoreOpenElementArray` | 56 B + covariance helper each | 56 B + covariance helper each |
| `StoreSealedElementArray` / `StoreLocalNewArray` | 58 B / 166 B, no helper | 58 B / 166 B, no helper |
| `ParseWithLengthGuard` / `ParseWithExactSlice` | identical (43 instructions) | identical (43 instructions) |
| `Sse2UnpackInterleave` / `ZipInterleave` | - | identical (38 instructions) |

## A2 SampledHashIndexBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.59 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method    | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ManualRef | .NET 10.0           | .NET 10.0 | 79.05 ns | 1.593 ns | 2.335 ns | 76.94 ns | 85.70 ns | 81.08 ns |  1.00 |    0.00 |     161 B |         - |          NA |
| Indexed   | .NET 10.0           | .NET 10.0 | 85.49 ns | 0.783 ns | 1.148 ns | 84.28 ns | 88.52 ns | 87.20 ns |  1.08 |    0.03 |     175 B |         - |          NA |
|           |                     |           |          |          |          |          |          |          |       |         |           |           |             |
| ManualRef | MediumRun-.NET 11.0 | .NET 11.0 | 79.25 ns | 4.089 ns | 5.865 ns | 72.59 ns | 85.78 ns | 85.36 ns |  1.00 |    0.00 |     156 B |         - |          NA |
| Indexed   | MediumRun-.NET 11.0 | .NET 11.0 | 85.63 ns | 0.866 ns | 1.297 ns | 84.08 ns | 89.90 ns | 87.63 ns |  1.09 |    0.08 |     175 B |         - |          NA |

## A3 SliceTailReadBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.77 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method    | Job                 | Runtime   | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SliceTail | .NET 10.0           | .NET 10.0 | 89.11 ns | 4.766 ns | 6.986 ns | 93.16 ns | 80.17 ns | 98.05 ns | 97.11 ns |  1.00 |    0.00 |     186 B |         - |          NA |
| RefTail   | .NET 10.0           | .NET 10.0 | 86.45 ns | 5.313 ns | 7.952 ns | 86.78 ns | 74.69 ns | 95.75 ns | 95.48 ns |  0.98 |    0.12 |     142 B |         - |          NA |
|           |                     |           |          |          |          |          |          |          |          |       |         |           |           |             |
| SliceTail | MediumRun-.NET 11.0 | .NET 11.0 | 78.49 ns | 1.082 ns | 1.620 ns | 78.47 ns | 74.60 ns | 81.85 ns | 80.64 ns |  1.00 |    0.00 |     141 B |         - |          NA |
| RefTail   | MediumRun-.NET 11.0 | .NET 11.0 | 94.68 ns | 1.339 ns | 2.004 ns | 95.22 ns | 88.26 ns | 97.31 ns | 96.37 ns |  1.21 |    0.04 |     139 B |         - |          NA |

## A3 SliceAdvanceLoopBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.77 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean      | Error    | StdDev   | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| IndexScalar        | .NET 10.0           | .NET 10.0 | 197.34 ns | 0.399 ns | 0.560 ns | 197.36 ns | 196.61 ns | 198.67 ns | 198.05 ns |  1.00 |    0.00 |     123 B |         - |          NA |
| SliceAdvanceScalar | .NET 10.0           | .NET 10.0 | 197.43 ns | 0.409 ns | 0.600 ns | 197.36 ns | 196.45 ns | 198.70 ns | 198.13 ns |  1.00 |    0.00 |     130 B |         - |          NA |
| IndexVector        | .NET 10.0           | .NET 10.0 |  68.10 ns | 3.148 ns | 4.711 ns |  67.77 ns |  62.44 ns |  75.22 ns |  73.75 ns |  0.35 |    0.02 |     161 B |         - |          NA |
| SliceAdvanceVector | .NET 10.0           | .NET 10.0 |  66.40 ns | 4.835 ns | 7.087 ns |  72.25 ns |  58.66 ns |  74.65 ns |  73.79 ns |  0.34 |    0.04 |     139 B |         - |          NA |
|                    |                     |           |           |          |          |           |           |           |           |       |         |           |           |             |
| IndexScalar        | MediumRun-.NET 11.0 | .NET 11.0 | 197.36 ns | 0.414 ns | 0.620 ns | 197.48 ns | 196.22 ns | 198.28 ns | 197.99 ns |  1.00 |    0.00 |     194 B |         - |          NA |
| SliceAdvanceScalar | MediumRun-.NET 11.0 | .NET 11.0 | 199.94 ns | 0.601 ns | 0.863 ns | 199.71 ns | 198.87 ns | 202.08 ns | 201.47 ns |  1.01 |    0.01 |      85 B |         - |          NA |
| IndexVector        | MediumRun-.NET 11.0 | .NET 11.0 |  68.86 ns | 3.752 ns | 5.381 ns |  68.16 ns |  62.36 ns |  76.53 ns |  75.51 ns |  0.35 |    0.03 |     171 B |         - |          NA |
| SliceAdvanceVector | MediumRun-.NET 11.0 | .NET 11.0 |  66.11 ns | 0.178 ns | 0.261 ns |  66.12 ns |  65.71 ns |  66.62 ns |  66.45 ns |  0.33 |    0.00 |      98 B |         - |          NA |

## A4 BitDerivedIndexBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.39 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job                 | Runtime   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |-------------------- |---------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| Log2Unsafe              | .NET 10.0           | .NET 10.0 | 294.4 ns | 4.67 ns |  6.85 ns | 286.1 ns | 309.5 ns | 305.6 ns |  1.00 |    0.00 |      63 B |         - |          NA |
| Log2Indexed             | .NET 10.0           | .NET 10.0 | 280.7 ns | 0.60 ns |  0.84 ns | 279.0 ns | 282.8 ns | 281.7 ns |  0.95 |    0.02 |      63 B |         - |          NA |
| LeadingZeroCountIndexed | .NET 10.0           | .NET 10.0 | 239.4 ns | 2.83 ns |  4.15 ns | 235.0 ns | 249.1 ns | 246.3 ns |  0.81 |    0.02 |      75 B |         - |          NA |
| PopCountIndexed         | .NET 10.0           | .NET 10.0 | 238.9 ns | 1.75 ns |  2.56 ns | 235.1 ns | 244.7 ns | 242.6 ns |  0.81 |    0.02 |      75 B |         - |          NA |
| OrComposedIndexed       | .NET 10.0           | .NET 10.0 | 466.2 ns | 1.28 ns |  1.79 ns | 463.6 ns | 470.4 ns | 468.5 ns |  1.58 |    0.04 |      97 B |         - |          NA |
|                         |                     |           |          |         |          |          |          |          |       |         |           |           |             |
| Log2Unsafe              | MediumRun-.NET 11.0 | .NET 11.0 | 285.2 ns | 2.93 ns |  4.11 ns | 279.7 ns | 292.3 ns | 290.3 ns |  1.00 |    0.00 |      63 B |         - |          NA |
| Log2Indexed             | MediumRun-.NET 11.0 | .NET 11.0 | 280.7 ns | 1.78 ns |  2.43 ns | 278.2 ns | 288.1 ns | 283.0 ns |  0.98 |    0.02 |      63 B |         - |          NA |
| LeadingZeroCountIndexed | MediumRun-.NET 11.0 | .NET 11.0 | 218.5 ns | 0.58 ns |  0.81 ns | 217.1 ns | 220.4 ns | 219.5 ns |  0.77 |    0.01 |      58 B |         - |          NA |
| PopCountIndexed         | MediumRun-.NET 11.0 | .NET 11.0 | 218.3 ns | 0.63 ns |  0.86 ns | 216.8 ns | 220.2 ns | 219.3 ns |  0.77 |    0.01 |      58 B |         - |          NA |
| OrComposedIndexed       | MediumRun-.NET 11.0 | .NET 11.0 | 411.2 ns | 6.97 ns | 10.22 ns | 399.7 ns | 431.3 ns | 425.2 ns |  1.44 |    0.04 |      80 B |         - |          NA |

## A5 UnsignedGuardOffsetBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.65 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SignedGuard   | .NET 10.0           | .NET 10.0 | 405.2 ns | 10.29 ns | 15.09 ns | 386.6 ns | 434.8 ns | 423.9 ns |  1.00 |    0.00 |      89 B |         - |          NA |
| UnsignedGuard | .NET 10.0           | .NET 10.0 | 348.0 ns |  1.83 ns |  2.74 ns | 342.5 ns | 353.5 ns | 350.7 ns |  0.86 |    0.03 |     108 B |         - |          NA |
|               |                     |           |          |          |          |          |          |          |       |         |           |           |             |
| SignedGuard   | MediumRun-.NET 11.0 | .NET 11.0 | 427.4 ns |  9.80 ns | 14.37 ns | 407.3 ns | 462.5 ns | 447.5 ns |  1.00 |    0.00 |      89 B |         - |          NA |
| UnsignedGuard | MediumRun-.NET 11.0 | .NET 11.0 | 324.2 ns |  2.81 ns |  4.02 ns | 315.4 ns | 331.5 ns | 329.3 ns |  0.76 |    0.03 |      89 B |         - |          NA |

## A6 CheckedAfterGuardBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.63 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| UncheckedMultiply | .NET 10.0           | .NET 10.0 | 327.8 ns | 0.98 ns |  1.41 ns | 324.9 ns | 330.1 ns | 329.5 ns |  1.00 |    0.00 |      55 B |         - |          NA |
| CheckedMultiply   | .NET 10.0           | .NET 10.0 | 348.5 ns | 1.00 ns |  1.37 ns | 345.7 ns | 352.0 ns | 350.0 ns |  1.06 |    0.01 |      68 B |         - |          NA |
| UncheckedNarrow   | .NET 10.0           | .NET 10.0 | 303.5 ns | 2.32 ns |  3.26 ns | 295.6 ns | 309.8 ns | 307.4 ns |  0.93 |    0.01 |      62 B |         - |          NA |
| CheckedNarrow     | .NET 10.0           | .NET 10.0 | 352.5 ns | 3.38 ns |  4.84 ns | 344.1 ns | 361.0 ns | 358.5 ns |  1.08 |    0.02 |      71 B |         - |          NA |
|                   |                     |           |          |         |          |          |          |          |       |         |           |           |             |
| UncheckedMultiply | MediumRun-.NET 11.0 | .NET 11.0 | 284.3 ns | 2.76 ns |  3.95 ns | 280.1 ns | 294.2 ns | 289.6 ns |  1.00 |    0.00 |      65 B |         - |          NA |
| CheckedMultiply   | MediumRun-.NET 11.0 | .NET 11.0 | 328.4 ns | 1.49 ns |  2.19 ns | 325.6 ns | 334.4 ns | 331.0 ns |  1.16 |    0.02 |      55 B |         - |          NA |
| UncheckedNarrow   | MediumRun-.NET 11.0 | .NET 11.0 | 303.6 ns | 8.57 ns | 12.30 ns | 292.1 ns | 342.4 ns | 316.9 ns |  1.07 |    0.04 |      61 B |         - |          NA |
| CheckedNarrow     | MediumRun-.NET 11.0 | .NET 11.0 | 296.6 ns | 2.21 ns |  3.24 ns | 291.6 ns | 303.1 ns | 301.2 ns |  1.04 |    0.02 |      61 B |         - |          NA |

## B2 NullableBoxingBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.7 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                 | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| PlainInterfaceCheck    | .NET 10.0           | .NET 10.0 | 0.7967 ns | 0.0012 ns | 0.0016 ns | 0.7966 ns | 0.7938 ns | 0.8011 ns | 0.7985 ns |  1.00 |    0.00 |      - |       8 B |         - |          NA |
| NullableInterfaceCheck | .NET 10.0           | .NET 10.0 | 5.8924 ns | 0.0245 ns | 0.0359 ns | 5.8854 ns | 5.8428 ns | 5.9874 ns | 5.9434 ns |  7.40 |    0.05 | 0.0029 |   1,084 B |      24 B |          NA |
| NullableExplicitBox    | .NET 10.0           | .NET 10.0 | 5.9095 ns | 0.0304 ns | 0.0446 ns | 5.9152 ns | 5.8358 ns | 6.0008 ns | 5.9553 ns |  7.42 |    0.06 | 0.0029 |   1,082 B |      24 B |          NA |
|                        |                     |           |           |           |           |           |           |           |           |       |         |        |           |           |             |
| PlainInterfaceCheck    | MediumRun-.NET 11.0 | .NET 11.0 | 0.7962 ns | 0.0038 ns | 0.0056 ns | 0.7960 ns | 0.7895 ns | 0.8123 ns | 0.8024 ns |  1.00 |    0.00 |      - |       8 B |         - |          NA |
| NullableInterfaceCheck | MediumRun-.NET 11.0 | .NET 11.0 | 0.9185 ns | 0.0690 ns | 0.1012 ns | 1.0018 ns | 0.8105 ns | 1.0318 ns | 1.0277 ns |  1.15 |    0.13 |      - |      68 B |         - |          NA |
| NullableExplicitBox    | MediumRun-.NET 11.0 | .NET 11.0 | 0.9167 ns | 0.0667 ns | 0.0999 ns | 0.9183 ns | 0.8108 ns | 1.0275 ns | 1.0196 ns |  1.15 |    0.12 |      - |      76 B |         - |          NA |

## F2 GenericEnumEqualsBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.7 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method         | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|-------:|----------:|------------:|
| EnumEquals     | .NET 10.0           | .NET 10.0 | 5.4206 ns | 0.0427 ns | 0.0625 ns | 5.2987 ns | 5.5634 ns | 5.4980 ns |  1.00 |     227 B | 0.0057 |      48 B |        1.00 |
| ComparerEquals | .NET 10.0           | .NET 10.0 | 0.7984 ns | 0.0027 ns | 0.0038 ns | 0.7930 ns | 0.8094 ns | 0.8040 ns |  0.15 |      13 B |      - |         - |        0.00 |
|                |                     |           |           |           |           |           |           |           |       |           |        |           |             |
| EnumEquals     | MediumRun-.NET 11.0 | .NET 11.0 | 0.7980 ns | 0.0016 ns | 0.0021 ns | 0.7938 ns | 0.8013 ns | 0.8008 ns |  1.00 |      15 B |      - |         - |          NA |
| ComparerEquals | MediumRun-.NET 11.0 | .NET 11.0 | 0.7987 ns | 0.0017 ns | 0.0026 ns | 0.7941 ns | 0.8048 ns | 0.8015 ns |  1.00 |      13 B |      - |         - |          NA |

## B3 ComparerDevirtBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.71 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                     | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| ConstrainedExisting        | .NET 10.0           | .NET 10.0 | 0.8098 ns | 0.0056 ns | 0.0076 ns | 0.8008 ns | 0.8369 ns | 0.8170 ns |  1.00 |    0.00 |      30 B |      - |         - |          NA |
| DefaultComparerExisting    | .NET 10.0           | .NET 10.0 | 0.8023 ns | 0.0024 ns | 0.0035 ns | 0.7966 ns | 0.8105 ns | 0.8069 ns |  0.99 |    0.01 |      28 B |      - |         - |          NA |
| ConstrainedNewInstance     | .NET 10.0           | .NET 10.0 | 1.9874 ns | 0.0110 ns | 0.0165 ns | 1.9629 ns | 2.0314 ns | 2.0089 ns |  2.45 |    0.03 |      57 B | 0.0029 |      24 B |          NA |
| DefaultComparerNewInstance | .NET 10.0           | .NET 10.0 | 1.9963 ns | 0.0153 ns | 0.0229 ns | 1.9577 ns | 2.0429 ns | 2.0238 ns |  2.47 |    0.04 |      57 B | 0.0029 |      24 B |          NA |
|                            |                     |           |           |           |           |           |           |           |       |         |           |        |           |             |
| ConstrainedExisting        | MediumRun-.NET 11.0 | .NET 11.0 | 0.8012 ns | 0.0025 ns | 0.0036 ns | 0.7960 ns | 0.8114 ns | 0.8046 ns |  1.00 |    0.00 |      30 B |      - |         - |          NA |
| DefaultComparerExisting    | MediumRun-.NET 11.0 | .NET 11.0 | 0.8016 ns | 0.0027 ns | 0.0038 ns | 0.7954 ns | 0.8126 ns | 0.8057 ns |  1.00 |    0.01 |      28 B |      - |         - |          NA |
| ConstrainedNewInstance     | MediumRun-.NET 11.0 | .NET 11.0 | 0.8323 ns | 0.0091 ns | 0.0128 ns | 0.8144 ns | 0.8531 ns | 0.8467 ns |  1.04 |    0.02 |      68 B |      - |         - |          NA |
| DefaultComparerNewInstance | MediumRun-.NET 11.0 | .NET 11.0 | 0.8173 ns | 0.0032 ns | 0.0046 ns | 0.8095 ns | 0.8234 ns | 0.8224 ns |  1.02 |    0.01 |      68 B |      - |         - |          NA |

## B3 ComparerDevirtBenchmark, JIT and NativeAOT (AotHarness)

With BenchmarkDotNet 0.16 the NativeAOT `Ratio` is already against the NativeAOT baseline row.

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.42 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  NativeAOT 11.0      : .NET 11.0.0, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                     | Job                 | Runtime        | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-------------------- |--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| ConstrainedExisting        | MediumRun-.NET 11.0 | .NET 11.0      | 0.8023 ns | 0.0016 ns | 0.0022 ns | 0.7979 ns | 0.8071 ns | 0.8047 ns |  1.00 |    0.00 |      - |         - |          NA |
| DefaultComparerExisting    | MediumRun-.NET 11.0 | .NET 11.0      | 0.7960 ns | 0.0017 ns | 0.0025 ns | 0.7922 ns | 0.8016 ns | 0.7993 ns |  0.99 |    0.00 |      - |         - |          NA |
| ConstrainedNewInstance     | MediumRun-.NET 11.0 | .NET 11.0      | 0.8144 ns | 0.0018 ns | 0.0027 ns | 0.8107 ns | 0.8210 ns | 0.8183 ns |  1.02 |    0.00 |      - |         - |          NA |
| DefaultComparerNewInstance | MediumRun-.NET 11.0 | .NET 11.0      | 0.8129 ns | 0.0018 ns | 0.0026 ns | 0.8078 ns | 0.8168 ns | 0.8165 ns |  1.01 |    0.00 |      - |         - |          NA |
|                            |                     |                |           |           |           |           |           |           |       |         |        |           |             |
| ConstrainedExisting        | NativeAOT 11.0      | NativeAOT 11.0 | 1.8149 ns | 0.0040 ns | 0.0059 ns | 1.8068 ns | 1.8254 ns | 1.8241 ns |  1.00 |    0.00 |      - |         - |          NA |
| DefaultComparerExisting    | NativeAOT 11.0      | NativeAOT 11.0 | 1.8085 ns | 0.0059 ns | 0.0089 ns | 1.7934 ns | 1.8277 ns | 1.8193 ns |  1.00 |    0.01 |      - |         - |          NA |
| ConstrainedNewInstance     | NativeAOT 11.0      | NativeAOT 11.0 | 3.4230 ns | 0.0845 ns | 0.1185 ns | 3.3077 ns | 3.7675 ns | 3.5868 ns |  1.89 |    0.06 | 0.0029 |      24 B |          NA |
| DefaultComparerNewInstance | NativeAOT 11.0      | NativeAOT 11.0 | 3.3555 ns | 0.0135 ns | 0.0202 ns | 3.3123 ns | 3.3950 ns | 3.3832 ns |  1.85 |    0.01 | 0.0029 |      24 B |          NA |

## C1 GenericVirtualDevirtBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.64 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------------ |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| NonGenericShared        | .NET 10.0           | .NET 10.0 | 0.7984 ns | 0.0030 ns | 0.0044 ns | 0.7982 ns | 0.7921 ns | 0.8091 ns | 0.8043 ns |  1.00 |    0.00 |      - |       6 B |         - |          NA |
| GenericShared           | .NET 10.0           | .NET 10.0 | 3.3369 ns | 0.2116 ns | 0.3035 ns | 3.5931 ns | 2.9939 ns | 3.6349 ns | 3.6234 ns |  4.18 |    0.37 |      - |     272 B |         - |          NA |
| NonGenericLocalInstance | .NET 10.0           | .NET 10.0 | 0.7969 ns | 0.0030 ns | 0.0043 ns | 0.7963 ns | 0.7911 ns | 0.8076 ns | 0.8023 ns |  1.00 |    0.01 |      - |       6 B |         - |          NA |
| GenericLocalInstance    | .NET 10.0           | .NET 10.0 | 4.1500 ns | 0.0553 ns | 0.0828 ns | 4.1391 ns | 4.0260 ns | 4.3220 ns | 4.2510 ns |  5.20 |    0.11 | 0.0029 |     277 B |      24 B |          NA |
|                         |                     |           |           |           |           |           |           |           |           |       |         |        |           |           |             |
| NonGenericShared        | MediumRun-.NET 11.0 | .NET 11.0 | 0.7963 ns | 0.0030 ns | 0.0043 ns | 0.7953 ns | 0.7896 ns | 0.8063 ns | 0.8024 ns |  1.00 |    0.00 |      - |       6 B |         - |          NA |
| GenericShared           | MediumRun-.NET 11.0 | .NET 11.0 | 0.7968 ns | 0.0041 ns | 0.0059 ns | 0.7950 ns | 0.7898 ns | 0.8091 ns | 0.8064 ns |  1.00 |    0.01 |      - |       4 B |         - |          NA |
| NonGenericLocalInstance | MediumRun-.NET 11.0 | .NET 11.0 | 0.7956 ns | 0.0021 ns | 0.0030 ns | 0.7962 ns | 0.7902 ns | 0.8008 ns | 0.7991 ns |  1.00 |    0.01 |      - |       6 B |         - |          NA |
| GenericLocalInstance    | MediumRun-.NET 11.0 | .NET 11.0 | 0.7954 ns | 0.0021 ns | 0.0030 ns | 0.7960 ns | 0.7893 ns | 0.8008 ns | 0.7987 ns |  1.00 |    0.01 |      - |       4 B |         - |          NA |

## E1 WhiteSpaceSearchBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.3 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SearchValuesIndexOf     | 19.03 ns | 0.069 ns | 0.101 ns | 19.02 ns | 18.86 ns | 19.26 ns | 19.16 ns |  1.00 |    0.00 |     725 B |         - |          NA |
| ApiIndexOfAnyWhiteSpace | 18.95 ns | 0.065 ns | 0.090 ns | 18.93 ns | 18.79 ns | 19.21 ns | 19.05 ns |  1.00 |    0.01 |     725 B |         - |          NA |
| ScalarLeadingTrim       | 20.17 ns | 1.779 ns | 2.608 ns | 21.84 ns | 17.36 ns | 23.18 ns | 22.94 ns |  1.06 |    0.13 |     144 B |         - |          NA |
| SearchValuesLeadingTrim | 46.75 ns | 0.201 ns | 0.288 ns | 46.68 ns | 46.25 ns | 47.54 ns | 47.09 ns |  2.46 |    0.02 |     715 B |         - |          NA |
| ApiLeadingTrim          | 46.66 ns | 0.313 ns | 0.459 ns | 46.54 ns | 46.08 ns | 47.84 ns | 47.39 ns |  2.45 |    0.03 |     715 B |         - |          NA |

## E2 SpanSortComparerBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.46 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method             | Job                 | Runtime   | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------- |-------------------- |---------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|----------:|------------:|
| SortDefault        | .NET 10.0           | .NET 10.0 |   731.3 ns |  6.79 ns | 10.16 ns |   717.4 ns |   753.0 ns |   749.2 ns |  1.00 |    0.00 |      - |   3,845 B |         - |          NA |
| SortStructComparer | .NET 10.0           | .NET 10.0 | 4,013.7 ns | 29.46 ns | 43.19 ns | 3,946.8 ns | 4,114.0 ns | 4,060.3 ns |  5.49 |    0.09 | 0.0076 |   5,002 B |      88 B |          NA |
|                    |                     |           |            |          |          |            |            |            |       |         |        |           |           |             |
| SortDefault        | MediumRun-.NET 11.0 | .NET 11.0 |   706.4 ns |  5.25 ns |  7.70 ns |   692.6 ns |   719.2 ns |   715.0 ns |  1.00 |    0.00 |      - |   4,523 B |         - |          NA |
| SortStructComparer | MediumRun-.NET 11.0 | .NET 11.0 |   603.2 ns |  2.07 ns |  2.97 ns |   597.6 ns |   610.7 ns |   605.8 ns |  0.85 |    0.01 |      - |   4,545 B |         - |          NA |

## E3 GZipEncoderBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.3 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                    | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|-------:|----------:|------------:|
| GZipStreamToNewStream     | 868.4 μs | 3.94 μs | 5.77 μs | 859.2 μs | 885.2 μs | 875.0 μs |  1.00 |  19,284 B | 6.8359 |   58072 B |       1.000 |
| GZipStreamToReusedStream  | 862.2 μs | 1.86 μs | 2.78 μs | 858.0 μs | 869.8 μs | 865.4 μs |  0.99 |  20,173 B |      - |     232 B |       0.004 |
| GZipEncoderToPooledBuffer | 861.8 μs | 1.90 μs | 2.60 μs | 858.1 μs | 869.8 μs | 865.2 μs |  0.99 |  16,330 B |      - |     144 B |       0.002 |

## E4 VectorLaneApiBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.3 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| Ssse3ShuffleReverse     | 58.50 ns | 0.303 ns | 0.444 ns | 57.68 ns | 59.30 ns | 59.01 ns |  1.00 |     109 B |         - |          NA |
| Vector128ShuffleReverse | 58.60 ns | 0.338 ns | 0.473 ns | 57.66 ns | 59.69 ns | 59.22 ns |  1.00 |     109 B |         - |          NA |
| LaneReverseApi          | 59.04 ns | 0.175 ns | 0.257 ns | 58.52 ns | 59.51 ns | 59.32 ns |  1.01 |     101 B |         - |          NA |
| Sse2UnpackInterleave    | 21.74 ns | 0.129 ns | 0.185 ns | 21.46 ns | 22.09 ns | 21.96 ns |  0.37 |     116 B |         - |          NA |
| ZipInterleave           | 21.86 ns | 0.162 ns | 0.237 ns | 21.49 ns | 22.35 ns | 22.14 ns |  0.37 |     116 B |         - |          NA |

## E5 BitCastStructVectorBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.67 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method       | Job                 | Runtime   | Mean       | Error   | StdDev  | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------- |-------------------- |---------- |-----------:|--------:|--------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| FieldwiseAdd | .NET 10.0           | .NET 10.0 |   586.8 ns | 0.87 ns | 1.19 ns |   584.4 ns |   589.1 ns |   587.9 ns |  1.00 |    0.00 |      70 B |         - |          NA |
| BitCastAdd   | .NET 10.0           | .NET 10.0 | 4,447.2 ns | 3.31 ns | 4.85 ns | 4,443.3 ns | 4,465.3 ns | 4,453.2 ns |  7.58 |    0.02 |     105 B |         - |          NA |
|              |                     |           |            |         |         |            |            |            |       |         |           |           |             |
| FieldwiseAdd | MediumRun-.NET 11.0 | .NET 11.0 |   587.2 ns | 1.21 ns | 1.81 ns |   584.1 ns |   591.3 ns |   589.1 ns |  1.00 |    0.00 |      70 B |         - |          NA |
| BitCastAdd   | MediumRun-.NET 11.0 | .NET 11.0 |   561.6 ns | 0.66 ns | 0.97 ns |   560.0 ns |   563.3 ns |   563.0 ns |  0.96 |    0.00 |      39 B |         - |          NA |

## F3 ArrayCovarianceBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.27 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job                 | Runtime   | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |-------------------- |---------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| StoreObjectArray        | .NET 10.0           | .NET 10.0 | 139.50 ns | 1.435 ns | 2.104 ns | 137.00 ns | 146.68 ns | 141.83 ns |  1.00 |    0.00 |     304 B |         - |          NA |
| StoreOpenElementArray   | .NET 10.0           | .NET 10.0 | 129.06 ns | 4.326 ns | 6.475 ns | 121.73 ns | 136.77 ns | 135.94 ns |  0.93 |    0.05 |     304 B |         - |          NA |
| StoreSealedElementArray | .NET 10.0           | .NET 10.0 |  77.98 ns | 0.477 ns | 0.684 ns |  76.87 ns |  79.62 ns |  79.01 ns |  0.56 |    0.01 |      58 B |         - |          NA |
| StoreLocalNewArray      | .NET 10.0           | .NET 10.0 |  73.05 ns | 0.953 ns | 1.426 ns |  70.92 ns |  76.00 ns |  74.73 ns |  0.52 |    0.01 |     166 B |         - |          NA |
|                         |                     |           |           |          |          |           |           |           |       |         |           |           |             |
| StoreObjectArray        | MediumRun-.NET 11.0 | .NET 11.0 | 110.93 ns | 3.448 ns | 4.604 ns | 107.23 ns | 127.82 ns | 115.32 ns |  1.00 |    0.00 |     278 B |         - |          NA |
| StoreOpenElementArray   | MediumRun-.NET 11.0 | .NET 11.0 |  92.51 ns | 0.807 ns | 1.158 ns |  90.45 ns |  96.26 ns |  93.65 ns |  0.84 |    0.03 |     278 B |         - |          NA |
| StoreSealedElementArray | MediumRun-.NET 11.0 | .NET 11.0 |  86.52 ns | 0.312 ns | 0.437 ns |  85.72 ns |  87.58 ns |  87.09 ns |  0.78 |    0.03 |      58 B |         - |          NA |
| StoreLocalNewArray      | MediumRun-.NET 11.0 | .NET 11.0 |  73.85 ns | 0.227 ns | 0.304 ns |  73.27 ns |  74.35 ns |  74.15 ns |  0.67 |    0.02 |     166 B |         - |          NA |

## G2 FixedLengthParseBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.63 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method      | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------ |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| LengthGuard | .NET 10.0           | .NET 10.0 | 128.9 ns | 0.47 ns | 0.65 ns | 127.6 ns | 130.2 ns | 129.7 ns |  1.00 |     223 B |         - |          NA |
| ExactSlice  | .NET 10.0           | .NET 10.0 | 128.2 ns | 0.60 ns | 0.87 ns | 127.1 ns | 130.1 ns | 129.5 ns |  0.99 |     223 B |         - |          NA |
|             |                     |           |          |         |         |          |          |          |       |           |           |             |
| LengthGuard | MediumRun-.NET 11.0 | .NET 11.0 | 127.8 ns | 0.48 ns | 0.67 ns | 126.6 ns | 129.0 ns | 128.6 ns |  1.00 |     223 B |         - |          NA |
| ExactSlice  | MediumRun-.NET 11.0 | .NET 11.0 | 128.2 ns | 0.73 ns | 1.09 ns | 126.5 ns | 131.5 ns | 129.6 ns |  1.00 |     223 B |         - |          NA |

## Basis for the verdicts

1. **The rule:** generated code and allocated bytes decide; time is read only where the intervals are disjoint.
   Time differences of a few percent within one process are not attributed to the code (pitfall 10)
2. **A2:** the JIT still cannot prove `value[length >> 1]` and `value[length - 1]` from `length != 0`, so the
   manual ref in `SampledNameTable.CalculateHash` stays
3. **A3:** both Slice shapes lose every check on net11. The index-arithmetic loop gains a bounds check on net11
   (137 → 197 B) without a time change, which is a second reason not to open Slice loops into index arithmetic
4. **A5:** the time gain of the uint guard exists on both generations and comes from the branch structure: the
   JIT fuses `i >= 0 && i < length` but not `i > 0 && i < length`, so the signed form keeps two data-dependent
   branches. Only the net10 bounds check is a generation change
5. **A6:** the overflow branch disappears. The 1.15x left on the multiply form is one `movsxd` and a different loop
   alignment, not a check; the narrowing form is instruction-identical
6. **B3:** for a sealed class key `EqualityComparer<T>.Default` was already devirtualized on net10. The net11
   change is that the per-call instance is stack-allocated in both forms. NativeAOT shows the same equality without the stack allocation
7. **E1:** the BCL API and a `SearchValues` built from every `char.IsWhiteSpace` character run the same code. The
   leading-trim shape, where the first character usually decides, is 2.3x faster as a scalar loop
8. **E3:** the encoder does not remove the allocation; what removes the 58 KB is reusing the output buffer, which a
   reused `MemoryStream` does as well
9. **F3:** the covariance helper is still called for `object[]` and for arrays of a non-sealed element type held in a
   field; sealed element types and arrays allocated in the method are check-free on both generations
10. **G2:** the length guard alone already removes every constant-index check, so `span[..N]` adds nothing
