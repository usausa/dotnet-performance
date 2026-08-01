# STK-06: Constant-size stackalloc

- Verdict: adopted
- Constant 512 + SkipLocalsInit: 1.6 ns
- Variable size + SkipLocalsInit: 4.3-4.6 ns (localloc, ~3x)
- Variable 512 zero-initialized: 14.8 ns (~9x)
- Also demonstrates MEM-03 (SkipLocalsInit): 6.6 -> 1.6 ns

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Size | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |----- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| **ConstantSize**         | **64**   |  **6.568 ns** | **0.3831 ns** | **0.5615 ns** |  **5.517 ns** |  **7.639 ns** |  **7.383 ns** |  **1.01** |    **0.12** |     **373 B** |         **-** |          **NA** |
| VariableSize         | 64   |  2.880 ns | 0.1205 ns | 0.1766 ns |  2.518 ns |  3.177 ns |  3.090 ns |  0.44 |    0.05 |     131 B |         - |          NA |
| ConstantSizeSkipInit | 64   |  1.573 ns | 0.0570 ns | 0.0854 ns |  1.405 ns |  1.714 ns |  1.688 ns |  0.24 |    0.02 |     113 B |         - |          NA |
| VariableSizeSkipInit | 64   |  4.339 ns | 0.0698 ns | 0.1045 ns |  4.197 ns |  4.538 ns |  4.477 ns |  0.67 |    0.06 |     156 B |         - |          NA |
|                      |      |           |           |           |           |           |           |       |         |           |           |             |
| **ConstantSize**         | **512**  |  **6.756 ns** | **0.2728 ns** | **0.4084 ns** |  **5.951 ns** |  **7.490 ns** |  **7.348 ns** |  **1.00** |    **0.08** |     **373 B** |         **-** |          **NA** |
| VariableSize         | 512  | 14.813 ns | 0.3263 ns | 0.4884 ns | 13.373 ns | 15.693 ns | 15.303 ns |  2.20 |    0.15 |     131 B |         - |          NA |
| ConstantSizeSkipInit | 512  |  1.586 ns | 0.0594 ns | 0.0870 ns |  1.370 ns |  1.777 ns |  1.666 ns |  0.24 |    0.02 |     113 B |         - |          NA |
| VariableSizeSkipInit | 512  |  4.612 ns | 0.0832 ns | 0.1194 ns |  4.400 ns |  4.828 ns |  4.768 ns |  0.69 |    0.04 |     158 B |         - |          NA |
