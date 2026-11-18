# GEN-03: Reflection invocation without the argument array (JIT and NativeAOT)

- Verdict: **adopted** on both runtimes. Prefer a typed `CreateDelegate<T>` where the signature is static, and
  `ConstructorInvoker` / `MethodInvoker` where it is not
- First measurement in this repository, on the reference environment (HX 370, x86-64-v4, .NET 11.0.0), JIT and
  NativeAOT 11.0
- Constructor, one argument - JIT: 9.43 ns / 56 B → invoker 5.25 ns / 24 B (0.56x) → typed delegate 3.21 ns /
  24 B (0.34x); NativeAOT: 17.99 ns / 56 B → 15.41 ns / 24 B (0.86x) → 3.58 ns / 24 B (0.20x)
- Setter - JIT: 6.40 ns / **0 B** → 4.91 ns (0.77x) → 2.03 ns (0.32x); NativeAOT: 20.40 ns / **32 B** →
  13.15 ns / 0 B (0.65x) → 3.71 ns / 0 B (0.18x)
- `SetValue` allocates only under NativeAOT, as expected
- The 24 B left on every constructor row is the constructed `AccessorTarget` itself: the benchmark boxes the
  argument once up front, so no box is measured
- With BenchmarkDotNet 0.16.0-preview.2 the `Ratio` of the NativeAOT rows is already against the NativeAOT
  baseline row (checked by hand: 15.405 / 17.986 = 0.857)

## ReflectionConstructBenchmark, JIT

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.58 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                       | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|-------:|----------:|------------:|
| InvokeWithArgumentArray      | 9.429 ns | 0.0803 ns | 0.1151 ns | 9.185 ns | 9.707 ns | 9.562 ns |  1.00 |    0.00 |   8,457 B | 0.0067 |      56 B |        1.00 |
| InvokeWithConstructorInvoker | 5.254 ns | 0.1585 ns | 0.2170 ns | 5.001 ns | 5.839 ns | 5.478 ns |  0.56 |    0.02 |   1,411 B | 0.0029 |      24 B |        0.43 |
| InvokeWithTypedDelegate      | 3.207 ns | 0.0320 ns | 0.0448 ns | 3.158 ns | 3.380 ns | 3.237 ns |  0.34 |    0.01 |      35 B | 0.0029 |      24 B |        0.43 |

## ReflectionAccessorBenchmark, JIT

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.58 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 11.0  Runtime=.NET 11.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method               | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|--------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| SetWithSetValue      | 6.401 ns | 0.0415 ns | 0.0568 ns | 6.320 ns | 6.567 ns | 6.466 ns |  1.00 |  25,263 B |         - |          NA |
| SetWithMethodInvoker | 4.908 ns | 0.0224 ns | 0.0315 ns | 4.858 ns | 4.993 ns | 4.945 ns |  0.77 |   2,023 B |         - |          NA |
| SetWithTypedDelegate | 2.026 ns | 0.0159 ns | 0.0223 ns | 2.001 ns | 2.090 ns | 2.052 ns |  0.32 |      51 B |         - |          NA |

## ReflectionConstructBenchmark, JIT and NativeAOT (AotHarness)

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.45 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  NativeAOT 11.0      : .NET 11.0.0, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                       | Job                 | Runtime        | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------------- |--------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| InvokeWithArgumentArray      | MediumRun-.NET 11.0 | .NET 11.0      |  9.363 ns | 0.0842 ns | 0.1124 ns |  9.386 ns |  9.163 ns |  9.530 ns |  9.500 ns |  1.00 |    0.00 | 0.0067 |      56 B |        1.00 |
| InvokeWithConstructorInvoker | MediumRun-.NET 11.0 | .NET 11.0      |  5.302 ns | 0.1228 ns | 0.1681 ns |  5.201 ns |  5.099 ns |  5.604 ns |  5.499 ns |  0.57 |    0.02 | 0.0029 |      24 B |        0.43 |
| InvokeWithTypedDelegate      | MediumRun-.NET 11.0 | .NET 11.0      |  3.198 ns | 0.0126 ns | 0.0168 ns |  3.200 ns |  3.167 ns |  3.236 ns |  3.220 ns |  0.34 |    0.00 | 0.0029 |      24 B |        0.43 |
|                              |                     |                |           |           |           |           |           |           |           |       |         |        |           |             |
| InvokeWithArgumentArray      | NativeAOT 11.0      | NativeAOT 11.0 | 17.986 ns | 0.3859 ns | 0.5152 ns | 17.791 ns | 17.581 ns | 19.921 ns | 18.513 ns |  1.00 |    0.00 | 0.0067 |      56 B |        1.00 |
| InvokeWithConstructorInvoker | NativeAOT 11.0      | NativeAOT 11.0 | 15.405 ns | 0.0894 ns | 0.1193 ns | 15.390 ns | 15.217 ns | 15.738 ns | 15.513 ns |  0.86 |    0.02 | 0.0029 |      24 B |        0.43 |
| InvokeWithTypedDelegate      | NativeAOT 11.0      | NativeAOT 11.0 |  3.583 ns | 0.0164 ns | 0.0219 ns |  3.582 ns |  3.535 ns |  3.615 ns |  3.609 ns |  0.20 |    0.01 | 0.0029 |      24 B |        0.43 |

## ReflectionAccessorBenchmark, JIT and NativeAOT (AotHarness)

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.51 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  NativeAOT 11.0      : .NET 11.0.0, X64 NativeAOT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method               | Job                 | Runtime        | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-------------------- |--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|-------:|----------:|------------:|
| SetWithSetValue      | MediumRun-.NET 11.0 | .NET 11.0      |  6.377 ns | 0.0313 ns | 0.0439 ns |  6.309 ns |  6.456 ns |  6.440 ns |  1.00 |      - |         - |          NA |
| SetWithMethodInvoker | MediumRun-.NET 11.0 | .NET 11.0      |  4.938 ns | 0.0493 ns | 0.0658 ns |  4.871 ns |  5.208 ns |  4.979 ns |  0.77 |      - |         - |          NA |
| SetWithTypedDelegate | MediumRun-.NET 11.0 | .NET 11.0      |  2.017 ns | 0.0104 ns | 0.0138 ns |  1.996 ns |  2.052 ns |  2.033 ns |  0.32 |      - |         - |          NA |
|                      |                     |                |           |           |           |           |           |           |       |        |           |             |
| SetWithSetValue      | NativeAOT 11.0      | NativeAOT 11.0 | 20.397 ns | 0.2656 ns | 0.3809 ns | 20.074 ns | 21.700 ns | 20.787 ns |  1.00 | 0.0038 |      32 B |        1.00 |
| SetWithMethodInvoker | NativeAOT 11.0      | NativeAOT 11.0 | 13.152 ns | 0.1273 ns | 0.1784 ns | 12.944 ns | 13.581 ns | 13.482 ns |  0.65 |      - |         - |        0.00 |
| SetWithTypedDelegate | NativeAOT 11.0      | NativeAOT 11.0 |  3.714 ns | 0.0427 ns | 0.0612 ns |  3.640 ns |  3.867 ns |  3.814 ns |  0.18 |      - |         - |        0.00 |

## Basis for the verdict

1. **Time:** every step has disjoint intervals on both runtimes. The typed delegate is the clear winner under
   NativeAOT (0.18-0.20x), where reflection itself is 2-3x slower than under JIT
2. **Allocation:** the argument array (32 B for one argument) disappears with both replacements. The setter
   only shows it under NativeAOT; under JIT `SetValue` already allocates nothing
3. **Generated code (JIT):** 8,457 B / 25,263 B for the reflection calls against 1,411 B / 2,023 B for the
   invokers and 35 B / 51 B for the typed delegates. NativeAOT has no disassembly diagnoser
4. **Invoker versus delegate:** under NativeAOT the invoker recovers less than under JIT (0.86x against
   0.56x on the constructor), so the invoker is the fallback for dynamic signatures, not the default
5. **Against the Zen 3 reference (NativeAOT):** same direction, smaller invoker gain - 0.86x against 0.64x on
   the constructor and 0.65x against 0.51x on the setter. The sixteen-argument constructor and the getter are
   not part of this benchmark
