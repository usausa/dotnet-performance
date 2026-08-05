# COL-02: FrozenDictionary adoption conditions (string keys)

- Verdict: conditional - and for string keys at these sizes the condition is NOT met
- Build: 10.2x slower at 16 entries (147 -> 1,493 ns), 7.4x at 256 (2,323 -> 17,139 ns); allocation 4.25x both
- Lookup (non-interned probes): 1.05x at 16, 1.04x at 256 - CIs overlap, i.e. NO measurable lookup advantage
- With no lookup win, the 7-10x build cost never amortizes: keep Dictionary for string keys, or use a domain-specific table (COL-04 sampled hash: 0.56-0.84x)
- Consistent with R-08 (unconditional Frozen adoption rejected)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method          | Count | Mean         | Error      | StdDev     | Min          | Max          | P90          | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|---------------- |------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|-------:|----------:|----------:|------------:|
| **BuildDictionary** | **16**    |     **80.03 ns** |   **0.519 ns** |   **0.761 ns** |     **78.20 ns** |     **81.10 ns** |     **80.96 ns** |  **1.00** |    **0.01** | **0.0726** |   **7,225 B** |     **608 B** |        **1.00** |
| BuildFrozen     | 16    |    847.19 ns |  46.227 ns |  67.759 ns |    783.75 ns |  1,027.75 ns |    942.77 ns | 10.59 |    0.84 | 0.3080 |  43,408 B |    2584 B |        4.25 |
|                 |       |              |            |            |              |              |              |       |         |        |           |           |             |
| **BuildDictionary** | **256**   |  **1,281.70 ns** |   **8.566 ns** |  **12.285 ns** |  **1,239.12 ns** |  **1,297.26 ns** |  **1,293.10 ns** |  **1.00** |    **0.01** | **0.9956** |   **7,376 B** |    **8336 B** |        **1.00** |
| BuildFrozen     | 256   | 10,509.78 ns | 218.161 ns | 312.881 ns | 10,161.30 ns | 11,718.41 ns | 10,809.18 ns |  8.20 |    0.25 | 4.2267 |  38,597 B |   35416 B |        4.25 |

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Count | Mean        | Error     | StdDev    | Min         | Max         | P90         | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |------ |------------:|----------:|----------:|------------:|------------:|------------:|------:|--------:|----------:|----------:|------------:|
| **LookupDictionary** | **16**    |    **70.59 ns** |  **0.419 ns** |  **0.574 ns** |    **69.72 ns** |    **72.13 ns** |    **71.22 ns** |  **1.00** |    **0.01** |   **1,091 B** |         **-** |          **NA** |
| LookupFrozen     | 16    |    70.65 ns |  0.239 ns |  0.343 ns |    70.15 ns |    71.44 ns |    71.13 ns |  1.00 |    0.01 |   1,135 B |         - |          NA |
|                  |       |             |           |           |             |             |             |       |         |           |           |             |
| **LookupDictionary** | **256**   | **1,272.78 ns** | **15.235 ns** | **20.338 ns** | **1,251.49 ns** | **1,336.96 ns** | **1,295.33 ns** |  **1.00** |    **0.02** |   **1,131 B** |         **-** |          **NA** |
| LookupFrozen     | 256   | 1,240.91 ns |  7.421 ns | 10.158 ns | 1,226.01 ns | 1,263.15 ns | 1,256.63 ns |  0.98 |    0.02 |   1,140 B |         - |          NA |
