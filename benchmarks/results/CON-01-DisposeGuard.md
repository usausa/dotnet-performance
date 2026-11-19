# CON-01: Interlocked one-shot guard (dispose / init)

## net11 / net10 re-verification (2026-11-18, HX 370)

- Verdict: unchanged - Interlocked 3.91-4.00 ns against `Lock` 8.72 ns (2.2x) on net11, identical to net10

### DisposeGuardBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| PlainBool | 0.1935 ns | 0.2008 ns | 1.04 | - → - | 26 B → 26 B |
| VolatileBool | 0.1879 ns | 0.1887 ns | 1.00 | - → - | 26 B → 26 B |
| LockGuard | 8.8589 ns | 8.7209 ns | 0.98 | - → - | 3,676 B → 4,345 B |
| InterlockedCas | 3.9870 ns | 3.9973 ns | 1.00 | - → - | 56 B → 56 B |
| InterlockedExchange | 3.9082 ns | 3.9104 ns | 1.00 | - → - | 33 B → 33 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.45 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| PlainBool           | .NET 10.0           | .NET 10.0 | 0.1935 ns | 0.0068 ns | 0.0102 ns | 0.1812 ns | 0.2070 ns | 0.2052 ns |  1.00 |    0.00 |      26 B |         - |          NA |
| VolatileBool        | .NET 10.0           | .NET 10.0 | 0.1879 ns | 0.0027 ns | 0.0040 ns | 0.1807 ns | 0.1969 ns | 0.1922 ns |  0.97 |    0.05 |      26 B |         - |          NA |
| LockGuard           | .NET 10.0           | .NET 10.0 | 8.8589 ns | 0.0142 ns | 0.0212 ns | 8.8308 ns | 8.9046 ns | 8.8841 ns | 45.90 |    2.37 |   3,676 B |         - |          NA |
| InterlockedCas      | .NET 10.0           | .NET 10.0 | 3.9870 ns | 0.0037 ns | 0.0053 ns | 3.9779 ns | 4.0011 ns | 3.9932 ns | 20.66 |    1.07 |      56 B |         - |          NA |
| InterlockedExchange | .NET 10.0           | .NET 10.0 | 3.9082 ns | 0.0034 ns | 0.0049 ns | 3.9017 ns | 3.9218 ns | 3.9151 ns | 20.25 |    1.05 |      33 B |         - |          NA |
|                     |                     |           |           |           |           |           |           |           |       |         |           |           |             |
| PlainBool           | MediumRun-.NET 11.0 | .NET 11.0 | 0.2008 ns | 0.0093 ns | 0.0139 ns | 0.1825 ns | 0.2177 ns | 0.2156 ns |  1.00 |    0.00 |      26 B |         - |          NA |
| VolatileBool        | MediumRun-.NET 11.0 | .NET 11.0 | 0.1887 ns | 0.0022 ns | 0.0034 ns | 0.1829 ns | 0.1968 ns | 0.1931 ns |  0.94 |    0.07 |      26 B |         - |          NA |
| LockGuard           | MediumRun-.NET 11.0 | .NET 11.0 | 8.7209 ns | 0.0499 ns | 0.0732 ns | 8.6352 ns | 8.8788 ns | 8.8212 ns | 43.64 |    3.00 |   4,345 B |         - |          NA |
| InterlockedCas      | MediumRun-.NET 11.0 | .NET 11.0 | 3.9973 ns | 0.0109 ns | 0.0160 ns | 3.9831 ns | 4.0427 ns | 4.0158 ns | 20.00 |    1.37 |      56 B |         - |          NA |
| InterlockedExchange | MediumRun-.NET 11.0 | .NET 11.0 | 3.9104 ns | 0.0034 ns | 0.0050 ns | 3.9028 ns | 3.9202 ns | 3.9169 ns | 19.57 |    1.34 |      33 B |         - |          NA |

## Earlier record

- Verdict: adopted
- Thread-safe exactly-once guards: Interlocked 3.90-3.98 ns vs lock (System.Threading.Lock) 8.85 ns (2.2-2.3x), code 33-56 B vs 2,612 B
- Plain/volatile bool 0.18 ns is fastest but not thread-safe - sufficient for single-threaded types

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error     | StdDev    | Min       | Max       | P90       | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|----------:|----------:|------------:|
| PlainBool           | 0.1770 ns | 0.0016 ns | 0.0024 ns | 0.1693 ns | 0.1813 ns | 0.1796 ns |  1.00 |    0.02 |      26 B |         - |          NA |
| VolatileBool        | 0.1804 ns | 0.0034 ns | 0.0050 ns | 0.1757 ns | 0.1964 ns | 0.1860 ns |  1.02 |    0.03 |      26 B |         - |          NA |
| LockGuard           | 8.8513 ns | 0.0359 ns | 0.0492 ns | 8.8167 ns | 9.0569 ns | 8.8808 ns | 50.02 |    0.73 |   2,612 B |         - |          NA |
| InterlockedCas      | 3.9773 ns | 0.0031 ns | 0.0046 ns | 3.9705 ns | 3.9875 ns | 3.9828 ns | 22.48 |    0.31 |      56 B |         - |          NA |
| InterlockedExchange | 3.9035 ns | 0.0042 ns | 0.0061 ns | 3.8929 ns | 3.9152 ns | 3.9126 ns | 22.06 |    0.30 |      33 B |         - |          NA |
