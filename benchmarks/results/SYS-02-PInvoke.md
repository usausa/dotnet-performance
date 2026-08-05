# SYS-02: P/Invoke (LibraryImport + SuppressGCTransition)

- Verdict: retired from the pattern catalog (rejected-patterns R-19) - LibraryImport is the standard declaration rather than an optimization; SuppressGCTransition shows no measurable win
- LibraryImport: same speed as DllImport for a blittable call (1.13 vs 1.14 ns); its value is source-generated, AOT-safe marshalling
- SuppressGCTransition measures 1.26x (slower) here: the plain transition already costs only ~0.06 ns over an equivalent managed call, so there is nothing left for the attribute to skip. On environments where the GC transition is expensive it can still pay - measure, do not assume
- Code size halves with the attribute (70 vs 163 B)
- SuppressGCTransition constraints: sub-microsecond, non-blocking, no callbacks, no exceptions

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
