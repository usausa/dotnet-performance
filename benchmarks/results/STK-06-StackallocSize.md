# STK-06: Constant-size stackalloc

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
