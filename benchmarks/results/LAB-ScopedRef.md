# LAB-ScopedRef: scoped and [UnscopedRef] (study queue 7-1)

- Verdict: no difference (scoped) / rejected as a performance pattern (UnscopedRef accessor) -> R-20
- Re-measured on x86-64-v4 (Zen 5, .NET 10.0.11). The earlier x86-64-v3 (Zen 3) run reached the same verdict
- scoped: caller and callee disassembly are identical instruction for instruction. The only difference is the
  symbol name in the call. Caller 89 B for both span variants, 50 B for both ref variants; callee 35 B for
  SumSpan / SumScopedSpan and 38 B for StepRef / StepScopedRef - **byte for byte the same figures the
  x86-64-v3 run produced**, so the contract is ISA independent
- [UnscopedRef] ref-returning accessor: 1.02x against a get/set pair, CIs overlap - and the accessor's own
  interval is seven times wider (+/-0.029 vs +/-0.003 ns), so the tie is not resolvable here either
  (x86-64-v3: 1.07x, also overlapping)

## The code-size figure flips sign between machines - and it is padding, not code

| | x86-64-v3 | x86-64-v4 |
|---|---|---|
| GetSetPair | 85 B | 85 B |
| UnscopedRefAccessor | 88 B (**+3**) | 81 B (**-4**) |

**Why it moves:** both hot loops are 9 instructions on both machines. The byte difference is alignment padding
the JIT inserts before loop heads - GetSetPair carries 14 B of nops here (two 7 B forms), the accessor 9 B (one
`nop word ptr [rax+rax]`), leaving **71 vs 72 B of real code**. Nothing about the accessor got smaller; the
loop head simply landed on a different offset. The ref form folds the read-modify-write into `add [r8],r10`
and pays an extra `lea` for it, which is where its one extra byte of real code comes from.

## What to weigh

1. **Do not read a small Code Size delta as evidence.** The JIT pads to align loop and branch targets, and that
   padding can move by more than 10 B between builds and machines. A byte delta below roughly 16 B says nothing
   until you either count instructions or subtract the nops - this entry is the worked example of a delta that
   reversed sign for that reason alone
2. **Choose `scoped` on API grounds, never performance grounds.** Its codegen is provably identical, which is
   the argument *for* using it freely: it relaxes the escape constraint a ref struct imposes on callers at zero
   runtime cost. Adding or removing it should never be part of a performance change
3. **Choose `[UnscopedRef]` when the API needs it, which is the only reason available.** Without it the
   ref-returning accessor does not compile at all (CS8170). No axis - time, allocation, or real code size -
   improved on either machine, so it has no place in a tuning checklist

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio  | RatioSD | Code Size | Allocated | Alloc Ratio |
|-------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|----------:|----------:|------------:|
| GetSetPair          |  0.3881 ns | 0.0030 ns | 0.0040 ns |  0.3835 ns |  0.3983 ns |  0.3945 ns |   1.00 |    0.01 |      85 B |         - |          NA |
| UnscopedRefAccessor |  0.3943 ns | 0.0290 ns | 0.0406 ns |  0.3565 ns |  0.5026 ns |  0.4482 ns |   1.02 |    0.10 |      81 B |         - |          NA |
| PlainSpanParameter  | 60.5038 ns | 0.3454 ns | 0.5062 ns | 59.6630 ns | 61.5357 ns | 61.0597 ns | 155.91 |    2.01 |     124 B |         - |          NA |
| ScopedSpanParameter | 60.8333 ns | 0.4081 ns | 0.6109 ns | 59.6658 ns | 61.7826 ns | 61.6633 ns | 156.76 |    2.20 |     124 B |         - |          NA |
| PlainRefParameter   |  1.5109 ns | 0.0104 ns | 0.0156 ns |  1.4791 ns |  1.5480 ns |  1.5277 ns |   3.89 |    0.06 |      88 B |         - |          NA |
| ScopedRefParameter  |  1.5163 ns | 0.0071 ns | 0.0104 ns |  1.4792 ns |  1.5314 ns |  1.5289 ns |   3.91 |    0.05 |      88 B |         - |          NA |
