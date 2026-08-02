# TYP-05: Unsafe.As vs castclass vs is-pattern (type-guaranteed reference)

- Verdict: adopted (where the invariant is structurally guaranteed)
- castclass 718.4 ns (high variance) / is-pattern 551.5 ns (0.82x) / Unsafe.As 344.7 ns (0.51x)
- Code size 274 / 57 / 33 B - the cast helper and its EH path dominate the castclass form
- Safety: no type check at all; a wrong type is silent corruption. Restrict to registries where the value's type is established at registration

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method    | Mean     | Error     | StdDev    | Min      | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|---------- |---------:|----------:|----------:|---------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| CastClass | 718.4 ns | 133.61 ns | 195.85 ns | 539.8 ns | 1,102.0 ns | 1,045.9 ns |  1.07 |    0.38 |     274 B |         - |          NA |
| IsPattern | 551.5 ns |   6.42 ns |   9.21 ns | 533.8 ns |   570.0 ns |   561.1 ns |  0.82 |    0.19 |      57 B |         - |          NA |
| UnsafeAs  | 344.7 ns |   5.23 ns |   7.33 ns | 333.7 ns |   361.4 ns |   354.3 ns |  0.51 |    0.12 |      33 B |         - |          NA |
