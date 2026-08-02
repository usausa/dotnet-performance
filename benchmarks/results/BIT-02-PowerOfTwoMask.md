# BIT-02: Power-of-two mask vs modulo (1024 bucket-index calculations)

- Verdict: adopted (for runtime-sized tables); const-size modulo needs no manual mask
- Runtime-size modulo: 1,344.8 ns (div instruction) vs power-of-two mask 310.3 ns (0.23x)
- Const-size modulo: 253.0 ns (0.19x) - the JIT already lowers 'hash % 64' with a constant power-of-two to AND form; manual masking is only needed when the size is a runtime value
- Mask vs const-modulo: mins match (251 vs 253 ns); the mask row's higher mean comes from run variance

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------ |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| RuntimeSizeModulo | 1,344.8 ns | 17.52 ns | 26.22 ns | 1,308.8 ns | 1,394.5 ns | 1,374.1 ns |  1.00 |    0.03 |      57 B |         - |          NA |
| PowerOfTwoMask    |   310.3 ns | 29.73 ns | 44.50 ns |   251.4 ns |   393.7 ns |   379.6 ns |  0.23 |    0.03 |      51 B |         - |          NA |
| ConstSizeModulo   |   253.0 ns | 11.97 ns | 17.55 ns |   229.2 ns |   284.4 ns |   273.4 ns |  0.19 |    0.01 |      51 B |         - |          NA |
