# SEQ-05: Reading an unknown-length stream (256 KB)

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged. Pooled segments 0.32x / 0.31x and PipeReader 0.32x / 0.33x of MemoryStream + ToArray on net10 / net11; allocation identical
- PipeReader is 1.06x slower on net11 (55.1 → 58.4 μs)
- Re-run (2026-11-19): reproduced at 1.06x (55.0 → 58.6 μs)

### StreamSequenceBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| MemoryStreamToArray | 171.33 μs | 174.81 μs | 1.02 | 524680 B → 524680 B | 4,724 B → 5,555 B |
| PooledSegments | 54.47 μs | 54.34 μs | 1.00 | 64 B → 64 B | 9,624 B → 8,022 B |
| PipeReaderSequence | 55.05 μs | 58.41 μs | 1.06 | 504 B → 504 B | 16,455 B → 16,279 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.59 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | Code Size | Gen0     | Gen1     | Gen2     | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|----------:|---------:|---------:|---------:|----------:|------------:|
| MemoryStreamToArray | .NET 10.0           | .NET 10.0 | 171.33 μs | 1.159 μs | 1.662 μs | 167.91 μs | 173.93 μs | 173.31 μs |  1.00 |   4,724 B | 166.5039 | 166.5039 | 166.5039 |  524680 B |       1.000 |
| PooledSegments      | .NET 10.0           | .NET 10.0 |  54.47 μs | 0.137 μs | 0.182 μs |  54.16 μs |  54.88 μs |  54.67 μs |  0.32 |   9,624 B |        - |        - |        - |      64 B |       0.000 |
| PipeReaderSequence  | .NET 10.0           | .NET 10.0 |  55.05 μs | 0.167 μs | 0.245 μs |  54.65 μs |  55.62 μs |  55.34 μs |  0.32 |  16,455 B |        - |        - |        - |     504 B |       0.001 |
|                     |                     |           |           |          |          |           |           |           |       |           |          |          |          |           |             |
| MemoryStreamToArray | MediumRun-.NET 11.0 | .NET 11.0 | 174.81 μs | 1.021 μs | 1.527 μs | 172.19 μs | 177.94 μs | 176.48 μs |  1.00 |   5,555 B | 166.5039 | 166.5039 | 166.5039 |  524680 B |       1.000 |
| PooledSegments      | MediumRun-.NET 11.0 | .NET 11.0 |  54.34 μs | 0.086 μs | 0.125 μs |  54.06 μs |  54.53 μs |  54.48 μs |  0.31 |   8,022 B |        - |        - |        - |      64 B |       0.000 |
| PipeReaderSequence  | MediumRun-.NET 11.0 | .NET 11.0 |  58.41 μs | 0.163 μs | 0.239 μs |  57.92 μs |  58.80 μs |  58.70 μs |  0.33 |  16,279 B |        - |        - |        - |     504 B |       0.001 |

## Earlier record

- Use case: pulling an unknown-length payload out of a stream - a request body, a blob read, a file of unknown size - where the naive shape is "accumulate into a `MemoryStream`, then `ToArray`". This is the faster way to do that same copy
- Verdict: the finding is adopted, the custom builder is not - **`PipeReader` (ASY-03) already delivers essentially all of it**
- `MemoryStream` accumulate + `ToArray` 167.3 μs / **524,520 B** -> pooled segments 56.2 μs (**0.34x**) / 64 B -> `PipeReader` 57.4 μs (**0.34x**) / 504 B
- **All three copy the payload once** - the source comment's "no copy" claim is wrong. `PooledSegments` copies via `source.Read(chunk, ...)` into the rented chunk; `PipeReader` copies into its own pooled buffers. What the two save is the baseline's *extra* copies: the grow-copy chain as `MemoryStream` doubles its buffer, plus the final full `ToArray` copy
- Because the source here is a `MemoryStream` (already an array), that one remaining copy is an artifact of the harness. Against a real `FileStream`/`NetworkStream` it is unavoidable in every variant, so the comparison holds
- Every buffer past 85 KB lands on the LOH - Gen0/Gen1/Gen2 all show 166.5 collections, i.e. the large arrays are surviving to Gen2
- Custom `ReusableSequenceBuilder` vs `PipeReader`: 56.24 vs 57.40 μs is **0.98x** with non-overlapping CIs, so it is a real difference - and a 2% one, against 88 lines of hand-rolled segment pooling. Allocation 64 vs 504 B is noise next to the 524 KB it replaces
- Reach for the hand-rolled builder only when the `ReadOnlySequence` must outlive the read loop, or when the source is not a `Stream`. Otherwise `PipeReader.Create(stream)` is the answer and it is already documented
- Code size: 3,258 B (baseline) vs 7,157 B (builder) vs 8,485 B (PipeReader)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean      | Error    | StdDev   | Min       | Max       | P90       | Ratio | RatioSD | Gen0     | Code Size | Gen1     | Gen2     | Allocated | Alloc Ratio |
|-------------------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|---------:|----------:|---------:|---------:|----------:|------------:|
| MemoryStreamToArray | 167.29 μs | 5.480 μs | 7.859 μs | 156.15 μs | 184.93 μs | 180.99 μs |  1.00 |    0.06 | 166.5039 |   3,258 B | 166.5039 | 166.5039 |  524520 B |       1.000 |
| PooledSegments      |  56.24 μs | 0.335 μs | 0.491 μs |  55.47 μs |  57.33 μs |  56.84 μs |  0.34 |    0.02 |        - |   7,157 B |        - |        - |      64 B |       0.000 |
| PipeReaderSequence  |  57.40 μs | 0.304 μs | 0.446 μs |  56.68 μs |  58.36 μs |  58.13 μs |  0.34 |    0.02 |        - |   8,485 B |        - |        - |     504 B |       0.001 |
