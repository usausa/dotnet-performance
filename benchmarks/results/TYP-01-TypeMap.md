# TYP-01: TypeMap / TypeSlot

- Verdict: adopted (implemented)
- Generic path (TypeSlot<T>.Index): 0.14 ns, 0.02x vs Dictionary<Type,T> (~54x faster), code 34 B
- Runtime Type path: 14.0 ns, 1.93x vs Dictionary (dictionary lookup + array access) - use only when the type is not known statically
- FrozenDictionary 1.22x (slower than Dictionary here, consistent with R-08)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DictionaryLookup   |  2.4681 ns | 0.0505 ns | 0.0691 ns |  2.3476 ns |  2.6277 ns |  2.5262 ns |  1.00 |    0.04 |     921 B |         - |          NA |
| FrozenLookup       |  3.0734 ns | 0.0418 ns | 0.0558 ns |  2.9741 ns |  3.1824 ns |  3.1344 ns |  1.25 |    0.04 |      45 B |         - |          NA |
| TypeMapGeneric     |  0.2260 ns | 0.0555 ns | 0.0740 ns |  0.1697 ns |  0.4915 ns |  0.3030 ns |  0.09 |    0.03 |      34 B |         - |          NA |
| TypeMapRuntimeType | 10.4113 ns | 0.0451 ns | 0.0632 ns | 10.3206 ns | 10.5742 ns | 10.4934 ns |  4.22 |    0.12 |   3,486 B |         - |          NA |
