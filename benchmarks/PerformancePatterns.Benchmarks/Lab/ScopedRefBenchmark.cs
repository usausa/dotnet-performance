namespace PerformancePatterns.Benchmarks.Lab;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 7-1: scoped / [UnscopedRef] (C# 11)
// Question A: is "scoped" purely a compile-time escape contract (identical codegen), or does it cost anything?
//             Judge on Code Size first: if the instruction stream matches, it is a safety tool, not a perf tool.
// Question B: does an [UnscopedRef] ref-returning accessor beat a get/set pair for read-modify-write on struct state?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ScopedRefBenchmark
{
    private const int N = 64;

    private SlotHolder holder;

    private byte[] payload = default!;

    [GlobalSetup]
    public void Setup()
    {
        payload = new byte[256];
        for (var i = 0; i < payload.Length; i++)
        {
            payload[i] = (byte)(i & 0x0F);
        }
    }

    // --- Question B: accessor pair versus a returned ref ---

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public long GetSetPair()
    {
        holder = default;
        for (var i = 0; i < N; i++)
        {
            var slot = i & 7;
            holder.SetValue(slot, holder.GetValue(slot) + i);
        }

        return holder.Total();
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long UnscopedRefAccessor()
    {
        holder = default;
        for (var i = 0; i < N; i++)
        {
            ref var slot = ref holder.GetSlot(i & 7);
            slot += i;
        }

        return holder.Total();
    }

    // --- Question A: scoped on parameters. Compare Code Size, not only time ---

    [Benchmark(OperationsPerInvoke = N)]
    public long PlainSpanParameter()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += SumSpan(payload);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long ScopedSpanParameter()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += SumScopedSpan(payload);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long PlainRefParameter()
    {
        var total = 0L;
        var seed = 1L;
        for (var i = 0; i < N; i++)
        {
            total += StepRef(ref seed);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long ScopedRefParameter()
    {
        var total = 0L;
        var seed = 1L;
        for (var i = 0; i < N; i++)
        {
            total += StepScopedRef(ref seed);
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new ScopedRefBenchmark();
        benchmark.Setup();

        var pair = benchmark.GetSetPair();
        var byRef = benchmark.UnscopedRefAccessor();
        if (pair != byRef)
        {
            throw new InvalidOperationException($"Verify failed. ScopedRef accessor. pair=[{pair}] ref=[{byRef}]");
        }

        if (benchmark.PlainSpanParameter() != benchmark.ScopedSpanParameter())
        {
            throw new InvalidOperationException("Verify failed. ScopedRef span parameter.");
        }

        if (benchmark.PlainRefParameter() != benchmark.ScopedRefParameter())
        {
            throw new InvalidOperationException("Verify failed. ScopedRef ref parameter.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SumSpan(ReadOnlySpan<byte> source)
    {
        var total = 0L;
        for (var i = 0; i < source.Length; i++)
        {
            total += source[i];
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SumScopedSpan(scoped ReadOnlySpan<byte> source)
    {
        var total = 0L;
        for (var i = 0; i < source.Length; i++)
        {
            total += source[i];
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long StepRef(ref long value)
    {
        value = (value * 6364136223846793005L) + 1442695040888963407L;
        return value >>> 33;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long StepScopedRef(scoped ref long value)
    {
        value = (value * 6364136223846793005L) + 1442695040888963407L;
        return value >>> 33;
    }
}

// InlineArray storage so that the accessor genuinely returns a ref into "this"
[InlineArray(8)]
internal struct SlotBuffer
{
    private long element0;
}

internal struct SlotHolder
{
    private SlotBuffer slots;

    // Without [UnscopedRef] this does not compile (CS8170): a struct member may not return a ref to its own state
    [UnscopedRef]
    public ref long GetSlot(int index) => ref slots[index];

    public readonly long GetValue(int index) => slots[index];

    public void SetValue(int index, long value) => slots[index] = value;

    public readonly long Total()
    {
        var total = 0L;
        for (var i = 0; i < 8; i++)
        {
            total += slots[i];
        }

        return total;
    }
}
