# 📐 Benchmarking Guidelines

[日本語](benchmark-methodology.ja.md) | **English**

The BenchmarkDotNet setup used to verify pattern effectiveness, plus how to avoid the pitfalls that make measurements meaningless.
The "measured examples" in [README](../README.md) assume measurements taken according to these guidelines.

## 🧰 Base configuration

- **Keep MemoryDiagnoser enabled at all times** — always judge speed and allocation together
- **Enable DisassemblyDiagnoser (printSource, exportDiff)** — to inspect generated code and code size. Even when the speed difference is at the measurement-noise level, code size can settle which variant is better (the impact on inlining shows up in code size)
- To look at generated code as a one-off without going through a benchmark, set the environment variable `DOTNET_JitDisasm="MethodName"` and run: the JIT assembly goes to stdout (a Release build plus `DOTNET_TieredCompilation=0` lets you inspect the final code directly)
- **By default, measure on the latest runtime (net10.0) alone**. Reserve running multiple runtimes side by side for verifications that specifically ask whether the effect changes across generations (optimizations that disappear in a newer generation, such as bounds-check elimination idioms or uint-cast tricks)

```csharp
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
            maxDepth: 3, printSource: true, exportDiff: true)));
        AddColumn(StatisticColumn.Min, StatisticColumn.Max, StatisticColumn.P90);
    }
}

// On the class: net10.0 only by default. Add jobs for net8 and the like only on classes targeted for generation verification
[MediumRunJob(RuntimeMoniker.Net10_0)]
```

## ⚠️ Pitfalls that make measurements meaningless

### 1. The measurement target vanishing under optimization

If a result is pinned to the "empty loop floor", the JIT has eliminated that variant entirely and you are not comparing real costs. Prevent that by returning a value, applying `[MethodImpl(MethodImplOptions.NoInlining)]`, or using BenchmarkDotNet's Consumer. Conversely, the fact that it was eliminated is sometimes itself the conclusion — "that abstraction is zero-cost" — so be deliberate about which of the two you are measuring.

### 2. String interning short-circuiting the comparison

If you use string literals directly as keys, reference equality makes `string.Equals` short-circuit without comparing contents, so you are not measuring the comparison code at all. Production traffic brings external input (non-interned strings), so always build probe strings as copies and confirm they are non-interned before measuring.

```csharp
var probe = new string(literal.AsSpan());          // Create a non-interned copy
Debug.Assert(string.IsInterned(probe) is null || !ReferenceEquals(string.IsInterned(probe), probe));
```

### 3. Not verifying equivalence across variants

Before measuring, run a `Verify()` that confirms all variants return the same result (call it before `BenchmarkRunner.Run`). Measuring an implementation that is fast but wrong is pointless. Manual ref walking is especially prone to bugs (miscomputed end refs, forgetting to advance a ref in a dual walk, and so on). As a real example, a loop whose faulty end condition was "always true" had the whole condition removed by the JIT, producing an abnormally fast, bounds-check-free false result that was believed for a long time (after the fix, re-measurement dropped it from fastest to mid-pack).

### 4. Measuring only the best case

Measuring only ideal shapes such as declaration-order access leads you to pick an implementation that degrades in production. Parameterize the access shape (forward / reverse / partial access / mixed misses) and choose the implementation that is stable across shapes, not the one with the fastest average.

### 5. Exception paths mixed in or not separated

Measure success and failure cases separately via `[Params]`. A single exception throw costs on the order of several μs, which completely masks every other optimization difference (if the failure path throws, optimizing everything around it is meaningless).

### 6. Over-trusting microbenchmark results

We have measured cases where a 30x difference on an isolated primitive dilutes to roughly 1.1x once embedded in real processing (I/O, rendering, dominant computation). Make the final call with a benchmark shaped like the real workload, and use microbenchmarks to select which implementations are candidates.

### 7. Mixing TFM-dependent methods via #if (when running multiple runtimes)

If you mix benchmarks for APIs that exist only on newer runtimes into one class with `#if NET9_0_OR_GREATER` and the like, the child build for the older runtime cannot resolve the methods the host (latest TFM) discovered, and **every case on that runtime comes out NA**. Separate TFM-dependent comparisons into their own classes behind `#if`, and attach only the corresponding runtime's job to each class.

### 8. Setup work leaking into the measurement

Do preparation such as searching for colliding keys or generating data in `[GlobalSetup]` and keep it out of the measurement. `IterationSetup` degrades measurement accuracy, so design for GlobalSetup plus no need for state resets wherever possible.

## ⚖️ Decision criteria

- Evaluate on **three axes: speed, allocation, and code size**. An improvement on one axis alone is weak justification for adoption
- Make the Ratio baseline "the straightforward implementation you have today" so the improvement reads off directly
- Record optimizations whose effect vanished in a newer generation as patterns that are "no longer needed" (move them to [rejected-patterns.md](rejected-patterns.md))

### Handling measurements that fall within measurement noise

**"A nanosecond-scale difference" is not measurement noise.** Nanosecond-scale differences are exactly what this repository is about, and a difference with non-overlapping CIs is recorded as a real difference even at 0.2 ns. Call it "measurement noise" **only when the confidence intervals (error bars) overlap and the difference cannot be resolved statistically**. Even then, do not declare it "rejected" outright — **go down to the generated code and split the case in two**:

| Generated code check result | Record as |
|---|---|
| Differs (instruction sequence or code size differs) | Record it as **➖ measurement noise**. Do not reject it — a real difference exists below measurement resolution, so keep the numbers on record along with the room it has to pay off depending on code size, environment, and inlining context |
| Matches (identical instruction sequence) | **No difference** — move it to the rejected side. Record "the generated code matched" as the basis |

The check has two stages:

1. **First pass**: the Code Size column from DisassemblyDiagnoser. If the size differs between variants, the generated code differs
2. **Confirmation**: comparing instruction sequences with JitDisasm. DynamicMethod can be matched by name too

```
DOTNET_TieredCompilation=0 DOTNET_JitDisasm="*MethodName*" ./app.exe
```

Example: GEN-01's replacement of delegate Invoke with a `Call` measured as noise at 14.2 vs 14.6 ns, but the JitDisasm comparison showed **68 instructions / 229 bytes matching exactly**, which confirmed "no difference" (the load of the target field doubles as the null check, so the JIT removes the `callvirt` check).
