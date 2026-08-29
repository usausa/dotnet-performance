# TXT-08: ContainsAny vs IndexOfAny(...) >= 0

- Verdict: adopted (use ContainsAny whenever the position is not needed)
- Early match (index 4 of 256): 0.73x, confidence intervals non-overlapping
- Late match / no match: within noise (1.08x / 0.90x), never a meaningful regression
- Code size: about 30% smaller in every case (566/547/551 B -> 399/382/394 B)
- Rationale: ContainsAny does not have to extract the lane position out of the matching vector,
  so the saving grows the earlier the data matches

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean     | Error     | StdDev    | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|------------------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| IndexOfAny_Early   | 1.824 ns | 0.0980 ns | 0.1406 ns | 1.611 ns | 2.216 ns | 1.990 ns |  1.01 |    0.11 |     566 B |         - |          NA |
| ContainsAny_Early  | 1.328 ns | 0.1366 ns | 0.2002 ns | 1.110 ns | 2.033 ns | 1.550 ns |  0.73 |    0.12 |     399 B |         - |          NA |
| IndexOfAny_Late    | 7.018 ns | 0.2922 ns | 0.4096 ns | 6.194 ns | 7.822 ns | 7.576 ns |  3.87 |    0.36 |     547 B |         - |          NA |
| ContainsAny_Late   | 7.563 ns | 0.7009 ns | 1.0490 ns | 6.201 ns | 9.696 ns | 9.175 ns |  4.17 |    0.65 |     382 B |         - |          NA |
| IndexOfAny_Absent  | 6.438 ns | 0.7378 ns | 1.0815 ns | 5.131 ns | 8.326 ns | 8.056 ns |  3.55 |    0.64 |     551 B |         - |          NA |
| ContainsAny_Absent | 5.826 ns | 0.1647 ns | 0.2465 ns | 5.512 ns | 6.337 ns | 6.159 ns |  3.21 |    0.27 |     394 B |         - |          NA |
