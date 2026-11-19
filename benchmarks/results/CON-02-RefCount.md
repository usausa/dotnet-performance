# CON-02: Retain/release - optimistic increment and run-length batching

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted. Uncontended 1.00x on both generations (7.62 ns); 4 threads 0.57x on both (17.19 vs 30.16 ns on net11); run-length batching 0.06x on both

### RefCountUncontendedBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| CasLoop | 7.614 ns | 7.618 ns | 1.00 | - → - | 115 B → 115 B |
| Optimistic | 7.630 ns | 7.628 ns | 1.00 | - → - | 120 B → 120 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.36 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method     | Job                 | Runtime   | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|----------- |-------------------- |---------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| CasLoop    | .NET 10.0           | .NET 10.0 | 7.614 ns | 0.0045 ns | 0.0065 ns | 7.604 ns | 7.627 ns | 7.624 ns |  1.00 |     115 B |         - |          NA |
| Optimistic | .NET 10.0           | .NET 10.0 | 7.630 ns | 0.0080 ns | 0.0117 ns | 7.618 ns | 7.662 ns | 7.645 ns |  1.00 |     120 B |         - |          NA |
|            |                     |           |          |           |           |          |          |          |       |           |           |             |
| CasLoop    | MediumRun-.NET 11.0 | .NET 11.0 | 7.618 ns | 0.0084 ns | 0.0117 ns | 7.603 ns | 7.648 ns | 7.632 ns |  1.00 |     115 B |         - |          NA |
| Optimistic | MediumRun-.NET 11.0 | .NET 11.0 | 7.628 ns | 0.0059 ns | 0.0087 ns | 7.617 ns | 7.647 ns | 7.638 ns |  1.00 |     120 B |         - |          NA |

### RefCountContendedBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| CasLoop | 29.27 ns | 30.16 ns | 1.03 | - → - | 8,567 B → 11,136 B |
| Optimistic | 16.61 ns | 17.19 ns | 1.03 | - → - | 8,526 B → 11,145 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.65 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method     | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------- |-------------------- |---------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| CasLoop    | .NET 10.0           | .NET 10.0 | 29.27 ns | 0.358 ns | 0.535 ns | 27.88 ns | 30.25 ns | 29.87 ns |  1.00 |    0.00 |   8,567 B |         - |          NA |
| Optimistic | .NET 10.0           | .NET 10.0 | 16.61 ns | 0.549 ns | 0.769 ns | 14.06 ns | 17.41 ns | 17.16 ns |  0.57 |    0.03 |   8,526 B |         - |          NA |
|            |                     |           |          |          |          |          |          |          |       |         |           |           |             |
| CasLoop    | MediumRun-.NET 11.0 | .NET 11.0 | 30.16 ns | 0.376 ns | 0.539 ns | 29.29 ns | 30.83 ns | 30.77 ns |  1.00 |    0.00 |  11,136 B |         - |          NA |
| Optimistic | MediumRun-.NET 11.0 | .NET 11.0 | 17.19 ns | 0.115 ns | 0.169 ns | 16.83 ns | 17.54 ns | 17.37 ns |  0.57 |    0.01 |  11,145 B |         - |          NA |

### RetainBatchingBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| RetainPerRow | 7.6799 ns | 7.7676 ns | 1.01 | - → - | 199 B → 199 B |
| RetainPerRun | 0.4306 ns | 0.4312 ns | 1.00 | - → - | 253 B → 253 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 70.61 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method       | Job                 | Runtime   | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|------------- |-------------------- |---------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| RetainPerRow | .NET 10.0           | .NET 10.0 | 7.6799 ns | 0.0398 ns | 0.0545 ns | 7.6880 ns | 7.5978 ns | 7.7817 ns | 7.7504 ns |  1.00 |     199 B |         - |          NA |
| RetainPerRun | .NET 10.0           | .NET 10.0 | 0.4306 ns | 0.0018 ns | 0.0026 ns | 0.4302 ns | 0.4272 ns | 0.4367 ns | 0.4336 ns |  0.06 |     253 B |         - |          NA |
|              |                     |           |           |           |           |           |           |           |           |       |           |           |             |
| RetainPerRow | MediumRun-.NET 11.0 | .NET 11.0 | 7.7676 ns | 0.1733 ns | 0.2540 ns | 7.6251 ns | 7.5786 ns | 8.3872 ns | 8.2191 ns |  1.00 |     199 B |         - |          NA |
| RetainPerRun | MediumRun-.NET 11.0 | .NET 11.0 | 0.4312 ns | 0.0011 ns | 0.0016 ns | 0.4311 ns | 0.4278 ns | 0.4337 ns | 0.4332 ns |  0.06 |     253 B |         - |          NA |

## Earlier record

- Use case: **page / buffer cache pinning** - a range scan pins the pages it is reading so they cannot be evicted, and the last release frees deterministically rather than waiting for the GC. The batching shape (128 rows spread over 4 pages) is exactly a DB range scan over an index or heap
- Verdict: adopted (the win is in contention and in not doing the interlocked op at all; uncontended it is a wash)
- Uncontended: CAS retry loop 7.720 ns vs optimistic increment 7.728 ns - **1.00x, CIs overlap**. One uncontended `Interlocked.CompareExchange` costs the same as one `Interlocked.Increment`, so the retry loop never actually retries and there is nothing to save
- 4 threads on one instance: 29.30 ns -> 16.60 ns (**0.57x**). This is where the candidate earns its place - the CAS loop re-reads and re-tries on every lost race, the fetch-add always makes progress in one op
- Run-length batching (128 rows over 4 pages): 7.68 ns -> 0.483 ns (**0.06x**, ~16x). Retaining once per page run instead of once per row removes 124 of 128 interlocked ops; nothing beats not issuing the instruction
- Ordering of the two findings matters: batch first, then pick the retain primitive. Batching is worth ~16x, the primitive swap is worth 1.75x and only under contention
- Cost of the optimistic form is correctness surface, not code (115 -> 120 B): death must be stamped with a bias large enough that a late increment cannot resurrect a dead entry

## RefCountUncontendedBenchmark

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method     | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | Code Size | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|----------:|----------:|------------:|
| CasLoop    | 7.720 ns | 0.0201 ns | 0.0281 ns | 7.665 ns | 7.760 ns | 7.756 ns |  1.00 |     115 B |         - |          NA |
| Optimistic | 7.728 ns | 0.0290 ns | 0.0407 ns | 7.663 ns | 7.805 ns | 7.788 ns |  1.00 |     120 B |         - |          NA |

## RefCountContendedBenchmark (4 threads, same instance)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method     | Mean     | Error    | StdDev   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| CasLoop    | 29.30 ns | 0.952 ns | 1.425 ns | 27.54 ns | 32.99 ns | 30.85 ns |  1.00 |    0.07 |   4,052 B |         - |          NA |
| Optimistic | 16.60 ns | 0.182 ns | 0.262 ns | 15.99 ns | 17.08 ns | 16.84 ns |  0.57 |    0.03 |   4,208 B |         - |          NA |

## RetainBatchingBenchmark (128 rows over 4 pages)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean      | Error     | StdDev    | Median    | Min       | Max       | P90       | Ratio | Code Size | Allocated | Alloc Ratio |
|------------- |----------:|----------:|----------:|----------:|----------:|----------:|----------:|------:|----------:|----------:|------------:|
| RetainPerRow | 7.6819 ns | 0.0146 ns | 0.0210 ns | 7.6824 ns | 7.6430 ns | 7.7258 ns | 7.7033 ns |  1.00 |     199 B |         - |          NA |
| RetainPerRun | 0.4831 ns | 0.0356 ns | 0.0521 ns | 0.5249 ns | 0.4219 ns | 0.5448 ns | 0.5361 ns |  0.06 |     264 B |         - |          NA |

BenchmarkDotNet flagged `RetainPerRun` as bimodal (mValue 3.87) - the Mean 0.483 / Median 0.525 gap at sub-nanosecond scale. The 16x gap against the baseline is far outside that spread, so the conclusion holds either way.
