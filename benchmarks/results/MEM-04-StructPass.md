# MEM-04: Struct argument passing (by value vs in)

- Verdict: conditional - on x86-64-v4 the by-value copy penalty largely disappeared, and "the win grows with struct size" no longer holds
- Comparing each pair directly: 8-byte in/by-value 0.99x, **32-byte 0.81x** (1.164 vs 1.444 ns, non-overlapping CIs), **64-byte 0.98x** (1.211 vs 1.235 ns)
- On the previous Ryzen 9 5900X baseline the same pairs were 0.83x / 0.73x / 0.55x, i.e. monotonically better with size. Here only the 32-byte case still pays
- Everything except the mutable-member case now sits in a 1.16-1.44 ns band, which is the non-inlined call overhead itself - the copy is no longer what dominates
- **Defensive copy trap is the finding that survives intact**: in + non-readonly member 1.879 ns = 1.57x of in + readonly member, code size 219 B vs 109 B
- All variants allocation-free. Keep `in` for large readonly structs (it never costs anything and code size drops: 63/79 B vs 76/99 B), but do not expect it to show up in timings on this hardware

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| Size8ByValue         | 1.214 ns | 0.0124 ns | 0.0173 ns | 1.185 ns | 1.265 ns | 1.230 ns |  1.00 |    0.02 |      49 B |         - |          NA |
| Size8ByIn            | 1.207 ns | 0.0083 ns | 0.0114 ns | 1.183 ns | 1.230 ns | 1.220 ns |  1.00 |    0.02 |      51 B |         - |          NA |
| Size32ByValue        | 1.444 ns | 0.0174 ns | 0.0255 ns | 1.381 ns | 1.492 ns | 1.473 ns |  1.19 |    0.03 |      76 B |         - |          NA |
| Size32ByIn           | 1.164 ns | 0.0306 ns | 0.0458 ns | 1.086 ns | 1.255 ns | 1.219 ns |  0.96 |    0.04 |      63 B |         - |          NA |
| Size64ByValue        | 1.235 ns | 0.0359 ns | 0.0515 ns | 1.137 ns | 1.353 ns | 1.302 ns |  1.02 |    0.04 |      99 B |         - |          NA |
| Size64ByIn           | 1.211 ns | 0.0125 ns | 0.0186 ns | 1.176 ns | 1.242 ns | 1.238 ns |  1.00 |    0.02 |      79 B |         - |          NA |
| InWithReadonlyMember | 1.199 ns | 0.0236 ns | 0.0345 ns | 1.158 ns | 1.295 ns | 1.238 ns |  0.99 |    0.03 |     109 B |         - |          NA |
| InWithMutableMember  | 1.879 ns | 0.0182 ns | 0.0261 ns | 1.849 ns | 1.963 ns | 1.905 ns |  1.55 |    0.03 |     219 B |         - |          NA |
