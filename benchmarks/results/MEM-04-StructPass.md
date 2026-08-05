# MEM-04: Struct argument passing (by value vs in)

- Verdict: adopted - the win grows with struct size and is decisive from 64 bytes up
- in vs by-value (non-inlined call): 8 B 1.01x / 16 B **0.84x** / 32 B 0.94x / 64 B **0.34x** / 128 B **0.48x** / 256 B **0.32x**
- **in is flat (~1.1-1.25 ns) at every size**; the by-value side grows with size and is alignment-sensitive - Size128ByValue is bimodal across launches (1.5-3.5 ns, RatioSD 0.78). Predictability is itself an argument for in
- The defensive copy remains the hazard: in + non-readonly member 1.85 ns = 1.51x of in + readonly member, code size 219 B vs 112 B
- All variants allocation-free; in also trims code size at every size (51-84 B vs 49-161 B)
- Guidance: pass readonly structs of 16 B+ by in; from 64 B it is a 2-3x win, and below that it never costs anything

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Min       | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|----------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| Size8ByValue         | 1.227 ns | 0.0095 ns | 0.0140 ns | 1.1945 ns | 1.253 ns | 1.240 ns |  1.00 |    0.02 |      49 B |         - |          NA |
| Size8ByIn            | 1.237 ns | 0.0207 ns | 0.0310 ns | 1.1585 ns | 1.276 ns | 1.268 ns |  1.01 |    0.03 |      51 B |         - |          NA |
| Size16ByValue        | 1.481 ns | 0.0180 ns | 0.0270 ns | 1.4511 ns | 1.558 ns | 1.512 ns |  1.21 |    0.03 |      65 B |         - |          NA |
| Size16ByIn           | 1.251 ns | 0.0082 ns | 0.0118 ns | 1.2349 ns | 1.281 ns | 1.265 ns |  1.02 |    0.01 |      55 B |         - |          NA |
| Size32ByValue        | 1.314 ns | 0.0192 ns | 0.0287 ns | 1.2667 ns | 1.361 ns | 1.350 ns |  1.07 |    0.03 |      76 B |         - |          NA |
| Size32ByIn           | 1.241 ns | 0.0062 ns | 0.0091 ns | 1.2250 ns | 1.255 ns | 1.254 ns |  1.01 |    0.01 |      63 B |         - |          NA |
| Size64ByValue        | 3.222 ns | 0.0089 ns | 0.0133 ns | 3.2013 ns | 3.252 ns | 3.240 ns |  2.63 |    0.03 |      99 B |         - |          NA |
| Size64ByIn           | 1.099 ns | 0.0213 ns | 0.0319 ns | 1.0279 ns | 1.168 ns | 1.142 ns |  0.90 |    0.03 |      79 B |         - |          NA |
| Size128ByValue       | 2.485 ns | 0.6513 ns | 0.9748 ns | 1.5102 ns | 3.485 ns | 3.470 ns |  2.03 |    0.78 |     119 B |         - |          NA |
| Size128ByIn          | 1.186 ns | 0.0548 ns | 0.0820 ns | 0.9062 ns | 1.239 ns | 1.236 ns |  0.97 |    0.07 |      66 B |         - |          NA |
| Size256ByValue       | 3.790 ns | 0.0047 ns | 0.0071 ns | 3.7783 ns | 3.802 ns | 3.798 ns |  3.09 |    0.04 |     161 B |         - |          NA |
| Size256ByIn          | 1.202 ns | 0.0456 ns | 0.0682 ns | 0.9474 ns | 1.246 ns | 1.240 ns |  0.98 |    0.06 |      84 B |         - |          NA |
| InWithReadonlyMember | 1.185 ns | 0.0421 ns | 0.0631 ns | 1.0630 ns | 1.256 ns | 1.252 ns |  0.97 |    0.05 |     112 B |         - |          NA |
| InWithMutableMember  | 1.847 ns | 0.0058 ns | 0.0087 ns | 1.8375 ns | 1.865 ns | 1.861 ns |  1.51 |    0.02 |     219 B |         - |          NA |
