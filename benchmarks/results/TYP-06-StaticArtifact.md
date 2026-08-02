# TYP-06: Static pre-built artifacts per type

- Verdict: adopted
- Generic static field read: 0.09 ns / code size 6 B (effectively free, same as TYP-01 generic path)
- Dictionary<Type,string> cache: 4.8 ns (0.042x vs rebuild); rebuild every call: 116 ns + 760 B
- Static generic beats dictionary cache by ~53x; use dictionary only when the type is not known statically

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean        | Error      | StdDev     | Median      | Min        | Max         | P90         | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|------------------- |------------:|-----------:|-----------:|------------:|-----------:|------------:|------------:|------:|--------:|----------:|-------:|----------:|------------:|
| BuildEveryCall     | 116.2396 ns | 14.0987 ns | 21.1023 ns | 110.5467 ns | 96.2528 ns | 170.2890 ns | 161.1019 ns | 1.025 |    0.23 |   5,238 B | 0.0453 |     760 B |        1.00 |
| DictionaryCache    |   4.8175 ns |  0.1509 ns |  0.2212 ns |   4.8670 ns |  4.4604 ns |   5.1208 ns |   5.0527 ns | 0.042 |    0.01 |     921 B |      - |         - |        0.00 |
| StaticGenericField |   0.0912 ns |  0.0981 ns |  0.1468 ns |   0.0175 ns |  0.0000 ns |   0.4253 ns |   0.3714 ns | 0.001 |    0.00 |       6 B |      - |         - |        0.00 |
