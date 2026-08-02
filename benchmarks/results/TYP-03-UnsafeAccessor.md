# TYP-03: UnsafeAccessor vs reflection for private field access

- Verdict: adopted
- UnsafeAccessor 0.264 ns == public property 0.269 ns (code size 23 B both - compiles to a direct field load)
- FieldInfo.GetValue: 9.33 ns + 24 B boxing per read (34.8x)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max        | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|-----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| PublicProperty      | 0.2685 ns | 0.0052 ns | 0.0078 ns | 0.2579 ns |  0.2842 ns | 0.2788 ns |  1.00 |    0.04 |      23 B |      - |         - |          NA |
| UnsafeAccessorField | 0.2639 ns | 0.0036 ns | 0.0053 ns | 0.2568 ns |  0.2756 ns | 0.2703 ns |  0.98 |    0.03 |      23 B |      - |         - |          NA |
| ReflectionGetValue  | 9.3250 ns | 0.2676 ns | 0.3838 ns | 8.6116 ns | 10.1442 ns | 9.8179 ns | 34.76 |    1.72 |   4,329 B | 0.0014 |      24 B |          NA |
