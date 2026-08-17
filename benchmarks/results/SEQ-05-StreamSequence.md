# SEQ-05: Reading an unknown-length stream (256 KB)

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
