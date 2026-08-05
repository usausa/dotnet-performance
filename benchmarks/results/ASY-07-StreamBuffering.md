# ASY-07: Full buffering vs streaming chunks (1 MB payload)

- Verdict: adopted
- Full buffer then process: 514.8 us + 2,097,484 B with Gen0/1/2 collections (LOH pressure)
- ArrayPool 16 KB chunks: 273.1 us + 72 B (0.53x, zero effective allocation, no GC)
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
