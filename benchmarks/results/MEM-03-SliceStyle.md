# MEM-03: Slice(offset, length) vs range operator

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - Slice and the range operator both got faster on net11 (97.6 vs 101.0 ns, code 97 vs 100 B); the range operator keeps one extra instruction

### SliceStyleBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| SliceMethod | 110.16 ns | 97.64 ns | 0.89 | - → - | 100 B → 97 B |
| RangeOperator | 132.19 ns | 101.00 ns | 0.76 | - → - | 103 B → 100 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.51 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method        | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| SliceMethod   | .NET 10.0           | .NET 10.0 | 110.16 ns |  0.421 ns |  0.617 ns | 110.05 ns | 109.40 ns | 111.47 ns | 111.12 ns |  1.00 |    0.00 |     100 B |         - |          NA |
| RangeOperator | .NET 10.0           | .NET 10.0 | 132.19 ns | 17.525 ns | 26.231 ns | 132.41 ns | 105.45 ns | 159.42 ns | 158.71 ns |  1.20 |    0.23 |     103 B |         - |          NA |
|               |                     |           |           |           |           |           |           |           |           |       |         |           |           |             |
| SliceMethod   | MediumRun-.NET 11.0 | .NET 11.0 |  97.64 ns |  0.399 ns |  0.560 ns |  97.77 ns |  96.63 ns |  98.87 ns |  98.32 ns |  1.00 |    0.00 |      97 B |         - |          NA |
| RangeOperator | MediumRun-.NET 11.0 | .NET 11.0 | 101.00 ns |  5.581 ns |  8.180 ns |  94.44 ns |  92.48 ns | 110.47 ns | 109.70 ns |  1.03 |    0.08 |     100 B |         - |          NA |

## Earlier record

- Verdict: adopted (unchanged); the time difference on this hardware is measurement noise, the codegen difference is real
- 106.6 vs 107.0 ns for 256 slices, CIs overlap; code size 100 vs 103 B
- JIT level (disassembly): the loops are NOT identical - the range operator computes the end offset up front and carries an extra register shuffle (`mov r8d,r8d` / `mov r8d,r9d`), 15 vs 14 instructions per iteration; the bounds check is the same in both
- A wide out-of-order core absorbs the extra moves, so the difference sits below time resolution here; a narrower core may not absorb them as completely. Slice(offset, length) generates the tighter code either way, so keep preferring it in hot paths - it never costs anything

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method        | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| SliceMethod   | 106.6 ns | 0.35 ns | 0.50 ns | 105.6 ns | 107.5 ns | 107.3 ns |  1.00 |    0.01 |     100 B |         - |          NA |
| RangeOperator | 107.0 ns | 3.37 ns | 4.84 ns | 104.3 ns | 125.6 ns | 108.3 ns |  1.00 |    0.04 |     103 B |         - |          NA |
