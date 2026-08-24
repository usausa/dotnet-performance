# LAB-BitCast: Unsafe.BitCast vs Unsafe.As (study queue 7-7)

- Verdict: identical codegen -> adopt `Unsafe.BitCast` as the default on the safety axis. Re-measured twice on
  x86-64-v4 (Zen 5); the x86-64-v3 (Zen 3) run agreed
- **All five variants produce an identical instruction stream**: 20 instructions / 57 B for the three
  concrete-type forms (`Unsafe.As`, `Unsafe.BitCast`, `BitConverter.SingleToInt32Bits`) and 8 instructions /
  21 B for the two generic forms. Byte for byte the same figures on both machines
- In the generic case with `T = int` the reinterpretation disappears entirely; only a `movsxd` + `add` loop
  remains
- `BitConverter.SingleToInt32Bits` folds to the same code, so where a BCL API exists for the concrete type pair
  there is no reason to reach for `Unsafe` at all

## The 1-2% time spread is placement, and two runs prove it

| Form | x86-64-v4 run 1 | x86-64-v4 run 2 | x86-64-v3 |
|---|---:|---:|---:|
| `UnsafeAsReinterpret` | 222.9 ns (slowest of the three) | **230.5 ns (fastest)** | 241.5 ns (fastest) |
| `UnsafeBitCastReinterpret` | 220.6 ns | 234.4 ns | 243.9 ns |
| `BitConverterReinterpret` | 218.7 ns | 233.9 ns | 243.6 ns |

The instruction streams are identical, so nothing about the code under test can explain a ranking that reverses
between two processes on the same machine. This is the methodology's pitfall 10 (identical code, different
placement) reproduced deliberately.

**Note the scale of the drift.** Across the two processes the same method moved 222.9 -> 230.5 ns and its
neighbours 218.7 -> 233.9 ns: a **~7% between-process spread against a 1-2% within-run spread**. Any gap of a
few percent seen in a single process is inside that drift and cannot be attributed to the code under test until
a second process reproduces its sign.

## What to weigh

1. **Switch to `Unsafe.BitCast` by default.** The identical codegen is the argument *for* the migration, not an
   argument that it does not matter: `Unsafe.As<TFrom, TTo>` silently accepts a size mismatch and corrupts
   memory, `BitCast` refuses it. The safer form is free
2. **Keep `Unsafe.As<TFrom, TTo>` only for deliberately size-changing reinterpretation** - reading a prefix of a
   larger type, or viewing a value as something wider. That is the one thing `BitCast` cannot express, and it is
   exactly the case that needs the reviewer's attention
3. **Prefer a named BCL conversion when the concrete pair has one.** Same code, and the intent is readable
   without a comment
4. **Do not chase this benchmark's 1-2%.** Re-run in a fresh process before attributing a small gap to an API
   choice; here the sign flipped

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
| Method                   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|------------------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| UnsafeAsReinterpret      | 222.9 ns | 0.44 ns | 0.62 ns | 221.9 ns | 224.0 ns | 223.7 ns |  1.00 |      57 B |         - |          NA |
| UnsafeBitCastReinterpret | 220.6 ns | 1.81 ns | 2.54 ns | 217.0 ns | 224.7 ns | 223.4 ns |  0.99 |      57 B |         - |          NA |
| BitConverterReinterpret  | 218.7 ns | 0.65 ns | 0.97 ns | 217.3 ns | 220.7 ns | 220.3 ns |  0.98 |      57 B |         - |          NA |
| GenericUnsafeAs          | 219.6 ns | 1.01 ns | 1.48 ns | 218.1 ns | 223.5 ns | 221.6 ns |  0.98 |      21 B |         - |          NA |
| GenericBitCast           | 219.2 ns | 0.70 ns | 0.96 ns | 218.2 ns | 222.2 ns | 220.6 ns |  0.98 |      21 B |         - |          NA |

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
| Method                   | Mean     | Error   | StdDev  | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------------- |---------:|--------:|--------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| UnsafeAsReinterpret      | 230.5 ns | 1.83 ns | 2.68 ns | 225.6 ns | 236.9 ns | 233.3 ns |  1.00 |    0.02 |      57 B |         - |          NA |
| UnsafeBitCastReinterpret | 234.4 ns | 3.24 ns | 4.84 ns | 226.8 ns | 244.6 ns | 240.0 ns |  1.02 |    0.02 |      57 B |         - |          NA |
| BitConverterReinterpret  | 233.9 ns | 6.90 ns | 9.89 ns | 222.4 ns | 254.9 ns | 250.9 ns |  1.01 |    0.04 |      57 B |         - |          NA |
| GenericUnsafeAs          | 224.1 ns | 1.52 ns | 2.27 ns | 220.9 ns | 230.4 ns | 226.1 ns |  0.97 |    0.01 |      21 B |         - |          NA |
| GenericBitCast           | 225.3 ns | 1.97 ns | 2.77 ns | 222.3 ns | 235.5 ns | 227.7 ns |  0.98 |    0.02 |      21 B |         - |          NA |
