# R-04: foreach vs for (loop form selection)

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
