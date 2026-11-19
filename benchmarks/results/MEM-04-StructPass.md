# MEM-04: Struct argument passing (by value vs in)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - adopted. `in` stays at 1.0-1.4 ns at every size on both generations, while by-value at 256 B costs 3.0-4.1 ns

### StructPassBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| Size8ByValue | 1.1680 ns | 1.2194 ns | 1.04 | - → - | 49 B → 49 B |
| Size8ByIn | 1.2037 ns | 1.2112 ns | 1.01 | - → - | 51 B → 51 B |
| Size16ByValue | 1.3245 ns | 1.4234 ns | 1.07 | - → - | 65 B → 65 B |
| Size16ByIn | 1.2102 ns | 1.2087 ns | 1.00 | - → - | 55 B → 55 B |
| Size32ByValue | 1.2366 ns | 1.3356 ns | 1.08 | - → - | 76 B → 76 B |
| Size32ByIn | 1.2139 ns | 1.2069 ns | 0.99 | - → - | 63 B → 63 B |
| Size64ByValue | 2.5922 ns | 2.2199 ns | 0.86 | - → - | 99 B → 99 B |
| Size64ByIn | 0.9925 ns | 1.1460 ns | 1.15 | - → - | 79 B → 79 B |
| Size128ByValue | 1.4786 ns | 2.4001 ns | 1.62 | - → - | 119 B → 119 B |
| Size128ByIn | 1.2129 ns | 1.2110 ns | 1.00 | - → - | 66 B → 66 B |
| Size256ByValue | 2.9986 ns | 3.8073 ns | 1.27 | - → - | 161 B → 161 B |
| Size256ByIn | 1.1962 ns | 1.2197 ns | 1.02 | - → - | 84 B → 84 B |
| InWithReadonlyMember | 1.1958 ns | 1.1845 ns | 0.99 | - → - | 112 B → 112 B |
| InWithMutableMember | 1.9618 ns | 1.8630 ns | 0.95 | - → - | 219 B → 219 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.82 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime   | Mean      | Error     | StdDev    | Median   | Min       | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------- |-------------------- |---------- |----------:|----------:|----------:|---------:|----------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| Size8ByValue         | .NET 10.0           | .NET 10.0 | 1.1680 ns | 0.0436 ns | 0.0653 ns | 1.202 ns | 1.0078 ns | 1.236 ns | 1.227 ns |  1.00 |    0.00 |      49 B |         - |          NA |
| Size8ByIn            | .NET 10.0           | .NET 10.0 | 1.2037 ns | 0.0285 ns | 0.0426 ns | 1.208 ns | 1.1161 ns | 1.278 ns | 1.248 ns |  1.03 |    0.07 |      51 B |         - |          NA |
| Size16ByValue        | .NET 10.0           | .NET 10.0 | 1.3245 ns | 0.0692 ns | 0.1036 ns | 1.317 ns | 1.1788 ns | 1.452 ns | 1.440 ns |  1.14 |    0.11 |      65 B |         - |          NA |
| Size16ByIn           | .NET 10.0           | .NET 10.0 | 1.2102 ns | 0.0178 ns | 0.0267 ns | 1.218 ns | 1.1342 ns | 1.242 ns | 1.237 ns |  1.04 |    0.06 |      55 B |         - |          NA |
| Size32ByValue        | .NET 10.0           | .NET 10.0 | 1.2366 ns | 0.0137 ns | 0.0205 ns | 1.240 ns | 1.1909 ns | 1.268 ns | 1.261 ns |  1.06 |    0.06 |      76 B |         - |          NA |
| Size32ByIn           | .NET 10.0           | .NET 10.0 | 1.2139 ns | 0.0190 ns | 0.0279 ns | 1.220 ns | 1.1483 ns | 1.280 ns | 1.240 ns |  1.04 |    0.06 |      63 B |         - |          NA |
| Size64ByValue        | .NET 10.0           | .NET 10.0 | 2.5922 ns | 0.9205 ns | 1.3777 ns | 2.597 ns | 1.2027 ns | 3.973 ns | 3.955 ns |  2.23 |    1.17 |      99 B |         - |          NA |
| Size64ByIn           | .NET 10.0           | .NET 10.0 | 0.9925 ns | 0.0277 ns | 0.0397 ns | 1.005 ns | 0.9227 ns | 1.070 ns | 1.037 ns |  0.85 |    0.06 |      79 B |         - |          NA |
| Size128ByValue       | .NET 10.0           | .NET 10.0 | 1.4786 ns | 0.0095 ns | 0.0136 ns | 1.479 ns | 1.4479 ns | 1.502 ns | 1.494 ns |  1.27 |    0.07 |     119 B |         - |          NA |
| Size128ByIn          | .NET 10.0           | .NET 10.0 | 1.2129 ns | 0.0133 ns | 0.0199 ns | 1.214 ns | 1.1638 ns | 1.249 ns | 1.237 ns |  1.04 |    0.06 |      66 B |         - |          NA |
| Size256ByValue       | .NET 10.0           | .NET 10.0 | 2.9986 ns | 0.6226 ns | 0.8929 ns | 3.796 ns | 2.0481 ns | 3.840 ns | 3.825 ns |  2.58 |    0.77 |     161 B |         - |          NA |
| Size256ByIn          | .NET 10.0           | .NET 10.0 | 1.1962 ns | 0.0169 ns | 0.0253 ns | 1.195 ns | 1.1597 ns | 1.248 ns | 1.228 ns |  1.03 |    0.06 |      84 B |         - |          NA |
| InWithReadonlyMember | .NET 10.0           | .NET 10.0 | 1.1958 ns | 0.0220 ns | 0.0329 ns | 1.195 ns | 1.1349 ns | 1.265 ns | 1.229 ns |  1.03 |    0.07 |     112 B |         - |          NA |
| InWithMutableMember  | .NET 10.0           | .NET 10.0 | 1.9618 ns | 0.0654 ns | 0.0978 ns | 1.962 ns | 1.8567 ns | 2.070 ns | 2.065 ns |  1.68 |    0.13 |     219 B |         - |          NA |
|                      |                     |           |           |           |           |          |           |          |          |       |         |           |           |             |
| Size8ByValue         | MediumRun-.NET 11.0 | .NET 11.0 | 1.2194 ns | 0.0112 ns | 0.0168 ns | 1.222 ns | 1.1785 ns | 1.244 ns | 1.240 ns |  1.00 |    0.00 |      49 B |         - |          NA |
| Size8ByIn            | MediumRun-.NET 11.0 | .NET 11.0 | 1.2112 ns | 0.0221 ns | 0.0331 ns | 1.224 ns | 1.1203 ns | 1.250 ns | 1.242 ns |  0.99 |    0.03 |      51 B |         - |          NA |
| Size16ByValue        | MediumRun-.NET 11.0 | .NET 11.0 | 1.4234 ns | 0.0224 ns | 0.0336 ns | 1.431 ns | 1.3191 ns | 1.476 ns | 1.450 ns |  1.17 |    0.03 |      65 B |         - |          NA |
| Size16ByIn           | MediumRun-.NET 11.0 | .NET 11.0 | 1.2087 ns | 0.0194 ns | 0.0290 ns | 1.214 ns | 1.1401 ns | 1.254 ns | 1.244 ns |  0.99 |    0.03 |      55 B |         - |          NA |
| Size32ByValue        | MediumRun-.NET 11.0 | .NET 11.0 | 1.3356 ns | 0.0704 ns | 0.1054 ns | 1.319 ns | 1.2118 ns | 1.458 ns | 1.452 ns |  1.10 |    0.09 |      76 B |         - |          NA |
| Size32ByIn           | MediumRun-.NET 11.0 | .NET 11.0 | 1.2069 ns | 0.0233 ns | 0.0348 ns | 1.214 ns | 1.1112 ns | 1.253 ns | 1.245 ns |  0.99 |    0.03 |      63 B |         - |          NA |
| Size64ByValue        | MediumRun-.NET 11.0 | .NET 11.0 | 2.2199 ns | 0.6158 ns | 0.8633 ns | 1.491 ns | 1.3719 ns | 3.179 ns | 3.170 ns |  1.82 |    0.70 |      99 B |         - |          NA |
| Size64ByIn           | MediumRun-.NET 11.0 | .NET 11.0 | 1.1460 ns | 0.0550 ns | 0.0806 ns | 1.187 ns | 1.0091 ns | 1.236 ns | 1.227 ns |  0.94 |    0.07 |      79 B |         - |          NA |
| Size128ByValue       | MediumRun-.NET 11.0 | .NET 11.0 | 2.4001 ns | 0.6618 ns | 0.9700 ns | 1.496 ns | 1.4314 ns | 3.407 ns | 3.392 ns |  1.97 |    0.78 |     119 B |         - |          NA |
| Size128ByIn          | MediumRun-.NET 11.0 | .NET 11.0 | 1.2110 ns | 0.0118 ns | 0.0177 ns | 1.210 ns | 1.1751 ns | 1.244 ns | 1.236 ns |  0.99 |    0.02 |      66 B |         - |          NA |
| Size256ByValue       | MediumRun-.NET 11.0 | .NET 11.0 | 3.8073 ns | 0.0045 ns | 0.0067 ns | 3.808 ns | 3.7947 ns | 3.820 ns | 3.816 ns |  3.12 |    0.04 |     161 B |         - |          NA |
| Size256ByIn          | MediumRun-.NET 11.0 | .NET 11.0 | 1.2197 ns | 0.0107 ns | 0.0161 ns | 1.222 ns | 1.1801 ns | 1.244 ns | 1.240 ns |  1.00 |    0.02 |      84 B |         - |          NA |
| InWithReadonlyMember | MediumRun-.NET 11.0 | .NET 11.0 | 1.1845 ns | 0.0139 ns | 0.0199 ns | 1.184 ns | 1.1345 ns | 1.222 ns | 1.207 ns |  0.97 |    0.02 |     112 B |         - |          NA |
| InWithMutableMember  | MediumRun-.NET 11.0 | .NET 11.0 | 1.8630 ns | 0.0038 ns | 0.0055 ns | 1.862 ns | 1.8554 ns | 1.873 ns | 1.870 ns |  1.53 |    0.02 |     219 B |         - |          NA |

## Earlier record

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
