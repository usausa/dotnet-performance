# LAB-UnboxInPlace: Unsafe.Unbox for in-place update of an existing box (study queue 7-12)

- Verdict: adopted (recorded as an STK-05 extension). Re-measured on x86-64-v4 (Zen 5)
- **0.17x** against unbox / mutate the copy / rebox (171.2 vs 1,024.9 ns), code 251 vs 582 B, and allocation
  goes from **8,192 B to zero**. All three axes improve (x86-64-v3: 0.18x, byte-identical code sizes)
- **The ratio barely moved between two very different cores (0.18x -> 0.17x) because what the pattern removes is
  an allocation, not instructions.** The rebox form triggers 0.98 Gen0 and 0.03 Gen1 collections per 1,000
  operations here; that cost is set by the allocator and the GC, not by the pipeline, so it does not shrink on
  a wider machine the way instruction-count wins do
- Verify also asserts that the box instances are not replaced (ReferenceEquals), which is the behavioural
  difference from the rebox form, not just a performance one
- Scope: STK-05 is about not boxing in the first place. This covers the case where the box already exists and
  is handed to you (an object field, a dictionary of object, an interop boundary)
- `Unsafe.Unbox` does check the type and throws `InvalidCastException` on mismatch, but passing a non-boxed
  reference is undefined. Restrict it to boxes you created

## What to weigh

1. **Decide the aliasing question before the performance one.** In-place update preserves box identity, so every
   other reference to that box observes the change. That is either exactly the point (a shared mutable slot) or
   a bug that the rebox form was accidentally preventing. No ratio justifies getting this backwards
2. **Take it when the box already exists and you own it.** Three axes improve on both machines and the dominant
   one - 8,192 B of allocation per operation becoming zero - is machine independent
3. **Do not read this as a reason to box.** STK-05's rule is unchanged: avoid the box. This is the repair for
   boxes that an API hands you, not a licence to create them
4. **Keep it to boxes you created.** The type check protects against a mismatched `T`, but not against a
   reference that was never a box - that path is undefined behaviour, not an exception

## x86-64-v4

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method             | Mean       | Error    | StdDev   | Min      | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------- |-----------:|---------:|---------:|---------:|-----------:|-----------:|------:|--------:|----------:|-------:|-------:|----------:|------------:|
| UnboxCopyRebox     | 1,024.9 ns | 21.80 ns | 32.63 ns | 961.0 ns | 1,072.7 ns | 1,061.1 ns |  1.00 |    0.04 |     582 B | 0.9785 | 0.0305 |    8192 B |        1.00 |
| UnsafeUnboxInPlace |   171.2 ns |  0.89 ns |  1.34 ns | 169.2 ns |   173.9 ns |   173.7 ns |  0.17 |    0.01 |     251 B |      - |      - |         - |        0.00 |
