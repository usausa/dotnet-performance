# VEC-01: Explicit SIMD (Vector<T> / Vector256)

- Verdict: adopted
- Vector256: 0.11x, Vector<T>: 0.16x vs scalar loop
- Enumerable.Sum already 0.24x (BCL is vectorized) - prefer BCL SIMD APIs first

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| ScalarSum     | 1,111.0 ns | 45.50 ns | 63.78 ns | 1,044.3 ns | 1,291.8 ns | 1,180.3 ns |  1.00 |    0.08 |      51 B |         - |          NA |
| EnumerableSum |   267.2 ns |  5.46 ns |  7.84 ns |   257.2 ns |   289.3 ns |   278.7 ns |  0.24 |    0.01 |     857 B |         - |          NA |
| VectorTSum    |   176.4 ns |  3.89 ns |  5.46 ns |   168.6 ns |   191.1 ns |   184.4 ns |  0.16 |    0.01 |     161 B |         - |          NA |
| Vector256Sum  |   124.6 ns |  1.91 ns |  2.85 ns |   120.8 ns |   132.3 ns |   128.3 ns |  0.11 |    0.01 |     122 B |         - |          NA |
