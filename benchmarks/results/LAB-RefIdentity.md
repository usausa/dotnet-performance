# LAB-RefIdentity: ref identity and ref arithmetic (study queue 7-11)

- Verdict: index recovery rejected -> R-21; alias checking recorded as a quick-reference note. Re-measured on
  x86-64-v4 (Zen 5); both conclusions reproduce with slightly larger margins than x86-64-v3 (Zen 3)
- Recovering the index from a ref with `Unsafe.ByteOffset` costs **1.52x** against simply carrying the index
  (507.7 vs 333.3 ns, CIs disjoint), and the code is larger: 82 vs 64 B, 27 vs 22 instructions
  (x86-64-v3: 1.45x, same code sizes). Same conclusion as R-02
- Alias checking: `Unsafe.AreSame` on the first elements is **0.68x** of `MemoryExtensions.Overlaps`
  (453.2 vs 664.5 ns over 1,024 checks = **0.44 vs 0.65 ns per check**) with smaller code, 89 vs 123 B and
  31 vs 45 instructions (x86-64-v3: 0.71x)
- **The Ratio column compares everything to `IndexCarried`, which is a different workload from the two alias
  checks.** Only the within-question comparisons above mean anything
- `AliasCheckWithAreSame` is the noisiest row here (453.2 +/- 20.8 ns, median 436.4, range 426-535). Its
  comparison against Overlaps still holds by a wide margin, but do not quote its absolute value to three digits

**Why the index-recovery margin does not shrink on a wider core:** the recovery adds a `ByteOffset` subtract and
a shift **per element, on the dependent chain** that already carries the accumulation. That is work a wider core
cannot hide - it grew from 1.45x to 1.52x rather than shrinking, which is the opposite of what happens to
patterns whose extra work is independent (compare STK-10, where the margin narrowed).

## What to weigh

1. **Carry the index.** If you hold a ref you almost always had the index that produced it; recomputing it costs
   1.45-1.52x on both machines and more code. There is no configuration measured here where recovery wins
2. **Choose the alias check by the question, not by the cost.** `AreSame` answers "do these start at the same
   address"; `Overlaps` answers "do these ranges intersect at all". Using the cheaper one where the other was
   meant is a correctness bug, and 0.21 ns per check cannot pay for it
3. **Neither check belongs on an optimization list.** Both are sub-nanosecond per call. If an alias check is
   visible in a profile, the finding is that it sits inside a loop, not that the wrong API was chosen - hoist it
   to the precondition it is
4. **`Unsafe.ByteOffset` remains useful for what it says** - the distance between two refs - just not as a way
   to reconstruct a loop index you threw away

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
| Method                 | Mean     | Error    | StdDev   | Median   | Min      | Max      | P90      | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|----------------------- |---------:|---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|----------:|----------:|------------:|
| IndexCarried           | 333.3 ns |  0.96 ns |  1.35 ns | 333.7 ns | 331.4 ns | 335.6 ns | 334.7 ns |  1.00 |    0.01 |      64 B |         - |          NA |
| IndexRecoveredFromRef  | 507.7 ns |  6.63 ns |  9.30 ns | 506.3 ns | 497.5 ns | 528.0 ns | 519.4 ns |  1.52 |    0.03 |      82 B |         - |          NA |
| AliasCheckWithAreSame  | 453.2 ns | 20.81 ns | 31.15 ns | 436.4 ns | 426.0 ns | 535.3 ns | 500.3 ns |  1.36 |    0.09 |      89 B |         - |          NA |
| AliasCheckWithOverlaps | 664.5 ns |  1.92 ns |  2.88 ns | 663.4 ns | 660.7 ns | 671.9 ns | 667.7 ns |  1.99 |    0.01 |     123 B |         - |          NA |
