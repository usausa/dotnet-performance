# ASY-07: Full buffering vs streaming chunks (1 MB payload)

- Verdict: adopted
- Full buffer then process: 514.8 us + 2,097,484 B with Gen0/1/2 collections (LOH pressure)
- ArrayPool 16 KB chunks: 273.1 us + 72 B (0.53x, zero effective allocation, no GC)
- Peak memory drops from payload size to chunk size; throughput also improves via cache locality

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Gen0     | Code Size | Gen1     | Gen2     | Allocated | Alloc Ratio |
|---------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|---------:|----------:|---------:|---------:|----------:|------------:|
| FullBufferThenProcess | 514.8 μs | 11.96 μs | 16.37 μs | 481.8 μs | 549.8 μs | 530.9 μs |  1.00 |    0.04 | 499.0234 |   3,270 B | 499.0234 | 499.0234 | 2097484 B |       1.000 |
| StreamingPooledChunks | 273.1 μs |  5.68 μs |  8.33 μs | 256.6 μs | 291.5 μs | 282.4 μs |  0.53 |    0.02 |        - |   4,532 B |        - |        - |      72 B |       0.000 |
