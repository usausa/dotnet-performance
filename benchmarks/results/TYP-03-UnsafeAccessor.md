# TYP-03: UnsafeAccessor vs reflection for private field access

- Verdict: adopted
- UnsafeAccessor 0.192 ns == public property 0.192 ns (code size 23 B both - compiles to a direct field load)
- FieldInfo.GetValue: 4.77 ns + 24 B boxing per read (24.9x)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| PublicProperty      | 0.1917 ns | 0.0014 ns | 0.0019 ns | 0.1879 ns | 0.1957 ns | 0.1941 ns |  1.00 |    0.01 |      - |      23 B |         - |          NA |
| UnsafeAccessorField | 0.1922 ns | 0.0019 ns | 0.0026 ns | 0.1879 ns | 0.1985 ns | 0.1958 ns |  1.00 |    0.02 |      - |      23 B |         - |          NA |
| ReflectionGetValue  | 4.7727 ns | 0.0699 ns | 0.0933 ns | 4.5929 ns | 5.0580 ns | 4.8853 ns | 24.90 |    0.54 | 0.0029 |   4,311 B |      24 B |          NA |
