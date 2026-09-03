# R-04: foreach vs for (loop form selection)

- Verdict: no difference for array / Span / ReadOnlySpan (instruction sequences identical); one real exception
- **Exception: a `for` whose loop condition reads a field (`this.values.Length`) is 2.20x slower** and 67 B vs 32 B.
  It reloads the array reference every iteration, keeps a bounds check inside the loop, needs a stack frame,
  and uses indexed addressing instead of a pointer walk. Hoisting the reference into a local produces code
  identical to foreach.
- List<T>: foreach and for are the same (both reload _items and keep bounds checks); AsSpan is 0.46-0.50x
- 64-byte struct elements: all four forms share one instruction sequence — foreach emits no copy when the
  body only reads a field
- Verdicts for the ties rest on instruction-sequence identity, not on the times (this machine is noisy, ~±10%)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
## 1. Array / Span / ReadOnlySpan

| Method              | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ArrayForeach        | 268.5 ns | 25.98 ns | 37.27 ns | 226.1 ns | 346.9 ns | 317.4 ns |  1.02 |    0.19 |      32 B |         - |          NA |
| ArrayFieldFor       | 590.5 ns | 34.95 ns | 51.23 ns | 502.7 ns | 681.3 ns | 657.3 ns |  2.24 |    0.35 |      67 B |         - |          NA |
| ArrayLocalFor       | 317.7 ns | 27.71 ns | 40.62 ns | 265.2 ns | 414.2 ns | 370.9 ns |  1.20 |    0.22 |      32 B |         - |          NA |
| SpanForeach         | 335.6 ns | 37.11 ns | 55.54 ns | 262.6 ns | 445.2 ns | 405.0 ns |  1.27 |    0.27 |      54 B |         - |          NA |
| SpanFor             | 282.3 ns | 20.03 ns | 28.72 ns | 253.4 ns | 360.9 ns | 327.6 ns |  1.07 |    0.18 |      54 B |         - |          NA |
| ReadOnlySpanForeach | 261.4 ns |  4.43 ns |  6.21 ns | 248.3 ns | 272.9 ns | 267.7 ns |  0.99 |    0.13 |      54 B |         - |          NA |
| ReadOnlySpanFor     | 263.1 ns |  3.28 ns |  4.80 ns | 255.6 ns | 274.3 ns | 269.5 ns |  1.00 |    0.13 |      54 B |         - |          NA |

## 2. List<T>

| Method            | Mean     | Error    | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |---------:|---------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ListForeach       | 615.7 ns | 47.30 ns |  70.79 ns | 504.4 ns | 707.2 ns | 691.7 ns |  1.01 |    0.17 |      71 B |         - |          NA |
| ListFor           | 646.6 ns | 92.44 ns | 135.50 ns | 526.0 ns | 958.5 ns | 901.2 ns |  1.06 |    0.25 |      72 B |         - |          NA |
| ListAsSpanForeach | 279.0 ns | 19.24 ns |  27.60 ns | 252.7 ns | 367.0 ns | 302.0 ns |  0.46 |    0.07 |      72 B |         - |          NA |
| ListAsSpanFor     | 301.8 ns | 25.77 ns |  38.58 ns | 263.9 ns | 404.3 ns | 348.8 ns |  0.50 |    0.09 |      72 B |         - |          NA |

## 3. Large struct elements (64 bytes)

| Method      | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------ |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ForeachCopy | 475.5 ns | 6.22 ns |  9.31 ns | 464.3 ns | 503.9 ns | 487.9 ns |  1.00 |    0.03 |      51 B |         - |          NA |
| ForeachRef  | 474.5 ns | 6.89 ns | 10.31 ns | 460.7 ns | 494.3 ns | 489.4 ns |  1.00 |    0.03 |      51 B |         - |          NA |
| ForIndexer  | 481.6 ns | 7.99 ns | 11.71 ns | 463.7 ns | 504.9 ns | 494.5 ns |  1.01 |    0.03 |      51 B |         - |          NA |
| ForRef      | 474.3 ns | 5.62 ns |  8.41 ns | 461.4 ns | 488.5 ns | 485.7 ns |  1.00 |    0.03 |      51 B |         - |          NA |
