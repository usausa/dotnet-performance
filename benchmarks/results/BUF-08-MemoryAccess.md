# BUF-08: Memory<T> access cost, MemoryManager backing, and array interop

## net11 / net10 re-verification (2026-11-19, HX 370)

- Verdict: unchanged - adopted
- Slicing `Memory<T>` per chunk is 1.09x (array backed) and 1.16x (manager backed) of the hoisted span on net11 (1.07x / 1.08x on net10)
- TryGetArraySegment stays 0.89-0.90x of ToArray on both generations

### MemorySpanCostBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ArraySpanHoistedChunks | 770.6 ns | 745.5 ns | 0.97 | - → - | 248 B → 250 B |
| ArrayMemorySliceChunks | 824.5 ns | 812.3 ns | 0.99 | - → - | 268 B → 277 B |
| ArraySpanHoistedPerElement | 817.8 ns | 817.4 ns | 1.00 | - → - | 201 B → 207 B |
| ArraySpanPerElement | 2,505.6 ns | 2,883.0 ns | 1.15 | - → - | 178 B → 188 B |
| ManagerSpanHoistedChunks | 759.1 ns | 747.0 ns | 0.98 | - → - | 297 B → 299 B |
| ManagerMemorySliceChunks | 832.4 ns | 860.4 ns | 1.03 | - → - | 520 B → 532 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.78 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method                     | Job                 | Runtime   | Mean       | Error     | StdDev    | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |-------------------- |---------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| ArraySpanHoistedChunks     | .NET 10.0           | .NET 10.0 |   770.6 ns |  24.76 ns |  34.72 ns |   730.8 ns |   814.2 ns |   811.5 ns |  1.00 |    0.00 |     248 B |         - |          NA |
| ArrayMemorySliceChunks     | .NET 10.0           | .NET 10.0 |   824.5 ns |  15.16 ns |  21.74 ns |   793.8 ns |   859.8 ns |   852.5 ns |  1.07 |    0.05 |     268 B |         - |          NA |
| ArraySpanHoistedPerElement | .NET 10.0           | .NET 10.0 |   817.8 ns |   1.31 ns |   1.87 ns |   814.7 ns |   821.4 ns |   819.9 ns |  1.06 |    0.05 |     201 B |         - |          NA |
| ArraySpanPerElement        | .NET 10.0           | .NET 10.0 | 2,505.6 ns |  11.35 ns |  16.28 ns | 2,484.0 ns | 2,545.2 ns | 2,529.2 ns |  3.26 |    0.15 |     178 B |         - |          NA |
| ManagerSpanHoistedChunks   | .NET 10.0           | .NET 10.0 |   759.1 ns |   8.93 ns |  13.37 ns |   735.2 ns |   790.8 ns |   773.6 ns |  0.99 |    0.05 |     297 B |         - |          NA |
| ManagerMemorySliceChunks   | .NET 10.0           | .NET 10.0 |   832.4 ns |   4.18 ns |   5.86 ns |   823.9 ns |   849.8 ns |   838.4 ns |  1.08 |    0.05 |     520 B |         - |          NA |
|                            |                     |           |            |           |           |            |            |            |       |         |           |           |             |
| ArraySpanHoistedChunks     | MediumRun-.NET 11.0 | .NET 11.0 |   745.5 ns |  17.15 ns |  25.13 ns |   716.0 ns |   799.8 ns |   775.8 ns |  1.00 |    0.00 |     250 B |         - |          NA |
| ArrayMemorySliceChunks     | MediumRun-.NET 11.0 | .NET 11.0 |   812.3 ns |   3.65 ns |   5.24 ns |   803.3 ns |   820.2 ns |   819.2 ns |  1.09 |    0.04 |     277 B |         - |          NA |
| ArraySpanHoistedPerElement | MediumRun-.NET 11.0 | .NET 11.0 |   817.4 ns |   1.34 ns |   1.88 ns |   814.7 ns |   823.6 ns |   819.5 ns |  1.10 |    0.04 |     207 B |         - |          NA |
| ArraySpanPerElement        | MediumRun-.NET 11.0 | .NET 11.0 | 2,883.0 ns | 269.05 ns | 402.70 ns | 2,474.2 ns | 3,308.5 ns | 3,285.6 ns |  3.87 |    0.55 |     188 B |         - |          NA |
| ManagerSpanHoistedChunks   | MediumRun-.NET 11.0 | .NET 11.0 |   747.0 ns |   6.33 ns |   9.48 ns |   731.3 ns |   764.9 ns |   760.4 ns |  1.00 |    0.04 |     299 B |         - |          NA |
| ManagerMemorySliceChunks   | MediumRun-.NET 11.0 | .NET 11.0 |   860.4 ns |   5.09 ns |   7.46 ns |   847.1 ns |   878.0 ns |   871.1 ns |  1.16 |    0.04 |     532 B |         - |          NA |

### MemoryInteropBenchmark

net10 → net11 per row:

| Method | net10 | net11 | net11/net10 | Alloc net10 → net11 | Code net10 → net11 |
|---|---|---|---|---|---|
| ToArrayCopy | 979.1 ns | 987.8 ns | 1.01 | 4120 B → 4120 B | 755 B → 788 B |
| TryGetArraySegment | 881.0 ns | 881.1 ns | 1.00 | - → - | 565 B → 386 B |
| ManagerFallbackCopy | 979.8 ns | 982.9 ns | 1.00 | 4120 B → 4120 B | 1,295 B → 1,194 B |

```

BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9974)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
Memory: 95.62 GB Total, 69.68 GB Available
.NET SDK 11.0.100
  [Host]              : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4
  .NET 10.0           : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun-.NET 11.0 : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v4

IterationCount=15  LaunchCount=2  WarmupCount=10  

```
| Method              | Job                 | Runtime   | Mean     | Error    | StdDev   | Min      | Max        | P90        | Ratio | RatioSD | Code Size | Gen0   | Allocated | Alloc Ratio |
|-------------------- |-------------------- |---------- |---------:|---------:|---------:|---------:|-----------:|-----------:|------:|--------:|----------:|-------:|----------:|------------:|
| ToArrayCopy         | .NET 10.0           | .NET 10.0 | 979.1 ns |  3.42 ns |  5.11 ns | 969.4 ns |   990.2 ns |   985.2 ns |  1.00 |    0.00 |     755 B | 0.4921 |    4120 B |        1.00 |
| TryGetArraySegment  | .NET 10.0           | .NET 10.0 | 881.0 ns |  1.32 ns |  1.90 ns | 877.5 ns |   884.4 ns |   883.5 ns |  0.90 |    0.00 |     565 B |      - |         - |        0.00 |
| ManagerFallbackCopy | .NET 10.0           | .NET 10.0 | 979.8 ns |  4.41 ns |  6.19 ns | 970.2 ns |   995.8 ns |   987.8 ns |  1.00 |    0.01 |   1,295 B | 0.4921 |    4120 B |        1.00 |
|                     |                     |           |          |          |          |          |            |            |       |         |           |        |           |             |
| ToArrayCopy         | MediumRun-.NET 11.0 | .NET 11.0 | 987.8 ns | 13.22 ns | 18.54 ns | 968.1 ns | 1,043.9 ns | 1,015.4 ns |  1.00 |    0.00 |     788 B | 0.4921 |    4120 B |        1.00 |
| TryGetArraySegment  | MediumRun-.NET 11.0 | .NET 11.0 | 881.1 ns |  1.06 ns |  1.49 ns | 878.5 ns |   884.2 ns |   883.4 ns |  0.89 |    0.02 |     386 B |      - |         - |        0.00 |
| ManagerFallbackCopy | MediumRun-.NET 11.0 | .NET 11.0 | 982.9 ns |  5.76 ns |  8.44 ns | 972.2 ns | 1,005.6 ns |   994.8 ns |  1.00 |    0.02 |   1,194 B | 0.4921 |    4120 B |        1.00 |

## Earlier record

- Verdict: adopted. Every conclusion from the earlier x86-64-v3 (Zen 3) run reproduces on x86-64-v4 (Zen 5);
  the ratios move, the ranking does not
- Resolving `.Span` per element costs **3.08x / 3.11x** against hoisting it once (two runs; x86-64-v3: 2.96x)
- Slicing `Memory<T>` per chunk still loses to slicing a hoisted `Span<T>`: **1.05x** array backed and
  1.06-1.07x MemoryManager backed (x86-64-v3: 1.10x / 1.13x). The array-backed row overlapped by 7 ns in the
  first run and was re-measured; run 2 resolves it cleanly at the same 1.05x
- `MemoryMarshal.TryGetArray` removes the copy entirely when handing a `Memory<T>` to a byte[]+offset+count API:
  **0.84x** and 4,120 B -> 0 B allocated, code 934 -> 414 B (x86-64-v3: 0.90x, same allocation and code story)
- `TryGetArray` returns false for a MemoryManager backed Memory, so the copy fallback has to stay reachable
  (asserted in Verify)

## What the penalty actually scales with

The workload is fixed at 4,096 bytes. Only the number of `.Span` resolutions changes:

| Shape | Resolutions | Work per resolution | Cost |
|---|---:|---:|---:|
| `span[i]` hoisted | 1 | 4,096 elements | baseline 842 ns |
| `memory.Slice(o,16).Span` | 256 | 16 elements | 887 ns (**1.05x**, +0.17 ns each) |
| `memory.Span[i]` | 4,096 | 1 element | 2,627 ns (**3.11x**, +0.44 ns each) |

**Why the same operation costs 0.17 ns in one shape and 0.44 ns in the other:** a `.Span` resolution is a fixed
piece of work (unpack object + index + length, branch on array vs manager backing). In the chunk loop its result
feeds 16 element reads, so the out-of-order core hides most of its latency behind that work. In the per-element
loop it sits in a dependent chain with nothing to overlap, and the full latency lands on every iteration.

## What to weigh

1. **Hoist `.Span` out of every loop.** This is the whole pattern - 3.1x on both machines, no conditions. If a
   method takes `Memory<T>` and reads it more than once, resolve once into a local `Span<T>`
2. **When you cannot hoist (async boundaries force `Memory<T>` to survive an await), size the chunk by how much
   work each resolution feeds, not by byte count.** The penalty is per resolution and the core hides it in
   proportion to the work behind it: 16 elements per resolution already brings 3.11x down to 1.05x
3. **MemoryManager backing is not a throughput penalty - it measured slightly faster, and that counts.** Once
   `.Span` is hoisted the manager-backed walk came in at **0.95x** here (CIs disjoint) and 0.97x on Zen 3. A few
   percent is small, but it reproduced on both machines in the same direction, so it belongs in the decision as
   a point in favour rather than being rounded away. The hoisted loop's codegen is identical between the two, so
   the likely source is data alignment - NativeMemory hands back naturally aligned blocks while an array's
   payload starts 16 B past its header - and that part is **unverified**. The manager's cost is confined to the
   resolution itself (518 B vs 297 B of code), which only matters where the resolution is not hoisted
4. **Reach for `TryGetArray` at every byte[]-shaped API boundary.** It is faster on both machines and widening
   (0.90x -> **0.84x**, CIs disjoint), and it removes **4,120 B of allocation per call**. The time and the GC
   pressure are the reasons to adopt it; the call site more than halving (934 -> 414 B) is a bonus on top
5. **Keep the copy fallback compiled and tested.** `TryGetArray` fails for manager-backed memory, and the
   fallback measured 0.99-1.05x against the plain copy - it costs nothing to keep, and it is not dead code

## x86-64-v4 run 2 - Memory access (run 1 differed only in the 7 ns overlap noted above)

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method                     | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Code Size | Allocated | Alloc Ratio |
|--------------------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|----------:|----------:|------------:|
| ArraySpanHoistedChunks     |   843.5 ns |  9.09 ns | 13.32 ns |   819.1 ns |   878.5 ns |   855.2 ns |  1.00 |    0.02 |     248 B |         - |          NA |
| ArrayMemorySliceChunks     |   887.1 ns | 12.81 ns | 19.17 ns |   852.9 ns |   928.5 ns |   910.6 ns |  1.05 |    0.03 |     268 B |         - |          NA |
| ArraySpanHoistedPerElement |   842.3 ns |  1.78 ns |  2.61 ns |   838.2 ns |   847.6 ns |   845.4 ns |  1.00 |    0.02 |     201 B |         - |          NA |
| ArraySpanPerElement        | 2,626.7 ns | 16.61 ns | 24.35 ns | 2,596.8 ns | 2,687.6 ns | 2,659.6 ns |  3.11 |    0.06 |     178 B |         - |          NA |
| ManagerSpanHoistedChunks   |   802.8 ns |  7.48 ns | 10.73 ns |   782.3 ns |   823.7 ns |   815.2 ns |  0.95 |    0.02 |     297 B |         - |          NA |
| ManagerMemorySliceChunks   |   892.2 ns |  5.87 ns |  8.61 ns |   879.3 ns |   909.7 ns |   904.9 ns |  1.06 |    0.02 |     518 B |         - |          NA |

## x86-64-v4 - array interop

```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.9168/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.400
  [Host]              : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4
  MediumRun-.NET 10.0 : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v4

Job=MediumRun-.NET 10.0  Runtime=.NET 10.0  IterationCount=15  
LaunchCount=2  WarmupCount=10  

```
| Method              | Mean       | Error    | StdDev   | Min        | Max        | P90        | Ratio | RatioSD | Gen0   | Code Size | Allocated | Alloc Ratio |
|-------------------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-------:|----------:|----------:|------------:|
| ToArrayCopy         | 1,095.8 ns | 18.56 ns | 27.20 ns | 1,058.0 ns | 1,148.9 ns | 1,127.1 ns |  1.00 |    0.03 | 0.4921 |     934 B |    4120 B |        1.00 |
| TryGetArraySegment  |   922.8 ns |  6.63 ns |  9.29 ns |   908.2 ns |   951.1 ns |   932.2 ns |  0.84 |    0.02 |      - |     414 B |         - |        0.00 |
| ManagerFallbackCopy | 1,083.7 ns | 18.63 ns | 27.31 ns | 1,045.3 ns | 1,151.0 ns | 1,116.8 ns |  0.99 |    0.03 | 0.4921 |   1,243 B |    4120 B |        1.00 |
