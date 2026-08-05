# DAT-01: Column resolution strategies (per-row cost, 3 columns)

- Verdict: adopted
- GetOrdinal per row: 11.3 ns/row -> cached ordinals struct + in: 1.42 ns/row (0.13x, ~8x faster), code size 2,225 B -> 537 B
- GetValue + cast instead of typed getters: 7.18 ns/row + 48 B/row boxing (int + bool) - use GetInt32/GetString/GetBoolean
- In-memory sealed reader stand-in; real-provider virtual dispatch and I/O are out of scope (deltas isolate the resolution strategy)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Mean      | Error     | StdDev    | Median   | Min       | Max      | P90      | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|----------------------------- |----------:|----------:|----------:|---------:|----------:|---------:|---------:|------:|--------:|-------:|----------:|----------:|------------:|
| GetOrdinalPerRow             | 7.4462 ns | 0.0616 ns | 0.0864 ns | 7.421 ns | 7.3529 ns | 7.730 ns | 7.562 ns |  1.00 |    0.02 |      - |   2,219 B |         - |          NA |
| CachedOrdinalsStruct         | 0.9983 ns | 0.0368 ns | 0.0491 ns | 1.027 ns | 0.9323 ns | 1.057 ns | 1.049 ns |  0.13 |    0.01 |      - |     533 B |         - |          NA |
| CachedOrdinalsGetValueBoxing | 4.2622 ns | 0.0581 ns | 0.0852 ns | 4.282 ns | 3.9987 ns | 4.383 ns | 4.341 ns |  0.57 |    0.01 | 0.0057 |   1,169 B |      48 B |          NA |
