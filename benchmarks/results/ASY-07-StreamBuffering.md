# ASY-07: Full buffering vs streaming chunks (1 MB payload)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. Pooled 16 KB chunks are 0.65x of full buffering on net10 and 0.64x on net11 (219.4 vs 341.1 μs), still at 64 B against 2 MB

### StreamBufferingBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| FullBufferThenProcess | 337.1 μs | 341.1 μs | 1.01 | 2097973 B → 2097967 B | 4,724 B → 5,555 B |
| StreamingPooledChunks | 220.2 μs | 219.4 μs | 1.00 | 64 B → 64 B | 6,031 B → 7,116 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.8 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                | Job                 | Runtime   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0     | Code Size | Gen1     | Gen2     | Allocated | Alloc Ratio |
|---------------------- |-------------------- |---------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|---------:|----------:|---------:|---------:|----------:|------------:|
| FullBufferThenProcess | .NET 10.0           | .NET 10.0 | 337.1 μs | 6.85 μs | 10.26 μs | 318.3 μs | 361.8 μs | 349.9 μs |  1.00 |    0.00 | 498.0469 |   4,724 B | 497.5586 | 497.5586 | 2097973 B |       1.000 |
| StreamingPooledChunks | .NET 10.0           | .NET 10.0 | 220.2 μs | 2.48 μs |  3.72 μs | 215.7 μs | 228.4 μs | 225.2 μs |  0.65 |    0.02 |        - |   6,031 B |        - |        - |      64 B |       0.000 |
|                       |                     |           |          |         |          |          |          |          |       |         |          |           |          |          |           |             |
| FullBufferThenProcess | MediumRun-.NET 11.0 | .NET 11.0 | 341.1 μs | 6.66 μs |  9.55 μs | 325.7 μs | 362.7 μs | 352.1 μs |  1.00 |    0.00 | 499.5117 |   5,555 B | 499.0234 | 499.0234 | 2097967 B |       1.000 |
| StreamingPooledChunks | MediumRun-.NET 11.0 | .NET 11.0 | 219.4 μs | 2.16 μs |  3.09 μs | 216.0 μs | 227.4 μs | 224.2 μs |  0.64 |    0.02 |        - |   7,116 B |        - |        - |      64 B |       0.000 |

## Earlier record

- Verdict: adopted
- Full buffer then process: 395.5 us + 2,097,484 B with Gen0/1/2 collections (LOH pressure)
- ArrayPool 16 KB chunks: 224.5 us + 64 B (0.58x, zero effective allocation, no GC)
- Peak memory drops from payload size to chunk size; throughput also improves via cache locality

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0     | Code Size | Gen1     | Gen2     | Allocated | Alloc Ratio |
|---------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|---------:|----------:|---------:|---------:|----------:|------------:|
| FullBufferThenProcess | 395.5 μs | 46.37 μs | 66.51 μs | 327.0 μs | 466.5 μs | 463.4 μs |  1.03 |    0.24 | 500.0000 |   3,277 B | 499.5117 | 499.5117 | 2097484 B |       1.000 |
| StreamingPooledChunks | 224.5 μs |  4.66 μs |  6.82 μs | 217.3 μs | 243.5 μs | 231.8 μs |  0.58 |    0.10 |        - |   4,520 B |        - |        - |      64 B |       0.000 |
