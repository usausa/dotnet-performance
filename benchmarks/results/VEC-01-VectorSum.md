# VEC-01: Explicit SIMD (Vector<T> / Vector256)

- Verdict: adopted - write width-agnostic SIMD (`Vector<T>`)
- Vector\<T\>: 0.14x, Vector256: 0.22x vs scalar loop (118.3 vs 184.3 ns, non-overlapping CIs)
- `Vector<int>.Count` follows the hardware (16 lanes on AVX-512) while `Vector256<int>` is pinned at 8 lanes, so the width-agnostic form does twice the work per iteration here; on AVX2-only cores both run 8 wide and the two forms are equivalent
- Hardcode a width only when the algorithm needs specific lane semantics
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
