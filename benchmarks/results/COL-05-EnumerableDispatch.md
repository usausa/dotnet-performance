# COL-05: IEnumerable concrete-type dispatch

- Verdict: adopted (conditional) - the conditions tightened
- List source 0.83x via CollectionsMarshal.AsSpan branch (253.7 -> 210.2 ns). Was 0.56x on the previous Ryzen 9 5900X baseline: List's own enumerator gained 1.94x from the newer core, so most of the win evaporated
- Array source: no gain on net10 (guarded devirtualization already handles it; 213.8 vs 209.8 ns)
- **The iterator fallback now costs 1.13x** (486.7 -> 552.0 ns, non-overlapping CIs). It was free on the previous baseline (829.0 vs 822.8 ns), so the type tests are no longer a "free bet" - a lazy-iterator argument pays for the branches it never uses
- Net: worth it when the input is predominantly List; against it when lazy iterators are common. On AOT the array branch remains valuable (no dynamic PGO)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method            | Mean     | Error    | StdDev   | Min      | Max      | P90      | Code Size | Allocated |
|------------------ |---------:|---------:|---------:|---------:|---------:|---------:|----------:|----------:|
| EnumerateArray    | 213.8 ns |  0.55 ns |  0.79 ns | 212.6 ns | 215.9 ns | 214.9 ns |     203 B |         - |
| DispatchArray     | 209.8 ns |  0.50 ns |  0.70 ns | 208.2 ns | 211.5 ns | 210.6 ns |     767 B |         - |
| EnumerateList     | 253.7 ns |  2.06 ns |  2.96 ns | 247.9 ns | 261.1 ns | 256.7 ns |     346 B |         - |
| DispatchList      | 210.2 ns |  0.79 ns |  1.11 ns | 209.1 ns | 212.9 ns | 211.9 ns |     786 B |         - |
| EnumerateIterator | 486.7 ns |  3.87 ns |  5.29 ns | 480.2 ns | 498.6 ns | 496.5 ns |     542 B |         - |
| DispatchIterator  | 552.0 ns | 28.85 ns | 42.29 ns | 501.2 ns | 623.2 ns | 598.8 ns |   1,175 B |         - |
