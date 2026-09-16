namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// STK-03 follow-up for .NET 10: conditional escape analysis under dynamic PGO can stack-allocate the
// enumerator when an IEnumerable<T> turns out to be a T[] at runtime, which removes the heap allocation
// that STK-03 exists to avoid. This measures where that applies and where it does not.
//
//   ArrayDirect        - int[] foreach: no enumerator object at all (reference)
//   InterfaceStatic    - field typed IEnumerable<int>, always holding int[]: PGO sees one type
//   InterfaceMixed     - field alternates between int[] and List<int>: PGO cannot commit to one type
//   StructEnumerator   - the STK-03 shape (duck-typed struct enumerator)
//
// Every variant sums the same 1024 values.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class EnumerableEscapeBenchmark
{
    private int[] array = default!;
    private IEnumerable<int> staticSource = default!;
    private IEnumerable<int> mixedArray = default!;
    private IEnumerable<int> mixedList = default!;
    private ArraySequence structSource;
    private int toggle;

    [GlobalSetup]
    public void Setup()
    {
        array = new int[1024];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        staticSource = array;
        mixedArray = array;
        mixedList = new List<int>(array);
        structSource = new ArraySequence(array);
    }

    [Benchmark(Baseline = true)]
    public long ArrayDirect()
    {
        var total = 0L;
        foreach (var value in array)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long InterfaceStatic()
    {
        var total = 0L;
        foreach (var value in staticSource)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long InterfaceMixed()
    {
        // Alternate the concrete type every call so the profile never settles on int[]
        var source = ((toggle++ & 1) == 0) ? mixedArray : mixedList;
        var total = 0L;
        foreach (var value in source)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long StructEnumerator()
    {
        var total = 0L;
        foreach (var value in structSource)
        {
            total += value;
        }

        return total;
    }

    // STK-03: a duck-typed struct enumerator over an array
    public readonly struct ArraySequence
    {
        private readonly int[] items;

        public ArraySequence(int[] items)
        {
            this.items = items;
        }

        public Enumerator GetEnumerator() => new(items);

        public struct Enumerator
        {
            private readonly int[] items;
            private int index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(int[] items)
            {
                this.items = items;
                index = -1;
            }

            public readonly int Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => items[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => ++index < items.Length;
        }
    }
}
