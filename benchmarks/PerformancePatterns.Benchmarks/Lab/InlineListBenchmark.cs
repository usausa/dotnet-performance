namespace PerformancePatterns.Benchmarks.Lab;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-08 follow-up: a small list that keeps its first elements in an [InlineArray] buffer inside the
// struct and spills to a heap array only when the buffer is exceeded (the "small vector" shape from the
// Qiita article on InlineArray value-type lists). STK-08 measures the fixed-size buffer only; this
// measures the variable-length form against List<T> at a size that fits (4) and one that spills (32).
//
// The struct is a ref struct so it cannot be copied into a field or captured — the article's own caveat
// is that a copied instance in buffer mode silently diverges from the original.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class InlineListBenchmark
{
    [Params(4, 32)]
    public int Items { get; set; }

    [Benchmark(Baseline = true)]
    public long ListDefault()
    {
        var list = new List<int>();
        for (var i = 0; i < Items; i++)
        {
            list.Add(i);
        }

        var total = 0L;
        foreach (var value in CollectionsMarshal.AsSpan(list))
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long ListWithCapacity()
    {
        var list = new List<int>(Items);
        for (var i = 0; i < Items; i++)
        {
            list.Add(i);
        }

        var total = 0L;
        foreach (var value in CollectionsMarshal.AsSpan(list))
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long InlineListStruct()
    {
        var list = default(InlineList<int>);
        for (var i = 0; i < Items; i++)
        {
            list.Add(i);
        }

        var total = 0L;
        foreach (var value in list.AsSpan())
        {
            total += value;
        }

        return total;
    }

    // Eight elements inline, then a heap array. Kept minimal on purpose: Add / AsSpan / Count.
    public ref struct InlineList<T>
    {
        private const int InlineCapacity = 8;

        private Buffer8 buffer;
        private T[]? spill;

        public int Count { get; private set; }

        public void Add(T item)
        {
            if (spill is null)
            {
                if (Count < InlineCapacity)
                {
                    buffer[Count++] = item;
                    return;
                }

                // First spill: copy the inline elements out, then keep growing on the heap
                spill = new T[InlineCapacity * 2];
                ((ReadOnlySpan<T>)buffer).CopyTo(spill);
            }
            else if (Count == spill.Length)
            {
                Array.Resize(ref spill, spill.Length * 2);
            }

            spill[Count++] = item;
        }

        [UnscopedRef]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ReadOnlySpan<T> AsSpan() =>
            spill is null
                ? ((ReadOnlySpan<T>)buffer)[..Count]
                : spill.AsSpan(0, Count);

        [InlineArray(InlineCapacity)]
        private struct Buffer8
        {
            private T element0;
        }
    }
}
