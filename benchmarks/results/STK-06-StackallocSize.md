# STK-06: Constant-size stackalloc

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - constant size + SkipLocalsInit is 1.41 ns on both; the variable 512 B zero-initialized form got 0.87x on net11 (8.01 → 6.94 ns) but stays the slowest

### StackallocSizeBenchmark

net10 → net11 per row:

| Method | Size | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|---|
| ConstantSize | 64 | 2.244 ns | 2.228 ns | 0.99 | - → - | 172 B → 178 B |
| VariableSize | 64 | 1.815 ns | 1.742 ns | 0.96 | - → - | 131 B → 136 B |
| ConstantSizeSkipInit | 64 | 1.413 ns | 1.415 ns | 1.00 | - → - | 113 B → 119 B |
| VariableSizeSkipInit | 64 | 2.807 ns | 2.796 ns | 1.00 | - → - | 156 B → 161 B |
| ConstantSize | 512 | 2.218 ns | 2.230 ns | 1.01 | - → - | 172 B → 178 B |
| VariableSize | 512 | 8.008 ns | 6.935 ns | 0.87 | - → - | 131 B → 136 B |
| ConstantSizeSkipInit | 512 | 1.412 ns | 1.411 ns | 1.00 | - → - | 113 B → 119 B |
| VariableSizeSkipInit | 512 | 2.804 ns | 2.806 ns | 1.00 | - → - | 156 B → 161 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.73 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Size | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |----- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| **ConstantSize**         | **.NET 10.0**           | **.NET 10.0** | **64**   | **2.244 ns** | **0.0090 ns** | **0.0131 ns** | **2.225 ns** | **2.283 ns** | **2.259 ns** |  **1.00** |    **0.00** |     **172 B** |         **-** |          **NA** |
| VariableSize         | .NET 10.0           | .NET 10.0 | 64   | 1.815 ns | 0.0237 ns | 0.0348 ns | 1.769 ns | 1.864 ns | 1.854 ns |  0.81 |    0.02 |     131 B |         - |          NA |
| ConstantSizeSkipInit | .NET 10.0           | .NET 10.0 | 64   | 1.413 ns | 0.0034 ns | 0.0048 ns | 1.405 ns | 1.422 ns | 1.418 ns |  0.63 |    0.00 |     113 B |         - |          NA |
| VariableSizeSkipInit | .NET 10.0           | .NET 10.0 | 64   | 2.807 ns | 0.0110 ns | 0.0165 ns | 2.781 ns | 2.851 ns | 2.824 ns |  1.25 |    0.01 |     156 B |         - |          NA |
|                      |                     |           |      |          |           |           |          |          |          |       |         |           |           |             |
| ConstantSize         | MediumRun-.NET 11.0 | .NET 11.0 | 64   | 2.228 ns | 0.0070 ns | 0.0105 ns | 2.210 ns | 2.248 ns | 2.241 ns |  1.00 |    0.00 |     178 B |         - |          NA |
| VariableSize         | MediumRun-.NET 11.0 | .NET 11.0 | 64   | 1.742 ns | 0.0107 ns | 0.0149 ns | 1.718 ns | 1.771 ns | 1.763 ns |  0.78 |    0.01 |     136 B |         - |          NA |
| ConstantSizeSkipInit | MediumRun-.NET 11.0 | .NET 11.0 | 64   | 1.415 ns | 0.0052 ns | 0.0075 ns | 1.404 ns | 1.439 ns | 1.424 ns |  0.64 |    0.00 |     119 B |         - |          NA |
| VariableSizeSkipInit | MediumRun-.NET 11.0 | .NET 11.0 | 64   | 2.796 ns | 0.0118 ns | 0.0172 ns | 2.766 ns | 2.842 ns | 2.817 ns |  1.25 |    0.01 |     161 B |         - |          NA |
|                      |                     |           |      |          |           |           |          |          |          |       |         |           |           |             |
| **ConstantSize**         | **.NET 10.0**           | **.NET 10.0** | **512**  | **2.218 ns** | **0.0082 ns** | **0.0122 ns** | **2.189 ns** | **2.238 ns** | **2.234 ns** |  **1.00** |    **0.00** |     **172 B** |         **-** |          **NA** |
| VariableSize         | .NET 10.0           | .NET 10.0 | 512  | 8.008 ns | 0.0587 ns | 0.0878 ns | 7.875 ns | 8.218 ns | 8.119 ns |  3.61 |    0.04 |     131 B |         - |          NA |
| ConstantSizeSkipInit | .NET 10.0           | .NET 10.0 | 512  | 1.412 ns | 0.0054 ns | 0.0081 ns | 1.400 ns | 1.431 ns | 1.423 ns |  0.64 |    0.00 |     113 B |         - |          NA |
| VariableSizeSkipInit | .NET 10.0           | .NET 10.0 | 512  | 2.804 ns | 0.0157 ns | 0.0220 ns | 2.776 ns | 2.867 ns | 2.829 ns |  1.26 |    0.01 |     156 B |         - |          NA |
|                      |                     |           |      |          |           |           |          |          |          |       |         |           |           |             |
| ConstantSize         | MediumRun-.NET 11.0 | .NET 11.0 | 512  | 2.230 ns | 0.0076 ns | 0.0106 ns | 2.213 ns | 2.256 ns | 2.242 ns |  1.00 |    0.00 |     178 B |         - |          NA |
| VariableSize         | MediumRun-.NET 11.0 | .NET 11.0 | 512  | 6.935 ns | 0.0248 ns | 0.0348 ns | 6.879 ns | 7.018 ns | 6.974 ns |  3.11 |    0.02 |     136 B |         - |          NA |
| ConstantSizeSkipInit | MediumRun-.NET 11.0 | .NET 11.0 | 512  | 1.411 ns | 0.0045 ns | 0.0065 ns | 1.401 ns | 1.426 ns | 1.418 ns |  0.63 |    0.00 |     119 B |         - |          NA |
| VariableSizeSkipInit | MediumRun-.NET 11.0 | .NET 11.0 | 512  | 2.806 ns | 0.0099 ns | 0.0145 ns | 2.773 ns | 2.840 ns | 2.821 ns |  1.26 |    0.01 |     161 B |         - |          NA |

## Earlier record

- Verdict: adopted
- Constant 512 + SkipLocalsInit: 0.27 ns; variable size + SkipLocalsInit: 1.76-1.78 ns (localloc); variable 512 zero-initialized: 6.09 ns
- A constant size removes the localloc instruction and turns zero-init into a fixed-size fill the JIT can elide with SkipLocalsInit - the only combination that stays sub-nanosecond
- Also demonstrates MEM-01 (SkipLocalsInit): 1.23 -> 0.29 ns at 64 B

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Size | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **ConstantSize**         | **64**   | **1.2314 ns** | **0.0067 ns** | **0.0101 ns** | **1.2311 ns** | **1.2161 ns** | **1.2545 ns** | **1.2431 ns** |  **1.00** |    **0.01** |     **172 B** |         **-** |          **NA** |
| VariableSize         | 64   | 0.8869 ns | 0.0233 ns | 0.0334 ns | 0.8757 ns | 0.8622 ns | 1.0105 ns | 0.9259 ns |  0.72 |    0.03 |     131 B |         - |          NA |
| ConstantSizeSkipInit | 64   | 0.2874 ns | 0.0346 ns | 0.0473 ns | 0.2752 ns | 0.2677 ns | 0.5074 ns | 0.2994 ns |  0.23 |    0.04 |     113 B |         - |          NA |
| VariableSizeSkipInit | 64   | 1.7841 ns | 0.0268 ns | 0.0402 ns | 1.7608 ns | 1.7386 ns | 1.8548 ns | 1.8416 ns |  1.45 |    0.03 |     156 B |         - |          NA |
|                      |      |           |           |           |           |           |           |           |       |         |           |           |             |
| **ConstantSize**         | **512**  | **1.4104 ns** | **0.1546 ns** | **0.2116 ns** | **1.2460 ns** | **1.2147 ns** | **1.8104 ns** | **1.6993 ns** |  **1.02** |    **0.21** |     **172 B** |         **-** |          **NA** |
| VariableSize         | 512  | 6.0875 ns | 0.0243 ns | 0.0341 ns | 6.0855 ns | 6.0310 ns | 6.2123 ns | 6.1148 ns |  4.41 |    0.61 |     131 B |         - |          NA |
| ConstantSizeSkipInit | 512  | 0.2737 ns | 0.0044 ns | 0.0065 ns | 0.2739 ns | 0.2623 ns | 0.2867 ns | 0.2826 ns |  0.20 |    0.03 |     113 B |         - |          NA |
| VariableSizeSkipInit | 512  | 1.7595 ns | 0.0080 ns | 0.0110 ns | 1.7588 ns | 1.7412 ns | 1.7900 ns | 1.7748 ns |  1.27 |    0.18 |     156 B |         - |          NA |
