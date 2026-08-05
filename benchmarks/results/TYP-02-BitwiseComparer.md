# TYP-02: BitwiseComparer vs default comparers (16-byte struct keys)

- Verdict: adopted (for structs without usable IEquatable)
- Dictionary lookup per key: default comparer on plain struct 25.7 ns + 96 B boxing per lookup (ObjectEqualityComparer path)
- BitwiseComparer on the same plain struct: 11.8 ns / 0 B (0.46x, no boxing, no Equals to write)
- Hand-written IEquatable is still fastest: 5.6 ns (0.22x) - implement it when you own the type; use BitwiseComparer for external types or to bypass custom Equals

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| DefaultComparerPlain     | 15.772 ns | 0.4323 ns | 0.6061 ns | 15.189 ns | 17.973 ns | 16.574 ns |  1.00 |    0.05 | 0.0115 |   5,509 B |      96 B |        1.00 |
| DefaultComparerEquatable |  3.687 ns | 0.0551 ns | 0.0808 ns |  3.579 ns |  3.949 ns |  3.759 ns |  0.23 |    0.01 |      - |     620 B |         - |        0.00 |
| BitwiseComparerPlain     |  8.429 ns | 0.0372 ns | 0.0521 ns |  8.340 ns |  8.514 ns |  8.489 ns |  0.54 |    0.02 |      - |   2,128 B |         - |        0.00 |
