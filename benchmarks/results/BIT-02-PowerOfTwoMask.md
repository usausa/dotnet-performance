# BIT-02: Power-of-two mask vs modulo (1024 bucket-index calculations)

- Verdict: adopted (for runtime-sized tables); const-size modulo needs no manual mask
- Runtime-size modulo: 1,203.5 ns (div instruction) vs power-of-two mask 215.3 ns (0.18x)
- Const-size modulo: 213.3 ns (0.18x) - the JIT already lowers 'hash % 64' with a constant power-of-two to AND form; manual masking is only needed when the size is a runtime value
- Mask and const-modulo are identical (both 51 B of code, same time)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean       | Error   | StdDev  | Min        | Max        | P90        | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------ |-----------:|--------:|--------:|-----------:|-----------:|-----------:|------:|----------:|----------:|------------:|
| RuntimeSizeModulo | 1,203.5 ns | 4.16 ns | 5.97 ns | 1,197.7 ns | 1,221.9 ns | 1,211.8 ns |  1.00 |      57 B |         - |          NA |
| PowerOfTwoMask    |   215.3 ns | 4.07 ns | 5.70 ns |   211.2 ns |   231.5 ns |   225.2 ns |  0.18 |      51 B |         - |          NA |
| ConstSizeModulo   |   213.3 ns | 0.80 ns | 1.14 ns |   212.0 ns |   216.1 ns |   215.2 ns |  0.18 |      51 B |         - |          NA |
