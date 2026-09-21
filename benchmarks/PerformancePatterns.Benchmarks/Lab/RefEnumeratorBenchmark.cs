namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-03 follow-up (Gravell 2022, "ref foreach"): a struct enumerator can return its Current as ref readonly T
// instead of T, and the caller can then write foreach (ref readonly var x in ...). With a large struct element
// that is *passed to a method* in the loop body, a by-value Current copies the element once per iteration; the ref
// form does not. R-04 already showed that reading a single field copies nothing either way, so the body here calls
// a non-inlined method that takes the element by `in` (the shape where the copy cannot be elided).
//   ValueCurrent    - struct enumerator, Entry Current, foreach (var e in ...)
//   RefCurrent      - struct enumerator, ref readonly Entry Current, foreach (ref readonly var e in ...)
//   SpanRefForeach  - foreach (ref readonly var e in span) over the same array (the BCL reference shape)
//   ForRef          - for + ref readonly local over the array (MEM-02 shape)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RefEnumeratorBenchmark
{
    private const int Count = 1024;

    private Entry[] entries = default!;

    [GlobalSetup]
    public void Setup()
    {
        entries = new Entry[Count];
        for (var i = 0; i < entries.Length; i++)
        {
            entries[i] = new Entry(i);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public long ValueCurrent()
    {
        var total = 0L;
        foreach (var entry in new ValueEnumerable(entries))
        {
            total += Consume(in entry);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long RefCurrent()
    {
        var total = 0L;
        foreach (ref readonly var entry in new RefEnumerable(entries))
        {
            total += Consume(in entry);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long SpanRefForeach()
    {
        var total = 0L;
        foreach (ref readonly var entry in entries.AsSpan())
        {
            total += Consume(in entry);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public long ForRef()
    {
        var total = 0L;
        var array = entries;
        for (var i = 0; i < array.Length; i++)
        {
            ref readonly var entry = ref array[i];
            total += Consume(in entry);
        }

        return total;
    }

    // Not inlined, so the callee needs the whole element by reference and the caller cannot narrow the copy to one field
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Consume(in Entry entry) => entry.Id + entry.Tail;

    // Every variant must produce the same sum
    public static void Verify()
    {
        var benchmark = new RefEnumeratorBenchmark();
        benchmark.Setup();
        var expected = benchmark.ForRef();
        if ((benchmark.ValueCurrent() != expected) ||
            (benchmark.RefCurrent() != expected) ||
            (benchmark.SpanRefForeach() != expected))
        {
            throw new InvalidOperationException("Verify failed. RefEnumerator");
        }
    }

    // 64-byte element: big enough that a per-iteration copy is visible
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private readonly struct Entry
    {
        public readonly long Id;
        public readonly long Tail;

        public Entry(long id)
        {
            Id = id;
            Tail = id * 2;
        }
    }

    private readonly struct ValueEnumerable(Entry[] source)
    {
        public ValueEnumerator GetEnumerator() => new(source);
    }

    private struct ValueEnumerator(Entry[] source)
    {
        private int index = -1;

        public readonly Entry Current => source[index];

        public bool MoveNext() => ++index < source.Length;
    }

    private readonly struct RefEnumerable(Entry[] source)
    {
        public RefEnumerator GetEnumerator() => new(source);
    }

    private struct RefEnumerator(Entry[] source)
    {
        private int index = -1;

        public readonly ref readonly Entry Current => ref source[index];

        public bool MoveNext() => ++index < source.Length;
    }
}
