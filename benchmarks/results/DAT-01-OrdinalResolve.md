# DAT-01: Column resolution strategies (per-row cost, 3 columns)

- Verdict: adopted
- GetOrdinal per row: 11.3 ns/row -> cached ordinals struct + in: 1.42 ns/row (0.13x, ~8x faster), code size 2,225 B -> 537 B
- GetValue + cast instead of typed getters: 7.18 ns/row + 48 B/row boxing (int + bool) - use GetInt32/GetString/GetBoolean
- In-memory sealed reader stand-in; real-provider virtual dispatch and I/O are out of scope (deltas isolate the resolution strategy)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|-------:|----------:|------------:|
| GetOrdinalPerRow             | 11.252 ns | 0.1609 ns | 0.2358 ns | 10.862 ns | 11.953 ns | 11.545 ns |  1.00 |    0.03 |   2,225 B |      - |         - |          NA |
| CachedOrdinalsStruct         |  1.423 ns | 0.0097 ns | 0.0143 ns |  1.395 ns |  1.455 ns |  1.441 ns |  0.13 |    0.00 |     537 B |      - |         - |          NA |
| CachedOrdinalsGetValueBoxing |  7.177 ns | 0.1596 ns | 0.2389 ns |  6.858 ns |  7.763 ns |  7.495 ns |  0.64 |    0.02 |   1,200 B | 0.0029 |      48 B |          NA |
