# MEM-05: Struct layout (field order, padding, LayoutKind)

- Verdict: adopted (conditional: scattered access)
- Sizes confirmed by Verify: Sequential/padded declaration order 32 B, Sequential/wide-first 24 B, LayoutKind.Auto 24 B
- Scattered traversal: 0.71x (packed) and 0.67x (auto) against the padded layout, CIs do not overlap
- Sequential traversal: 0.97-1.00x, CIs overlap. The prefetcher absorbs the footprint difference
- By value argument passing: 1.854 vs 2.034 ns (error bars do not overlap), the 24 B form is cheaper to copy
- Generated code is identical for all three types (65 B sequential / 93 B scattered). The difference is data side,
  not code side, so the "identical code means no difference" rule from the methodology does not apply here
- C# emits Sequential for structs by default, so the padded declaration order is what you get unless you either
  reorder the fields wide-first or mark the type [StructLayout(LayoutKind.Auto)]. Both reach 24 B

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method           | Mean          | Error         | StdDev        | Min           | Max           | P90           | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------- |--------------:|--------------:|--------------:|--------------:|--------------:|--------------:|------:|--------:|----------:|----------:|------------:|
| SequentialPadded | 14,839.794 ns |   402.7576 ns |   602.8287 ns | 14,196.465 ns | 15,673.035 ns | 15,607.353 ns | 1.002 |    0.06 |      65 B |         - |          NA |
| SequentialPacked | 14,741.606 ns |   364.7868 ns |   534.6999 ns | 14,181.685 ns | 15,800.589 ns | 15,574.031 ns | 0.995 |    0.05 |      65 B |         - |          NA |
| SequentialAuto   | 14,344.642 ns |   174.0211 ns |   232.3131 ns | 14,182.123 ns | 15,168.985 ns | 14,663.216 ns | 0.968 |    0.04 |      65 B |         - |          NA |
| ScatteredPadded  | 26,212.736 ns |   813.0870 ns | 1,216.9905 ns | 24,385.031 ns | 28,659.637 ns | 27,932.101 ns | 1.769 |    0.11 |      93 B |         - |          NA |
| ScatteredPacked  | 18,515.747 ns | 1,466.1910 ns | 2,055.3952 ns | 15,925.513 ns | 21,194.119 ns | 21,050.011 ns | 1.250 |    0.14 |      93 B |         - |          NA |
| ScatteredAuto    | 17,661.654 ns |   510.0610 ns |   763.4354 ns | 16,407.321 ns | 18,660.272 ns | 18,402.987 ns | 1.192 |    0.07 |      93 B |         - |          NA |
| ByValuePadded    |      2.034 ns |     0.0639 ns |     0.0957 ns |      1.927 ns |      2.267 ns |      2.185 ns | 0.000 |    0.00 |     102 B |         - |          NA |
| ByValuePacked    |      1.854 ns |     0.0557 ns |     0.0781 ns |      1.786 ns |      2.036 ns |      1.985 ns | 0.000 |    0.00 |     118 B |         - |          NA |

