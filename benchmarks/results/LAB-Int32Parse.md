# LAB: Int32 decoding — byte-by-byte vs Span vs pointer (R-09 follow-up)

> ⏳ **仮測定 / provisional** — B550H (AMD Ryzen 9 5900X, x86-64-v3, .NET 10.0.11). The catalog's reference environment is HX 370 (x86-64-v4); re-run there with `--filter "*Int32ParseBenchmark*"` and replace the tables below. Verdicts rest on generated code and allocation counts, which do not depend on the machine.
>
> ❗ **想定外 / unexpected** — this result contradicts (or goes beyond) the source article's claim. The verdict written below is provisional; decide adoption or rejection only after the HX 370 run confirms or overturns it.

- Verdict: MemoryMarshal.Cast<byte, int> + plain loop is the fastest form: 0.26x of byte-by-byte (240.9 ns, 54 B). fixed + int* is 0.52x (483.6 ns, 97 B) — Cast is 2x faster than the pointer
- BinaryPrimitives.ReadInt32LittleEndian per element is also 0.52x: this is almost certainly the 'Span' variant behind the NDepend article's 0.26x-vs-0.19x claim, since its 0.26x matches Cast exactly
- R-09 stands and is reinforced: reinterpret once with Cast, then index; do not reach for pointers

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9445/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]              : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method         | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| ByteByByte     | 928.1 ns | 16.34 ns | 24.46 ns | 914.7 ns | 902.6 ns | 985.5 ns | 964.5 ns |  1.00 |    0.04 |     200 B |         - |          NA |
| SpanPerElement | 482.7 ns | 16.70 ns | 24.99 ns | 482.6 ns | 450.7 ns | 551.8 ns | 514.4 ns |  0.52 |    0.03 |      85 B |         - |          NA |
| SpanCast       | 240.9 ns |  4.80 ns |  7.19 ns | 239.8 ns | 229.5 ns | 259.0 ns | 250.0 ns |  0.26 |    0.01 |      54 B |         - |          NA |
| Pointer        | 483.6 ns | 11.61 ns | 17.38 ns | 476.1 ns | 464.9 ns | 519.2 ns | 506.7 ns |  0.52 |    0.02 |      97 B |         - |          NA |
