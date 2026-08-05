# SYS-02: P/Invoke (LibraryImport + SuppressGCTransition)

- Verdict: LibraryImport adopted (for AOT, not speed); **SuppressGCTransition REVERSED and is rejected on this hardware**
- LibraryImport: same speed as DllImport for a blittable call (0.99x); value is AOT/trimming-safe marshalling
- **SuppressGCTransition is now 1.26x, i.e. SLOWER** (1.433 vs 1.139 ns, non-overlapping CIs). It measured 0.57x on the previous Ryzen 9 5900X baseline
- The reason the win vanished: the plain P/Invoke transition itself became almost free here - DllImport 1.139 ns vs an equivalent managed call at 1.078 ns (0.95x, was 0.52x). With nothing left to skip, the attribute only adds its own cost
- Code size still drops (70 B vs 163 B), so the attribute is not useless - but measure before applying it, do not assume the speedup
- SuppressGCTransition constraints are unchanged: sub-microsecond, non-blocking, no callbacks, no exceptions

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| DllImportCall           | 1.139 ns | 0.0180 ns | 0.0263 ns | 1.097 ns | 1.194 ns | 1.172 ns |  1.00 |    0.03 |     163 B |         - |          NA |
| LibraryImportCall       | 1.132 ns | 0.0149 ns | 0.0214 ns | 1.098 ns | 1.186 ns | 1.165 ns |  0.99 |    0.03 |     163 B |         - |          NA |
| LibraryImportSuppressGC | 1.433 ns | 0.0265 ns | 0.0380 ns | 1.352 ns | 1.538 ns | 1.449 ns |  1.26 |    0.04 |      70 B |         - |          NA |
| ManagedTickCount64      | 1.078 ns | 0.0020 ns | 0.0028 ns | 1.072 ns | 1.083 ns | 1.082 ns |  0.95 |    0.02 |      63 B |         - |          NA |
