# TYP-01: TypeMap / TypeSlot

- Verdict: adopted (implemented)
- Generic path (TypeSlot<T>.Index): 0.14 ns, 0.02x vs Dictionary<Type,T> (~54x faster), code 34 B
- Runtime Type path: 14.0 ns, 1.93x vs Dictionary (dictionary lookup + array access) - use only when the type is not known statically
- FrozenDictionary 1.22x (slower than Dictionary here, consistent with R-08)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| DictionaryLookup   |  7.2690 ns | 0.1975 ns | 0.2956 ns |  6.6419 ns |  7.7660 ns |  7.6817 ns |  1.00 |    0.06 |     921 B |         - |          NA |
| FrozenLookup       |  8.8450 ns | 0.2796 ns | 0.4184 ns |  8.1824 ns |  9.6852 ns |  9.4436 ns |  1.22 |    0.08 |      45 B |         - |          NA |
| TypeMapGeneric     |  0.1354 ns | 0.0914 ns | 0.1339 ns |  0.0000 ns |  0.3611 ns |  0.3051 ns |  0.02 |    0.02 |      34 B |         - |          NA |
| TypeMapRuntimeType | 14.0063 ns | 0.5084 ns | 0.7452 ns | 11.9441 ns | 15.7552 ns | 14.7482 ns |  1.93 |    0.13 |   3,486 B |         - |          NA |
