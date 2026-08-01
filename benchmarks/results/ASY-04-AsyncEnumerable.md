# ASY-04: IAsyncEnumerable のコスト

判定: 収録(同期完了データの await foreach は 11.6 倍/要素。真に非同期な生成のみに使う)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8894/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]              : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method       | Mean       | Error     | StdDev    | Min        | Max       | P90       | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|------------- |-----------:|----------:|----------:|-----------:|----------:|----------:|------:|--------:|-------:|----------:|----------:|------------:|
| SyncForeach  |  0.9566 ns | 0.0777 ns | 0.1164 ns |  0.8130 ns |  1.154 ns |  1.117 ns |  1.01 |    0.17 | 0.0000 |     488 B |         - |          NA |
| AsyncForeach | 10.9726 ns | 0.6274 ns | 0.9197 ns | 10.0267 ns | 13.478 ns | 12.084 ns | 11.63 |    1.65 |      - |   3,896 B |         - |          NA |
