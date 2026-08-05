# TYP-05: Unsafe.As vs castclass vs is-pattern (type-guaranteed reference)

- Verdict: adopted (where the invariant is structurally guaranteed)
- castclass 718.4 ns (high variance) / is-pattern 551.5 ns (0.82x) / Unsafe.As 344.7 ns (0.51x)
- Code size 274 / 57 / 33 B - the cast helper and its EH path dominate the castclass form
- Safety: no type check at all; a wrong type is silent corruption. Restrict to registries where the value's type is established at registration

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method    | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|---------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| CastClass | 335.5 ns | 2.02 ns | 2.77 ns | 331.6 ns | 341.2 ns | 339.1 ns |  1.00 |     274 B |         - |          NA |
| IsPattern | 324.7 ns | 1.82 ns | 2.49 ns | 321.7 ns | 330.7 ns | 328.9 ns |  0.97 |      57 B |         - |          NA |
| UnsafeAs  | 212.8 ns | 1.23 ns | 1.76 ns | 210.5 ns | 217.3 ns | 215.2 ns |  0.63 |      33 B |         - |          NA |
