# R-04: foreach vs for (loop form selection)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: on net11 a `for` terminated by `i != length` is bounds-check free and runs like the `<` form; on net10 it
  keeps a bounds check inside the loop
- `DOTNET_JitDisasm` (FullOpts): `ArrayLocalForNotEqual` 63 B + RNGCHKFAIL and `SpanForNotEqual` 69 B + RNGCHKFAIL on
  net10; 33 B and 54 B without a check on net11 (`<`: 32 B and 54 B)
- Not instruction-identical to `<` (the array form is 17 against 18 instructions, the span form differs in register
  set-up), so the forms are equivalent rather than identical
- Times on net11 (BDN, net11 job only): 214.6 / 214.8 ns for `!=` against 220.8 / 215.5 ns for `<`. A 3% single-process
  gap is not attributed to the form (pitfall 10)

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.35 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean     | Error   | StdDev  | Min      | Max      | P90      | Code Size | Allocated |
|---------------------- |---------:|--------:|--------:|---------:|---------:|---------:|----------:|----------:|
| ArrayLocalFor         | 220.8 ns | 1.03 ns | 1.55 ns | 219.1 ns | 225.2 ns | 223.3 ns |      32 B |         - |
| SpanForeach           | 216.1 ns | 1.19 ns | 1.62 ns | 214.6 ns | 220.3 ns | 218.8 ns |      54 B |         - |
| SpanFor               | 215.5 ns | 0.50 ns | 0.75 ns | 214.3 ns | 217.4 ns | 216.4 ns |      54 B |         - |
| ArrayLocalForNotEqual | 214.6 ns | 0.65 ns | 0.91 ns | 213.1 ns | 216.6 ns | 215.9 ns |      33 B |         - |
| SpanForNotEqual       | 214.8 ns | 0.85 ns | 1.20 ns | 213.2 ns | 217.5 ns | 216.7 ns |      54 B |         - |

### All loop-form classes on both generations

- Array / Span / ReadOnlySpan forms are identical on both generations; the field-reading `for` stays 1.08x (67 B)
- `!=` termination: 1.07x / 1.09x on net10 and 0.98x / 1.00x on net11 (the A7 result above, now with net10 times)
- List: a `for` over the List stays the slowest (1.29x of foreach on net11) and AsSpan is 0.86x; struct elements: all four forms are identical (51 B)

#### LoopFormSpanBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ArrayForeach | 220.1 ns | 219.8 ns | 1.00 | - → - | 32 B → 32 B |
| ArrayFieldFor | 237.2 ns | 237.3 ns | 1.00 | - → - | 67 B → 67 B |
| ArrayLocalFor | 219.6 ns | 219.9 ns | 1.00 | - → - | 32 B → 32 B |
| SpanForeach | 217.8 ns | 215.3 ns | 0.99 | - → - | 54 B → 54 B |
| SpanFor | 217.0 ns | 215.0 ns | 0.99 | - → - | 54 B → 54 B |
| ReadOnlySpanForeach | 216.2 ns | 215.9 ns | 1.00 | - → - | 54 B → 54 B |
| ReadOnlySpanFor | 215.5 ns | 215.3 ns | 1.00 | - → - | 54 B → 54 B |
| ArrayLocalForNotEqual | 234.6 ns | 214.5 ns | 0.91 | - → - | 63 B → 33 B |
| SpanForNotEqual | 235.5 ns | 215.3 ns | 0.91 | - → - | 69 B → 54 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.98 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Mean     | Error   | StdDev  | Median   | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ArrayForeach          | .NET 10.0           | .NET 10.0 | 220.1 ns | 0.45 ns | 0.58 ns | 220.2 ns | 219.0 ns | 221.4 ns | 220.6 ns |  1.00 |      32 B |         - |          NA |
| ArrayFieldFor         | .NET 10.0           | .NET 10.0 | 237.2 ns | 0.38 ns | 0.55 ns | 237.1 ns | 236.3 ns | 238.0 ns | 237.9 ns |  1.08 |      67 B |         - |          NA |
| ArrayLocalFor         | .NET 10.0           | .NET 10.0 | 219.6 ns | 0.31 ns | 0.44 ns | 219.6 ns | 218.9 ns | 220.7 ns | 220.2 ns |  1.00 |      32 B |         - |          NA |
| SpanForeach           | .NET 10.0           | .NET 10.0 | 217.8 ns | 2.12 ns | 3.05 ns | 216.0 ns | 214.7 ns | 223.3 ns | 223.0 ns |  0.99 |      54 B |         - |          NA |
| SpanFor               | .NET 10.0           | .NET 10.0 | 217.0 ns | 1.03 ns | 1.48 ns | 216.8 ns | 214.7 ns | 220.1 ns | 219.3 ns |  0.99 |      54 B |         - |          NA |
| ReadOnlySpanForeach   | .NET 10.0           | .NET 10.0 | 216.2 ns | 0.68 ns | 0.97 ns | 216.2 ns | 214.6 ns | 218.2 ns | 217.6 ns |  0.98 |      54 B |         - |          NA |
| ReadOnlySpanFor       | .NET 10.0           | .NET 10.0 | 215.5 ns | 0.48 ns | 0.65 ns | 215.6 ns | 214.4 ns | 216.8 ns | 216.3 ns |  0.98 |      54 B |         - |          NA |
| ArrayLocalForNotEqual | .NET 10.0           | .NET 10.0 | 234.6 ns | 0.44 ns | 0.60 ns | 234.6 ns | 233.4 ns | 235.8 ns | 235.5 ns |  1.07 |      63 B |         - |          NA |
| SpanForNotEqual       | .NET 10.0           | .NET 10.0 | 235.5 ns | 0.83 ns | 1.22 ns | 235.2 ns | 234.1 ns | 238.9 ns | 236.7 ns |  1.07 |      69 B |         - |          NA |
|                       |                     |           |          |         |         |          |          |          |          |       |           |           |             |
| ArrayForeach          | MediumRun-.NET 11.0 | .NET 11.0 | 219.8 ns | 0.60 ns | 0.83 ns | 219.7 ns | 218.7 ns | 222.7 ns | 220.6 ns |  1.00 |      32 B |         - |          NA |
| ArrayFieldFor         | MediumRun-.NET 11.0 | .NET 11.0 | 237.3 ns | 0.53 ns | 0.79 ns | 237.3 ns | 235.8 ns | 238.8 ns | 238.3 ns |  1.08 |      67 B |         - |          NA |
| ArrayLocalFor         | MediumRun-.NET 11.0 | .NET 11.0 | 219.9 ns | 0.38 ns | 0.55 ns | 219.8 ns | 218.8 ns | 221.1 ns | 220.4 ns |  1.00 |      32 B |         - |          NA |
| SpanForeach           | MediumRun-.NET 11.0 | .NET 11.0 | 215.3 ns | 0.45 ns | 0.64 ns | 215.3 ns | 214.3 ns | 216.7 ns | 216.0 ns |  0.98 |      54 B |         - |          NA |
| SpanFor               | MediumRun-.NET 11.0 | .NET 11.0 | 215.0 ns | 0.33 ns | 0.47 ns | 215.1 ns | 214.2 ns | 215.8 ns | 215.6 ns |  0.98 |      54 B |         - |          NA |
| ReadOnlySpanForeach   | MediumRun-.NET 11.0 | .NET 11.0 | 215.9 ns | 1.01 ns | 1.45 ns | 215.5 ns | 214.6 ns | 221.0 ns | 217.1 ns |  0.98 |      54 B |         - |          NA |
| ReadOnlySpanFor       | MediumRun-.NET 11.0 | .NET 11.0 | 215.3 ns | 0.34 ns | 0.49 ns | 215.4 ns | 214.4 ns | 216.1 ns | 215.9 ns |  0.98 |      54 B |         - |          NA |
| ArrayLocalForNotEqual | MediumRun-.NET 11.0 | .NET 11.0 | 214.5 ns | 0.30 ns | 0.41 ns | 214.5 ns | 213.8 ns | 215.4 ns | 215.0 ns |  0.98 |      33 B |         - |          NA |
| SpanForNotEqual       | MediumRun-.NET 11.0 | .NET 11.0 | 215.3 ns | 0.47 ns | 0.66 ns | 215.2 ns | 214.2 ns | 217.0 ns | 216.1 ns |  0.98 |      54 B |         - |          NA |

#### LoopFormListBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ListForeach | 250.2 ns | 251.5 ns | 1.01 | - → - | 71 B → 71 B |
| ListFor | 316.9 ns | 325.2 ns | 1.03 | - → - | 72 B → 72 B |
| ListAsSpanForeach | 216.0 ns | 216.4 ns | 1.00 | - → - | 72 B → 72 B |
| ListAsSpanFor | 216.2 ns | 215.3 ns | 1.00 | - → - | 72 B → 72 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.87 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ListForeach       | .NET 10.0           | .NET 10.0 | 250.2 ns | 2.58 ns |  3.87 ns | 242.6 ns | 258.7 ns | 254.8 ns |  1.00 |    0.00 |      71 B |         - |          NA |
| ListFor           | .NET 10.0           | .NET 10.0 | 316.9 ns | 1.15 ns |  1.61 ns | 313.6 ns | 322.0 ns | 318.5 ns |  1.27 |    0.02 |      72 B |         - |          NA |
| ListAsSpanForeach | .NET 10.0           | .NET 10.0 | 216.0 ns | 0.62 ns |  0.93 ns | 214.5 ns | 218.0 ns | 217.2 ns |  0.86 |    0.01 |      72 B |         - |          NA |
| ListAsSpanFor     | .NET 10.0           | .NET 10.0 | 216.2 ns | 0.78 ns |  1.15 ns | 214.5 ns | 218.9 ns | 217.4 ns |  0.86 |    0.01 |      72 B |         - |          NA |
|                   |                     |           |          |         |          |          |          |          |       |         |           |           |             |
| ListForeach       | MediumRun-.NET 11.0 | .NET 11.0 | 251.5 ns | 3.00 ns |  4.30 ns | 241.9 ns | 262.6 ns | 255.5 ns |  1.00 |    0.00 |      71 B |         - |          NA |
| ListFor           | MediumRun-.NET 11.0 | .NET 11.0 | 325.2 ns | 6.86 ns | 10.06 ns | 312.2 ns | 340.9 ns | 336.7 ns |  1.29 |    0.04 |      72 B |         - |          NA |
| ListAsSpanForeach | MediumRun-.NET 11.0 | .NET 11.0 | 216.4 ns | 1.30 ns |  1.91 ns | 214.5 ns | 222.9 ns | 218.3 ns |  0.86 |    0.02 |      72 B |         - |          NA |
| ListAsSpanFor     | MediumRun-.NET 11.0 | .NET 11.0 | 215.3 ns | 0.28 ns |  0.38 ns | 214.5 ns | 215.8 ns | 215.7 ns |  0.86 |    0.01 |      72 B |         - |          NA |

#### LoopFormStructBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ForeachCopy | 290.9 ns | 289.8 ns | 1.00 | - → - | 51 B → 51 B |
| ForeachRef | 289.4 ns | 289.5 ns | 1.00 | - → - | 51 B → 51 B |
| ForIndexer | 289.9 ns | 289.7 ns | 1.00 | - → - | 51 B → 51 B |
| ForRef | 289.6 ns | 289.3 ns | 1.00 | - → - | 51 B → 51 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.88 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method      | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------ |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ForeachCopy | .NET 10.0           | .NET 10.0 | 290.9 ns | 0.86 ns | 1.20 ns | 288.6 ns | 292.7 ns | 292.4 ns |  1.00 |      51 B |         - |          NA |
| ForeachRef  | .NET 10.0           | .NET 10.0 | 289.4 ns | 0.58 ns | 0.81 ns | 287.2 ns | 290.8 ns | 290.4 ns |  1.00 |      51 B |         - |          NA |
| ForIndexer  | .NET 10.0           | .NET 10.0 | 289.9 ns | 0.98 ns | 1.46 ns | 287.8 ns | 293.7 ns | 291.7 ns |  1.00 |      51 B |         - |          NA |
| ForRef      | .NET 10.0           | .NET 10.0 | 289.6 ns | 0.64 ns | 0.90 ns | 287.1 ns | 292.2 ns | 290.4 ns |  1.00 |      51 B |         - |          NA |
|             |                     |           |          |         |         |          |          |          |       |           |           |             |
| ForeachCopy | MediumRun-.NET 11.0 | .NET 11.0 | 289.8 ns | 0.71 ns | 1.02 ns | 287.0 ns | 291.9 ns | 290.9 ns |  1.00 |      51 B |         - |          NA |
| ForeachRef  | MediumRun-.NET 11.0 | .NET 11.0 | 289.5 ns | 0.89 ns | 1.31 ns | 285.4 ns | 291.9 ns | 290.9 ns |  1.00 |      51 B |         - |          NA |
| ForIndexer  | MediumRun-.NET 11.0 | .NET 11.0 | 289.7 ns | 0.59 ns | 0.87 ns | 288.0 ns | 291.5 ns | 290.7 ns |  1.00 |      51 B |         - |          NA |
| ForRef      | MediumRun-.NET 11.0 | .NET 11.0 | 289.3 ns | 0.65 ns | 0.92 ns | 287.5 ns | 290.9 ns | 290.3 ns |  1.00 |      51 B |         - |          NA |

## Earlier record

- Verdict: no difference for array / Span / ReadOnlySpan (instruction sequences identical, and the times match
  to within 0.2 ns); two real exceptions
- **Exception 1: a `for` whose loop condition reads a field (`this.values.Length`) is 1.13x slower** and 67 B vs 32 B.
  It reloads the array reference every iteration, keeps a bounds check inside the loop, needs a stack frame,
  and uses indexed addressing instead of a pointer walk. Hoisting the reference into a local produces code
  identical to foreach.
- **Exception 2: the `List<T>` indexed `for` is 1.29x slower than foreach** - it carries two bounds checks per
  step (Count, then the array) where the enumerator form carries one. AsSpan is 0.85x for either form
- 64-byte struct elements: all four forms share one instruction sequence - foreach emits no copy when the
  body only reads a field
- The four Span / ReadOnlySpan forms are identical to each other (54 B, one instruction sequence) and sit
  ~7 ns (3%) above the array forms at 1024 elements
- The earlier run of this benchmark (Ryzen 9 5900X, x86-64-v3) put ArrayFieldFor at 2.20x and ListFor within
  noise. Every code size is unchanged between the two machines, so the *size* of these penalties is
  core-dependent while their existence is not - judge the ties on instruction-sequence identity, not on time

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9278/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
## 1. Array / Span / ReadOnlySpan

| Method              | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|-------------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ArrayForeach        | 212.4 ns | 0.30 ns | 0.43 ns | 211.8 ns | 213.4 ns | 212.8 ns |  1.00 |      32 B |         - |          NA |
| ArrayFieldFor       | 239.1 ns | 0.98 ns | 1.47 ns | 237.1 ns | 242.1 ns | 241.2 ns |  1.13 |      67 B |         - |          NA |
| ArrayLocalFor       | 212.6 ns | 0.43 ns | 0.63 ns | 211.4 ns | 213.9 ns | 213.4 ns |  1.00 |      32 B |         - |          NA |
| SpanForeach         | 219.6 ns | 0.35 ns | 0.50 ns | 218.8 ns | 220.8 ns | 220.4 ns |  1.03 |      54 B |         - |          NA |
| SpanFor             | 219.4 ns | 0.30 ns | 0.43 ns | 218.5 ns | 220.3 ns | 219.8 ns |  1.03 |      54 B |         - |          NA |
| ReadOnlySpanForeach | 219.8 ns | 0.36 ns | 0.52 ns | 218.8 ns | 221.0 ns | 220.4 ns |  1.04 |      54 B |         - |          NA |
| ReadOnlySpanFor     | 219.9 ns | 0.48 ns | 0.72 ns | 218.9 ns | 221.5 ns | 220.9 ns |  1.04 |      54 B |         - |          NA |

## 2. List<T>

| Method            | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ListForeach       | 254.8 ns | 3.03 ns | 4.54 ns | 248.7 ns | 265.7 ns | 261.0 ns |  1.00 |    0.02 |      71 B |         - |          NA |
| ListFor           | 328.8 ns | 1.98 ns | 2.97 ns | 323.2 ns | 334.1 ns | 332.4 ns |  1.29 |    0.03 |      72 B |         - |          NA |
| ListAsSpanForeach | 216.3 ns | 0.48 ns | 0.71 ns | 215.2 ns | 217.7 ns | 217.3 ns |  0.85 |    0.01 |      72 B |         - |          NA |
| ListAsSpanFor     | 216.2 ns | 0.54 ns | 0.80 ns | 215.0 ns | 218.0 ns | 217.3 ns |  0.85 |    0.02 |      72 B |         - |          NA |

## 3. Large struct elements (64 bytes)

| Method      | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------ |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ForeachCopy | 295.7 ns | 0.65 ns | 0.94 ns | 293.4 ns | 297.5 ns | 296.9 ns |  1.00 |      51 B |         - |          NA |
| ForeachRef  | 296.3 ns | 0.87 ns | 1.22 ns | 293.4 ns | 299.4 ns | 297.9 ns |  1.00 |      51 B |         - |          NA |
| ForIndexer  | 296.7 ns | 0.60 ns | 0.88 ns | 295.1 ns | 298.5 ns | 297.5 ns |  1.00 |      51 B |         - |          NA |
| ForRef      | 294.5 ns | 0.57 ns | 0.82 ns | 292.7 ns | 296.5 ns | 295.3 ns |  1.00 |      51 B |         - |          NA |
