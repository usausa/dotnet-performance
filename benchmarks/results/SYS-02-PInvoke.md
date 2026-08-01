# SYS-02: P/Invoke 高速化

判定: 収録(SuppressGCTransition 0.57 倍でマネージド並み。LibraryImport は同速だが AOT 対応が価値)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| DllImportCall           | 2.405 ns | 0.0909 ns | 0.1303 ns | 2.253 ns | 2.606 ns | 2.562 ns |  1.00 |    0.08 |     163 B |         - |          NA |
| LibraryImportCall       | 2.307 ns | 0.0210 ns | 0.0314 ns | 2.256 ns | 2.380 ns | 2.352 ns |  0.96 |    0.05 |     163 B |         - |          NA |
| LibraryImportSuppressGC | 1.361 ns | 0.0176 ns | 0.0263 ns | 1.329 ns | 1.427 ns | 1.399 ns |  0.57 |    0.03 |      70 B |         - |          NA |
| ManagedTickCount64      | 1.242 ns | 0.0367 ns | 0.0538 ns | 1.140 ns | 1.356 ns | 1.289 ns |  0.52 |    0.04 |      63 B |         - |          NA |
