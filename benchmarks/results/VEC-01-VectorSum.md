# VEC-01: Explicit SIMD (Vector<T> / Vector256)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - Vector<T> 0.17x and Vector256 0.22x of the scalar loop on net11 (0.15x / 0.23x on net10)
- Vector<T> reads 1.10x on net11 (125.2 → 137.5 ns, code 161 → 167 B); it is still the fastest form

### VectorSumBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ScalarSum | 819.7 ns | 817.6 ns | 1.00 | - → - | 51 B → 51 B |
| EnumerableSum | 254.9 ns | 248.9 ns | 0.98 | - → - | 608 B → 801 B |
| VectorTSum | 125.2 ns | 137.5 ns | 1.10 | - → - | 161 B → 167 B |
| Vector256Sum | 187.0 ns | 181.5 ns | 0.97 | - → - | 122 B → 118 B |

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
| Method        | Job                 | Runtime   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| ScalarSum     | .NET 10.0           | .NET 10.0 | 819.7 ns | 2.12 ns | 3.11 ns | 815.3 ns | 828.7 ns | 824.0 ns |  1.00 |      51 B |         - |          NA |
| EnumerableSum | .NET 10.0           | .NET 10.0 | 254.9 ns | 0.39 ns | 0.58 ns | 253.9 ns | 256.0 ns | 255.5 ns |  0.31 |     608 B |         - |          NA |
| VectorTSum    | .NET 10.0           | .NET 10.0 | 125.2 ns | 0.67 ns | 0.96 ns | 123.9 ns | 127.3 ns | 126.5 ns |  0.15 |     161 B |         - |          NA |
| Vector256Sum  | .NET 10.0           | .NET 10.0 | 187.0 ns | 0.25 ns | 0.37 ns | 186.2 ns | 187.7 ns | 187.3 ns |  0.23 |     122 B |         - |          NA |
|               |                     |           |          |         |         |          |          |          |       |           |           |             |
| ScalarSum     | MediumRun-.NET 11.0 | .NET 11.0 | 817.6 ns | 1.31 ns | 1.79 ns | 813.4 ns | 821.5 ns | 819.3 ns |  1.00 |      51 B |         - |          NA |
| EnumerableSum | MediumRun-.NET 11.0 | .NET 11.0 | 248.9 ns | 0.39 ns | 0.56 ns | 247.4 ns | 249.9 ns | 249.5 ns |  0.30 |     801 B |         - |          NA |
| VectorTSum    | MediumRun-.NET 11.0 | .NET 11.0 | 137.5 ns | 4.53 ns | 6.21 ns | 130.5 ns | 144.8 ns | 144.0 ns |  0.17 |     167 B |         - |          NA |
| Vector256Sum  | MediumRun-.NET 11.0 | .NET 11.0 | 181.5 ns | 3.39 ns | 5.08 ns | 175.4 ns | 187.5 ns | 186.9 ns |  0.22 |     118 B |         - |          NA |

## Earlier record

- Verdict: adopted - explicit SIMD is decisively faster than scalar; write it width-agnostic (`Vector<T>`)
- Both SIMD forms beat the scalar loop by a wide margin: Vector\<T\> 0.14x, Vector256 0.22x - the policy "use SIMD for bulk aggregation" stands regardless of CPU
- The ranking between the two forms is CPU-width dependent: `Vector<int>.Count` follows the hardware (16 lanes on AVX-512) while `Vector256<int>` is pinned at 8 lanes, so the width-agnostic form wins here (118.3 vs 184.3 ns, non-overlapping CIs); on AVX2-only cores both run 8 wide and can come out even
- Width-agnostic code is the safe default across CPUs; hardcode a width only when the algorithm needs specific lane semantics
- Enumerable.Sum is 0.31x with no code of your own (BCL is vectorized) - prefer BCL SIMD APIs first

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ScalarSum     | 826.3 ns | 17.81 ns | 24.97 ns | 809.5 ns | 903.3 ns | 856.8 ns |  1.00 |    0.04 |      51 B |         - |          NA |
| EnumerableSum | 252.3 ns |  0.29 ns |  0.42 ns | 251.4 ns | 253.2 ns | 252.8 ns |  0.31 |    0.01 |     857 B |         - |          NA |
| VectorTSum    | 118.3 ns |  0.53 ns |  0.79 ns | 116.8 ns | 119.9 ns | 119.4 ns |  0.14 |    0.00 |     161 B |         - |          NA |
| Vector256Sum  | 184.3 ns |  2.85 ns |  4.26 ns | 168.2 ns | 186.7 ns | 186.2 ns |  0.22 |    0.01 |     122 B |         - |          NA |
