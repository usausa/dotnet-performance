# MEM-06: Struct argument passing (by value vs in)

- Verdict: adopted
- 8-byte struct: in 0.83x / 32-byte: 0.73x / 64-byte: 0.55x vs by-value (non-inlined call)
- Defensive copy trap confirmed: in + non-readonly member = 1.86x slower than in + readonly member, code size 219 B vs 109 B
- All variants allocation-free; the win grows with struct size

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| Size8ByValue         | 1.700 ns | 0.1086 ns | 0.1625 ns | 1.403 ns | 1.865 ns | 1.845 ns |  1.01 |    0.14 |      49 B |         - |          NA |
| Size8ByIn            | 1.406 ns | 0.0373 ns | 0.0523 ns | 1.333 ns | 1.487 ns | 1.474 ns |  0.83 |    0.09 |      51 B |         - |          NA |
| Size32ByValue        | 1.562 ns | 0.0164 ns | 0.0245 ns | 1.519 ns | 1.605 ns | 1.590 ns |  0.93 |    0.10 |      76 B |         - |          NA |
| Size32ByIn           | 1.144 ns | 0.0091 ns | 0.0131 ns | 1.123 ns | 1.177 ns | 1.159 ns |  0.68 |    0.07 |      63 B |         - |          NA |
| Size64ByValue        | 2.473 ns | 0.0176 ns | 0.0263 ns | 2.430 ns | 2.523 ns | 2.501 ns |  1.47 |    0.15 |     103 B |         - |          NA |
| Size64ByIn           | 1.368 ns | 0.0103 ns | 0.0154 ns | 1.347 ns | 1.400 ns | 1.389 ns |  0.81 |    0.08 |      79 B |         - |          NA |
| InWithReadonlyMember | 1.596 ns | 0.0097 ns | 0.0142 ns | 1.571 ns | 1.628 ns | 1.613 ns |  0.95 |    0.10 |     109 B |         - |          NA |
| InWithMutableMember  | 2.968 ns | 0.0443 ns | 0.0662 ns | 2.887 ns | 3.117 ns | 3.069 ns |  1.76 |    0.18 |     219 B |         - |          NA |
