# VEC-02: Fixed-width intrinsics (byte shuffle), the case Vector<T> cannot express

- Verdict: adopted
- Byte shuffle beats the scalar loop by 0.46x (portable Vector128.Shuffle) on uint endianness reversal
- The width-agnostic Vector<T> arithmetic form reaches almost the same speed (0.50x) without any shuffle, but
  its code is 1.75x larger (333 vs 190 B)
- Ssse3.Shuffle measuring 0.24x is NOT an API difference. The two forms compile to a byte-identical
  instruction stream (190 B, same vpshufb). The 1.76x gap is code placement:
    Ssse3 loop     at ...9F5C, spans 9F5C-9F78, fits inside the 64 B window [9F40, 9F80)
    Vector128 loop at ...9FBC, spans 9FBC-9FD8, straddles the 64 B boundary at 9FC0
  Confirmed by adding byte-identical duplicate methods: each duplicate reproduced its original's address and
  time (Vector128...B 124.41 ns at ...9FBC, Ssse3...B 65.69 ns at ...9F5C). Swapping the declaration order
  does not move the placement
- Therefore: default to the portable Vector128.Shuffle. There is no performance reason to drop to raw ISA
  intrinsics here
- The element count is 1021, deliberately not a multiple of the vector width, so the scalar tail is exercised
```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean      | Error    | StdDev   | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |----------:|---------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| ScalarReverse           | 266.46 ns | 6.626 ns | 9.502 ns | 261.26 ns | 255.79 ns | 284.31 ns | 282.63 ns |  1.00 |    0.05 |     145 B |         - |          NA |
| Vector128ShuffleReverse | 121.53 ns | 3.171 ns | 4.746 ns | 120.53 ns | 115.74 ns | 131.07 ns | 126.84 ns |  0.46 |    0.02 |     190 B |         - |          NA |
| Ssse3ShuffleReverse     |  64.35 ns | 1.692 ns | 2.532 ns |  65.89 ns |  60.43 ns |  67.80 ns |  66.73 ns |  0.24 |    0.01 |     190 B |         - |          NA |
| VectorArithmeticReverse | 131.78 ns | 3.787 ns | 5.668 ns | 130.26 ns | 125.80 ns | 144.25 ns | 140.23 ns |  0.50 |    0.03 |     333 B |         - |          NA |

## Placement probe: byte-identical duplicate methods

Vector128ShuffleReverseB and Ssse3ShuffleReverseB are copies of the two originals with nothing changed but
the method name. Each duplicate reproduces its original both in address and in time, which is what rules out
an API difference and leaves code placement as the cause.

| Method                   | Mean      | Error    | StdDev   | Min       | Max       | P90       | Code Size | Allocated |
|------------------------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|----------:|
| Vector128ShuffleReverse  | 123.84 ns | 4.409 ns | 6.600 ns | 116.02 ns | 136.70 ns | 129.39 ns |     190 B |         - |
| Ssse3ShuffleReverse      |  70.49 ns | 2.105 ns | 3.150 ns |  66.31 ns |  75.04 ns |  74.36 ns |     190 B |         - |
| Ssse3ShuffleReverseB     |  65.69 ns | 2.167 ns | 3.243 ns |  60.58 ns |  72.83 ns |  69.50 ns |     190 B |         - |
| Vector128ShuffleReverseB | 124.41 ns | 3.518 ns | 5.266 ns | 115.98 ns | 132.97 ns | 130.51 ns |     190 B |         - |
