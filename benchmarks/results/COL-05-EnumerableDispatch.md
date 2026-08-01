# COL-05: IEnumerable concrete-type dispatch

- Verdict: adopted (conditional)
- List source 0.56x via CollectionsMarshal.AsSpan branch
- Array source: no gain on net10 (guarded devirtualization already handles it; 258.5 vs 272.4 ns)
- No penalty on iterator fallback (829.0 vs 822.8 ns)
- On AOT the array branch remains valuable (no dynamic PGO)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error    | StdDev   | Min      | Max      | P90      | Code Size | Allocated |
|------------------ |---------:|---------:|---------:|---------:|---------:|---------:|----------:|----------:|
| EnumerateArray    | 258.5 ns |  6.88 ns | 10.30 ns | 240.8 ns | 279.7 ns | 270.8 ns |     203 B |         - |
| DispatchArray     | 272.4 ns |  5.07 ns |  7.43 ns | 255.2 ns | 286.4 ns | 280.7 ns |     767 B |         - |
| EnumerateList     | 492.5 ns |  9.20 ns | 13.20 ns | 471.8 ns | 512.7 ns | 509.7 ns |     346 B |         - |
| DispatchList      | 277.3 ns | 25.32 ns | 37.89 ns | 250.3 ns | 382.8 ns | 331.1 ns |     786 B |         - |
| EnumerateIterator | 829.0 ns | 15.12 ns | 21.68 ns | 799.5 ns | 888.9 ns | 859.4 ns |     542 B |         - |
| DispatchIterator  | 822.8 ns | 12.07 ns | 18.07 ns | 793.4 ns | 866.1 ns | 846.4 ns |   1,175 B |         - |
