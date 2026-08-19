# TXT-11: Type fast path for `object` -> `string` conversion

- Verdict: **adopted for monomorphic call sites, conditional on the fast path being small enough to inline. No claim on polymorphic call sites.**
- Narrow fast path (`int` / `string`, then the same fallback), monomorphic input: **0.91-0.96x across three runs, non-overlapping CIs every time**
- Wide fast path (9 arms, every supported type), monomorphic input: **1.10-1.13x - slower than having no fast path at all**, also reproduced three times
- **Mixed (polymorphic) input: no reliable difference.** The same narrow form measured 0.91x, 0.97x and 1.05x across runs; the interface baseline itself drifts by 6%. Recorded as measurement noise, not as a gain
- The deciding factor on monomorphic input is the inlining threshold: 216-315 B is inlined, 660-799 B is not and costs a call per conversion
- Allocation is identical in all three forms
- Shortest-overload selection is *not* the mechanism - that was separately found not to reproduce on net10

## The shapes compared

```csharp
// Baseline: the usual generic converter
value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value.ToString()

// Narrow fast path - adopted
value switch
{
    int v => v.ToString(CultureInfo.InvariantCulture),
    string v => v,
    _ => value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value.ToString(),
}

// Wide fast path - counterproductive: 9 arms push it past the inlining threshold
```

Two input shapes are measured, because Dynamic PGO behaves completely differently between them: `Monomorphic` (16 boxed `int`) lets guarded devirtualization specialize the interface call; `Mixed` (16 different boxed types through one call site) does not.

## Measured (net10 / x86-64-v4, per value)

Four runs. Runs 2-4 were taken from a standalone harness; the last column is the checked-in benchmark in this repository and is the one to trust for absolute values.

| Shape | Form | run 2 | run 3 | run 4 | **repo** |
|---|---|---:|---:|---:|---:|
| Monomorphic | Interface path (baseline) | 5.559 ns | 5.494 ns | 5.145 ns | **4.944 ns** |
| | Wide (9 arms) | 6.149 (1.11) | 6.052 (1.10) | 5.672 (1.10) | 5.601 (**1.13**) |
| | **Narrow (2 arms)** | - | 5.001 (0.91) | 4.712 (0.92) | 4.723 (**0.96**) |
| Mixed | Interface path (baseline) | 15.427 ns | 15.590 ns | 14.647 ns | **14.220 ns** |
| | Wide (9 arms) | 15.389 (1.00) | 14.498 (0.93) | 14.377 (0.98) | 14.306 (1.01) |
| | Narrow (2 arms) | - | 14.139 (0.91) | 14.142 (0.97) | 14.953 (**1.05**) |

- **Monomorphic is stable and conclusive.** The narrow form is faster in every run with non-overlapping CIs; the wide form is slower in every run
- **Mixed is not.** The narrow form ranges 0.91x to 1.05x - it changes sign between runs - and the interface baseline drifts from 15.59 to 14.22 ns. The CIs overlap in the repository run (interface 14.220 +/- 0.573, narrow 14.953 +/- 0.407). Per the repository's own criterion this is measurement noise; the generated code does differ (see below), so it is recorded as noise rather than "no difference", but **no gain may be claimed on polymorphic call sites**

Allocation is identical throughout (40 B monomorphic, 32 B mixed) - the returned string dominates and no form changes it.

## Why: the inlining threshold

Tier1 disassembly via `DOTNET_JitDisasm` (the DisassemblyDiagnoser does not always emit the type-switch bodies):

| | Monomorphic | Mixed |
|---|---|---|
| `ConvertInterface` | 201 B, inlined into the caller (loop 966 B) | 193 B, inlined (loop 330 B) |
| `ConvertTypeSwitch` (9 arms) | **660 B, not inlined** - loop stays 50 B, a call per value | **799 B, not inlined** - loop 50 B |
| `ConvertTypeSwitchNarrow` (2 arms) | **216 B, inlined** (loop 961 B) | **315 B, inlined** (loop 976 B) |

- **Clearing the inlining threshold is what decides the monomorphic result.** At 216-315 B the fast path is inlined and pays off; at 660-799 B it stays out of line and adds a call per conversion, which is why the wide form is *slower* than not having a fast path at all
- On monomorphic input the interface form is already strong: Tier1 reports `4 inlinees with PGO data`, with one MethodTable guard and `int.ToString(IFormatProvider)` inlined down to a `tail.jmp` into `System.Number.UInt32ToDecStr_NoSmallNumberCheck`, small-value fast path included. The narrow form still wins by removing the guard
- On mixed input the whole comparison is unstable: the interface path's Tier1 code size was observed at 1,777 B, 1,806 B, 15,426 B and 15,792 B in different processes as Dynamic PGO settled differently. That is the source of the sign changes above, and the reason no ratio is claimed there

## Guidance

- Add a type fast path only for **the few types that actually dominate the call site**, and keep it small
- **A fast path that enumerates every supported type defeats itself** - it stops being inlined, and on a monomorphic call site it is worse than no fast path
- **Expect nothing on a genuinely polymorphic call site.** The measurement there is dominated by how Dynamic PGO happens to settle, and did not reproduce in either direction
- Check the result rather than assuming: `DOTNET_JitDisasm=<method>` with `DOTNET_TC_CallCountingDelayMs=0`, and compare Tier1 `Total bytes of code` against the caller's size to see whether it was inlined
- This is a type dispatch question, not a string one: the `switch` here lowers to MethodTable comparisons, unrelated to the length / character bucketing of [TXT-10](TXT-10-StringSwitchDispatch.md)
- Related: [COL-05](COL-05-EnumerableDispatch.md) applies the same type-test idea to `IEnumerable<T>` arguments, where the deciding factor is the input type distribution instead
