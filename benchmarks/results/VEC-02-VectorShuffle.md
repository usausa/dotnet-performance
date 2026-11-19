# VEC-02: Fixed-width intrinsics (byte shuffle), the case Vector<T> cannot express

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. The Vector128 byte shuffle stays the fastest at 0.31x of the scalar loop on net11 (0.28x on net10)
- The shuffle forms are 1.07-1.10x slower on net11 (58.95 → 64.56 ns) and the arithmetic `Vector<T>` form 0.82x faster (103.6 → 85.0 ns, 0.40x of scalar), so the gap between them narrows from 1.76x to 1.32x

### VectorShuffleBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ScalarReverse | 211.20 ns | 210.58 ns | 1.00 | - → - | 145 B → 145 B |
| Vector128ShuffleReverse | 58.95 ns | 64.56 ns | 1.10 | - → - | 193 B → 193 B |
| Ssse3ShuffleReverse | 71.64 ns | 76.68 ns | 1.07 | - → - | 193 B → 193 B |
| VectorArithmeticReverse | 103.57 ns | 84.98 ns | 0.82 | - → - | 321 B → 327 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.73 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                  | Job                 | Runtime   | Mean      | Error    | StdDev   | Median    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |-------------------- |---------- |----------:|---------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| ScalarReverse           | .NET 10.0           | .NET 10.0 | 211.20 ns | 0.419 ns | 0.614 ns | 211.22 ns | 210.29 ns | 212.52 ns | 212.18 ns |  1.00 |    0.00 |     145 B |         - |          NA |
| Vector128ShuffleReverse | .NET 10.0           | .NET 10.0 |  58.95 ns | 0.502 ns | 0.736 ns |  58.87 ns |  57.84 ns |  60.58 ns |  59.81 ns |  0.28 |    0.00 |     193 B |         - |          NA |
| Ssse3ShuffleReverse     | .NET 10.0           | .NET 10.0 |  71.64 ns | 0.772 ns | 1.155 ns |  71.75 ns |  69.95 ns |  73.44 ns |  73.22 ns |  0.34 |    0.01 |     193 B |         - |          NA |
| VectorArithmeticReverse | .NET 10.0           | .NET 10.0 | 103.57 ns | 0.255 ns | 0.366 ns | 103.56 ns | 102.85 ns | 104.44 ns | 104.00 ns |  0.49 |    0.00 |     321 B |         - |          NA |
|                         |                     |           |           |          |          |           |           |           |           |       |         |           |           |             |
| ScalarReverse           | MediumRun-.NET 11.0 | .NET 11.0 | 210.58 ns | 0.351 ns | 0.492 ns | 210.52 ns | 209.81 ns | 211.73 ns | 211.16 ns |  1.00 |    0.00 |     145 B |         - |          NA |
| Vector128ShuffleReverse | MediumRun-.NET 11.0 | .NET 11.0 |  64.56 ns | 3.901 ns | 5.468 ns |  68.87 ns |  58.58 ns |  70.96 ns |  70.01 ns |  0.31 |    0.03 |     193 B |         - |          NA |
| Ssse3ShuffleReverse     | MediumRun-.NET 11.0 | .NET 11.0 |  76.68 ns | 0.296 ns | 0.434 ns |  76.78 ns |  75.88 ns |  77.51 ns |  77.17 ns |  0.36 |    0.00 |     193 B |         - |          NA |
| VectorArithmeticReverse | MediumRun-.NET 11.0 | .NET 11.0 |  84.98 ns | 1.756 ns | 2.628 ns |  85.19 ns |  81.60 ns |  88.36 ns |  87.76 ns |  0.40 |    0.01 |     327 B |         - |          NA |

## Earlier record

- Verdict: adopted. Re-measured twice on x86-64-v4 (Zen 5)
- Byte shuffle beats the scalar loop by **0.28-0.29x** on uint endianness reversal (x86-64-v3: 0.46x for the
  portable form - see below for why that number was depressed)
- The width-agnostic `Vector<T>` arithmetic form reaches 0.42-0.44x without any shuffle, but its code is 1.7x
  larger (321 vs 190 B)
- The element count is 1,021, deliberately not a multiple of the vector width, so the scalar tail is exercised

## `Vector128.Shuffle` vs `Ssse3.Shuffle`: the x86-64-v3 gap was placement, and this run proves it

| | x86-64-v3 | x86-64-v4 run 1 | x86-64-v4 run 2 |
|---|---:|---:|---:|
| `Vector128.Shuffle` (portable) | 121.53 ns | 62.51 ns | 64.69 ns |
| `Ssse3.Shuffle` (raw ISA) | 64.35 ns | 61.06 ns | 62.21 ns |
| Gap | **1.76x, CIs disjoint** | 1.02x, CIs disjoint | **1.04x, CIs overlap** |

The two forms compile to a **byte-identical instruction stream** (59 instructions / 190 B, the same `vpshufb`)
on both machines. On x86-64-v3 the portable form's hot loop straddled a 64-byte instruction-fetch boundary
(`9FBC`-`9FD8` across `9FC0`) while the ISA form's fitted inside one (`9F5C`-`9F78`), and duplicate methods
reproduced each original's address and time. **On this machine the portable form lands well and the gap
collapses to 1-4%, which run 2 cannot even resolve** - the 121.53 ns figure was the anomaly, not the 64.35 ns
one. Nothing about the API changed.

## What to weigh

1. **Default to the portable `Vector128.Shuffle`.** Two machines and three runs now agree there is no API-level
   difference against the raw ISA intrinsic, and the portable form carries the CPU-support fallback for free
2. **A gap this large between byte-identical code is a placement finding, not a result.** Before quoting one,
   re-measure in a second process and check the loop addresses in the DisassemblyDiagnoser output
   (`printInstructionAddresses`); see pitfall 10 in the methodology
3. **Reach for a shuffle only when no BCL API exists and `Vector<T>` cannot express the permutation.** The
   arithmetic form here is within 1.5x of the shuffle without needing fixed-width intrinsics at all, at the cost
   of 1.7x the code
4. **Keep the tail and the unsupported-CPU fallback exercised.** The element count is deliberately not a
   multiple of the vector width so every run walks the scalar tail

## x86-64-v4 run 1

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| ScalarReverse           | 213.05 ns | 0.474 ns | 0.710 ns | 211.92 ns | 214.38 ns | 214.08 ns |  1.00 |     145 B |         - |          NA |
| Vector128ShuffleReverse |  62.51 ns | 0.476 ns | 0.713 ns |  61.50 ns |  63.89 ns |  63.49 ns |  0.29 |     190 B |         - |          NA |
| Ssse3ShuffleReverse     |  61.06 ns | 0.512 ns | 0.751 ns |  60.07 ns |  62.54 ns |  62.01 ns |  0.29 |     190 B |         - |          NA |
| VectorArithmeticReverse |  93.88 ns | 0.716 ns | 1.072 ns |  92.03 ns |  95.54 ns |  95.10 ns |  0.44 |     321 B |         - |          NA |

## x86-64-v4 run 2

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                  | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| ScalarReverse           | 222.33 ns | 4.428 ns | 6.208 ns | 216.91 ns | 243.98 ns | 229.16 ns |  1.00 |    0.04 |     145 B |         - |          NA |
| Vector128ShuffleReverse |  64.69 ns | 2.912 ns | 4.269 ns |  61.43 ns |  78.04 ns |  70.87 ns |  0.29 |    0.02 |     190 B |         - |          NA |
| Ssse3ShuffleReverse     |  62.21 ns | 1.365 ns | 1.822 ns |  60.20 ns |  67.04 ns |  64.34 ns |  0.28 |    0.01 |     190 B |         - |          NA |
| VectorArithmeticReverse |  93.41 ns | 0.696 ns | 0.998 ns |  91.81 ns |  95.61 ns |  94.76 ns |  0.42 |    0.01 |     321 B |         - |          NA |
