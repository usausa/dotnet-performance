# LAB-ScopedRef: scoped and [UnscopedRef] (study queue 7-1)

- Verdict: no difference (scoped) / rejected as a performance pattern (UnscopedRef accessor) -> R-20
- scoped: caller and callee disassembly are identical instruction for instruction. The only difference is the
  symbol name in the call. Caller 89 B for both span variants, 50 B for both ref variants; callee 35 B for
  SumSpan / SumScopedSpan and 38 B for StepRef / StepScopedRef
- scoped is therefore a pure compile-time escape contract with zero codegen cost. It is neither something to
  add for speed nor something to avoid for speed
- [UnscopedRef] ref-returning accessor: 1.07x against a get/set pair, CIs overlap. The disassembly differs
  (the ref form folds into a single `add [r8],r10` read-modify-write but pays an extra `lea`), yet the
  instruction count is the same 7 and the code is larger: 85 -> 88 B. No axis improves
- Both remain worth documenting as language features: without [UnscopedRef] the accessor does not compile
  at all (CS8170), and scoped is the only way to relax the escape constraint a ref struct imposes on callers

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean       | Error     | StdDev    | Median     | Min        | Max        | P90        | Ratio  | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-----------:|-------:|--------:|----------:|----------:|------------:|
| GetSetPair          |  0.5013 ns | 0.0164 ns | 0.0246 ns |  0.4892 ns |  0.4751 ns |  0.5552 ns |  0.5346 ns |   1.00 |    0.07 |      85 B |         - |          NA |
| UnscopedRefAccessor |  0.5349 ns | 0.0121 ns | 0.0177 ns |  0.5297 ns |  0.5127 ns |  0.5709 ns |  0.5670 ns |   1.07 |    0.06 |      88 B |         - |          NA |
| PlainSpanParameter  | 69.2197 ns | 1.9975 ns | 2.9279 ns | 67.9761 ns | 65.7576 ns | 73.8288 ns | 72.9021 ns | 138.40 |    8.66 |     124 B |         - |          NA |
| ScopedSpanParameter | 70.0049 ns | 1.5503 ns | 2.3204 ns | 69.8936 ns | 66.2871 ns | 73.6494 ns | 72.8535 ns | 139.97 |    7.97 |     124 B |         - |          NA |
| PlainRefParameter   |  2.2759 ns | 0.0572 ns | 0.0856 ns |  2.2302 ns |  2.1903 ns |  2.4184 ns |  2.4052 ns |   4.55 |    0.27 |      88 B |         - |          NA |
| ScopedRefParameter  |  2.2964 ns | 0.0701 ns | 0.1049 ns |  2.2405 ns |  2.1823 ns |  2.5070 ns |  2.4501 ns |   4.59 |    0.30 |      88 B |         - |          NA |

