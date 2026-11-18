# SEQ-06: Which members a Stream subclass must override

- Verdict: **adopted**. A memory-backed stream should override the members `MemoryStream` overrides
- First measurement in this repository, on the reference environment (HX 370, x86-64-v4, .NET 11.0.0 JIT),
  on an idle machine (0-3% CPU before the run)
- Figures are per operation (`OperationsPerInvoke` = 1,000 for the byte members, 100 for the async members)
- `ReadByte`: 3.117 ns / 32 B → 0.416 ns / 0 B (0.13x)
- `WriteByte`: 1.910 ns / 32 B → 0.623 ns / 0 B (0.33x)
- `CopyTo` of 64 KB: 2,221 ns → 1,404 ns (0.63x); the 65,624 B on both sides is the destination `MemoryStream`
- 64-byte `WriteAsync`: 191.8 ns / 113 B → 2.73 ns / 1 B (0.014x)
- `FlushAsync`: 156.6 ns / 65 B → 1.05 ns / 0 B (0.007x)
- The BDN `Ratio` column is against `ReadByteDefault` for every row; the ratios above are re-derived per pair

## StreamOverrideBenchmark

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.1 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean          | Error      | StdDev     | Median        | Min           | Max           | P90           | Ratio  | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------- |--------------:|-----------:|-----------:|--------------:|--------------:|--------------:|--------------:|-------:|--------:|----------:|-------:|-------:|----------:|------------:|
| ReadByteDefault    |     3.1168 ns |  0.0544 ns |  0.0797 ns |     3.0911 ns |     3.0243 ns |     3.3296 ns |     3.2409 ns |   1.00 |    0.00 |     908 B | 0.0038 |      - |      32 B |        1.00 |
| ReadByteOverride   |     0.4163 ns |  0.0036 ns |  0.0051 ns |     0.4151 ns |     0.4096 ns |     0.4327 ns |     0.4229 ns |   0.13 |    0.00 |     104 B |      - |      - |         - |        0.00 |
| WriteByteDefault   |     1.9104 ns |  0.0289 ns |  0.0385 ns |     1.9186 ns |     1.8403 ns |     1.9892 ns |     1.9467 ns |   0.61 |    0.02 |     151 B | 0.0038 |      - |      32 B |        1.00 |
| WriteByteOverride  |     0.6228 ns |  0.1352 ns |  0.1939 ns |     0.7943 ns |     0.4150 ns |     0.8113 ns |     0.8040 ns |   0.20 |    0.06 |      98 B |      - |      - |         - |        0.00 |
| CopyToDefault      | 2,220.9730 ns | 18.5783 ns | 24.8015 ns | 2,225.0355 ns | 2,177.0893 ns | 2,274.2767 ns | 2,253.8255 ns | 713.01 |   18.99 |   8,562 B | 7.8087 | 1.3008 |   65624 B |    2,050.75 |
| CopyToOverride     | 1,403.5618 ns |  6.7941 ns |  9.5243 ns | 1,403.6551 ns | 1,390.3416 ns | 1,437.9759 ns | 1,410.0158 ns | 450.59 |   11.34 |   3,217 B | 7.8106 | 1.5602 |   65624 B |    2,050.75 |
| WriteAsyncDefault  |   191.7996 ns |  6.9393 ns | 10.1715 ns |   189.6576 ns |   178.4174 ns |   219.8359 ns |   202.9635 ns |  61.57 |    3.54 |  15,630 B | 0.0134 |      - |     113 B |        3.53 |
| WriteAsyncOverride |     2.7320 ns |  0.0206 ns |  0.0296 ns |     2.7287 ns |     2.6880 ns |     2.7939 ns |     2.7793 ns |   0.88 |    0.02 |   4,087 B | 0.0001 |      - |       1 B |        0.03 |
| FlushAsyncDefault  |   156.6246 ns |  4.1879 ns |  6.1386 ns |   155.2119 ns |   147.1779 ns |   168.9330 ns |   164.1165 ns |  50.28 |    2.29 |  10,047 B | 0.0076 |      - |      65 B |        2.03 |
| FlushAsyncOverride |     1.0458 ns |  0.0151 ns |  0.0207 ns |     1.0389 ns |     1.0209 ns |     1.1005 ns |     1.0745 ns |   0.34 |    0.01 |   3,032 B |      - |      - |         - |        0.00 |

## Basis for the verdict

1. **Time:** every pair has disjoint intervals. The async members gain the most because the default path
   queues the synchronous work to the thread pool behind a semaphore
2. **Allocation:** the default byte members allocate a one-element array per call (32 B); the default async
   members allocate the task and its state per call. The overrides allocate nothing except the occasional
   completed task (1 B per operation on `WriteAsync`)
3. **Generated code:** the default members dispatch through the base implementations (908 B for
   `ReadByteDefault`, 15,630 B for `WriteAsyncDefault`) against 104 B and 4,087 B for the overrides
4. **Stability:** `WriteByteOverride` is bimodal across its two launches (0.415 / 0.80 ns, StdDev 0.19 ns).
   Both modes are well below the default form, so the verdict does not depend on which one reproduces
5. **Against the Zen 3 reference:** same direction, absolute numbers 2-4x smaller. `CopyTo` reads 0.63x here
   on 64 KB against 0.51x there on 1 MB
6. **Benchmark fix:** the class was `sealed`, which BenchmarkDotNet rejects without running anything (exit
   code 0). It now follows the `Dispose(bool)` shape of `MemorySpanCostBenchmark`
