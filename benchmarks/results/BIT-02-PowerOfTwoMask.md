# BIT-02: Power-of-two mask vs modulo (1024 bucket-index calculations)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - identical times and code sizes on both generations (runtime-size modulo 1,200 ns against the mask and the constant modulo at 213 ns)

### PowerOfTwoMaskBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| RuntimeSizeModulo | 1,200.5 ns | 1,200.2 ns | 1.00 | - → - | 57 B → 57 B |
| PowerOfTwoMask | 213.1 ns | 213.1 ns | 1.00 | - → - | 51 B → 51 B |
| ConstSizeModulo | 213.8 ns | 213.0 ns | 1.00 | - → - | 51 B → 51 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.53 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method            | Job                 | Runtime   | Mean       | Error   | StdDev  | Min        | Max        | P90        | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------ |-------------------- |---------- |-----------:|--------:|--------:|-----------:|-----------:|-----------:|------:|----------:|----------:|------------:|
| RuntimeSizeModulo | .NET 10.0           | .NET 10.0 | 1,200.5 ns | 1.35 ns | 1.98 ns | 1,198.3 ns | 1,206.1 ns | 1,203.8 ns |  1.00 |      57 B |         - |          NA |
| PowerOfTwoMask    | .NET 10.0           | .NET 10.0 |   213.1 ns | 0.63 ns | 0.88 ns |   211.5 ns |   214.3 ns |   214.2 ns |  0.18 |      51 B |         - |          NA |
| ConstSizeModulo   | .NET 10.0           | .NET 10.0 |   213.8 ns | 1.24 ns | 1.77 ns |   211.6 ns |   218.6 ns |   215.6 ns |  0.18 |      51 B |         - |          NA |
|                   |                     |           |            |         |         |            |            |            |       |           |           |             |
| RuntimeSizeModulo | MediumRun-.NET 11.0 | .NET 11.0 | 1,200.2 ns | 1.23 ns | 1.80 ns | 1,198.3 ns | 1,203.6 ns | 1,203.2 ns |  1.00 |      57 B |         - |          NA |
| PowerOfTwoMask    | MediumRun-.NET 11.0 | .NET 11.0 |   213.1 ns | 0.78 ns | 1.17 ns |   211.3 ns |   216.0 ns |   214.8 ns |  0.18 |      51 B |         - |          NA |
| ConstSizeModulo   | MediumRun-.NET 11.0 | .NET 11.0 |   213.0 ns | 0.59 ns | 0.79 ns |   211.9 ns |   215.4 ns |   214.2 ns |  0.18 |      51 B |         - |          NA |

## Earlier record

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
