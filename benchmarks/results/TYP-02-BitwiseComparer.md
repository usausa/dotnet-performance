# TYP-02: BitwiseComparer vs default comparers (16-byte struct keys)

- Verdict: adopted (for structs without usable IEquatable)
- Dictionary lookup per key: default comparer on plain struct 25.7 ns + 96 B boxing per lookup (ObjectEqualityComparer path)
- BitwiseComparer on the same plain struct: 11.8 ns / 0 B (0.46x, no boxing, no Equals to write)
- Hand-written IEquatable is still fastest: 5.6 ns (0.22x) - implement it when you own the type; use BitwiseComparer for external types or to bypass custom Equals

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| DefaultComparerPlain     | 25.686 ns | 0.8932 ns | 1.2810 ns | 24.140 ns | 28.796 ns | 27.562 ns |  1.00 |    0.07 | 0.0057 |   5,427 B |      96 B |        1.00 |
| DefaultComparerEquatable |  5.582 ns | 0.1738 ns | 0.2601 ns |  5.039 ns |  6.110 ns |  5.897 ns |  0.22 |    0.01 |      - |     620 B |         - |        0.00 |
| BitwiseComparerPlain     | 11.760 ns | 0.3144 ns | 0.4705 ns | 11.225 ns | 13.029 ns | 12.335 ns |  0.46 |    0.03 |      - |   2,121 B |         - |        0.00 |
