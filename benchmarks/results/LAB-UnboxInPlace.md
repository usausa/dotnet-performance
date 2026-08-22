# LAB-UnboxInPlace: Unsafe.Unbox for in-place update of an existing box (study queue 7-12)

- Verdict: adopted (recorded as an STK-05 extension)
- 0.18x against unbox / mutate the copy / rebox (286.5 vs 1,601.5 ns), code 251 vs 582 B, and allocation goes
  from 8,192 B to zero. All three axes improve
- Verify also asserts that the box instances are not replaced (ReferenceEquals), which is the behavioural
  difference from the rebox form, not just a performance one
- Scope: STK-05 is about not boxing in the first place. This covers the case where the box already exists and
  is handed to you (an object field, a dictionary of object, an interop boundary)
- Unsafe.Unbox does check the type and throws InvalidCastException on mismatch, but passing a non-boxed
  reference is undefined. Restrict it to boxes you created
```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| UnboxCopyRebox     | 1,601.5 ns | 45.89 ns | 67.27 ns | 1,505.3 ns | 1,723.8 ns | 1,689.3 ns |  1.00 |    0.06 |     582 B | 0.4883 | 0.0134 |    8192 B |        1.00 |
| UnsafeUnboxInPlace |   286.5 ns |  9.10 ns | 13.63 ns |   268.0 ns |   314.9 ns |   306.8 ns |  0.18 |    0.01 |     251 B |      - |      - |         - |        0.00 |

