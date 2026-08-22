# LAB-BitCast: Unsafe.BitCast vs Unsafe.As (study queue 7-7)

- Verdict: no difference in codegen -> adopt BitCast as the default on the safety axis
- All five variants produce an identical instruction stream: 57 B for the three concrete-type forms
  (Unsafe.As, Unsafe.BitCast, BitConverter.SingleToInt32Bits) and 21 B for the two generic forms
- In the generic case with T = int the reinterpretation disappears entirely; only a movsxd + add loop remains
- Identical code is the argument for switching: Unsafe.As<TFrom, TTo> silently accepts a size mismatch and
  corrupts memory, BitCast refuses it. The safer form costs nothing
- BitConverter.SingleToInt32Bits folds to the same code as well, so when a BCL API exists for the concrete
  type pair there is no reason to reach for Unsafe at all

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                   | Mean     | Error   | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------- |---------:|--------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| UnsafeAsReinterpret      | 241.5 ns | 5.42 ns |  8.12 ns | 228.5 ns | 254.2 ns | 251.5 ns |  1.00 |    0.05 |      57 B |         - |          NA |
| UnsafeBitCastReinterpret | 243.9 ns | 6.49 ns |  9.72 ns | 230.0 ns | 260.6 ns | 254.3 ns |  1.01 |    0.05 |      57 B |         - |          NA |
| BitConverterReinterpret  | 243.6 ns | 5.72 ns |  8.56 ns | 230.9 ns | 257.0 ns | 252.9 ns |  1.01 |    0.05 |      57 B |         - |          NA |
| GenericUnsafeAs          | 237.5 ns | 6.14 ns |  9.19 ns | 224.8 ns | 254.8 ns | 250.4 ns |  0.98 |    0.05 |      21 B |         - |          NA |
| GenericBitCast           | 239.7 ns | 7.11 ns | 10.43 ns | 225.1 ns | 256.9 ns | 251.7 ns |  0.99 |    0.05 |      21 B |         - |          NA |

