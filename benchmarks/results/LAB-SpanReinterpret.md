# LAB-SpanReinterpret: MemoryMarshal.Cast and its traps (study queue 7-8)

- Verdict: confirms the existing recommendation; the traps are the deliverable
- MemoryMarshal.Cast is the fastest of the three (243.7 ns), ahead of a manual Unsafe.ReadUnaligned walk
  (428.3 ns, 1.76x) and a BinaryPrimitives loop (463.2 ns, 1.90x). Same reason as R-02: the indexed form over
  the reinterpreted span keeps bounds-check elimination and auto-vectorization
- Trap 1 (asserted in Verify): casting 10 bytes to int yields length 2. The trailing 2 bytes disappear
  silently, no exception
- Trap 2 (asserted in Verify): casting int[3] to byte yields length 12. This is the direction that carries the
  int overflow guard
- Trap 3: Cast performs no alignment check. On Arm, Cast<byte, double> over an unaligned buffer can raise
  DataMisalignedException

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| MemoryMarshalCast    | 243.7 ns |  6.39 ns |  9.57 ns | 243.3 ns | 230.3 ns | 268.9 ns | 254.2 ns |  1.00 |    0.05 |      57 B |         - |          NA |
| UnsafeReadUnaligned  | 428.3 ns | 13.78 ns | 20.62 ns | 416.5 ns | 408.9 ns | 471.2 ns | 462.3 ns |  1.76 |    0.11 |      54 B |         - |          NA |
| BinaryPrimitivesRead | 463.2 ns |  9.11 ns | 13.36 ns | 457.9 ns | 448.2 ns | 493.9 ns | 484.0 ns |  1.90 |    0.09 |     103 B |         - |          NA |

