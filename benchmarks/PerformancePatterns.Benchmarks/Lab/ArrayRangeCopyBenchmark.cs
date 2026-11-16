namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-06 study: the range operator on an array allocates, the one on a Span does not
// Question: `array[..n]` lowers to RuntimeHelpers.GetSubArray, which allocates a brand-new array (never a pooled one)
// and copies into it. In a pooled grow loop that means the written bytes are copied twice and the intermediate array
// is thrown away - how much does `array.AsSpan(0, n).CopyTo(destination)` save, and does the LOH/gen2 pressure vanish?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ArrayRangeCopyBenchmark
{
    private const int ChunkSize = 4096;

    [Params(64 * 1024, 1024 * 1024)]
    public int TotalBytes { get; set; }

    // ❌ buffer[..index].CopyTo(grown) - GetSubArray allocates a non-pooled array on every grow
    [Benchmark(Baseline = true)]
    public int GrowWithRangeOperator() => GrowWithRangeOperator(TotalBytes);

    // ✅ buffer.AsSpan(0, index).CopyTo(grown) - one copy, no intermediate array
    [Benchmark]
    public int GrowWithAsSpan() => GrowWithAsSpan(TotalBytes);

    public static void Verify()
    {
        foreach (var total in new[] { ChunkSize, 64 * 1024, (3 * ChunkSize) + 11 })
        {
            if (GrowWithRangeOperator(total) != GrowWithAsSpan(total))
            {
                throw new InvalidOperationException($"Verify failed. ArrayRangeCopy total=[{total}]");
            }
        }

        // The grown buffer must keep the bytes written before the grow
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(ChunkSize);
        buffer.AsSpan(0, ChunkSize).Fill(7);
        var grown = pool.Rent(ChunkSize * 2);
        buffer.AsSpan(0, ChunkSize).CopyTo(grown);
        var sliced = buffer[..ChunkSize];
        if (!sliced.AsSpan().SequenceEqual(grown.AsSpan(0, ChunkSize)) || ReferenceEquals(sliced, buffer))
        {
            throw new InvalidOperationException("Verify failed. ArrayRangeCopy content");
        }

        pool.Return(grown);
        pool.Return(buffer);
    }

    private static int GrowWithRangeOperator(int totalBytes)
    {
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(ChunkSize);
        var index = 0;
        try
        {
            while (index < totalBytes)
            {
                if ((buffer.Length - index) < ChunkSize)
                {
                    var grown = pool.Rent(buffer.Length * 2);
                    buffer[..index].CopyTo(grown.AsSpan());
                    pool.Return(buffer);
                    buffer = grown;
                }

                buffer.AsSpan(index, ChunkSize).Fill(unchecked((byte)index));
                index += ChunkSize;
            }

            return index;
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    private static int GrowWithAsSpan(int totalBytes)
    {
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(ChunkSize);
        var index = 0;
        try
        {
            while (index < totalBytes)
            {
                if ((buffer.Length - index) < ChunkSize)
                {
                    var grown = pool.Rent(buffer.Length * 2);
                    buffer.AsSpan(0, index).CopyTo(grown);
                    pool.Return(buffer);
                    buffer = grown;
                }

                buffer.AsSpan(index, ChunkSize).Fill(unchecked((byte)index));
                index += ChunkSize;
            }

            return index;
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}

// MEM-06 study (isolated): the cost of one slice, with no grow loop around it
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ArraySliceStyleBenchmark
{
    private const int SliceLength = 1024;

    private byte[] source = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new byte[4096];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = unchecked((byte)i);
        }
    }

    // ❌ A new array per call (GetSubArray)
    [Benchmark(Baseline = true)]
    public int ArrayRangeOperator()
    {
        var slice = source[..SliceLength];
        return slice.Length + slice[SliceLength - 1];
    }

    // ✅ A view over the same memory
    [Benchmark]
    public int SpanSlice()
    {
        var slice = source.AsSpan(0, SliceLength);
        return slice.Length + slice[SliceLength - 1];
    }

    public static void Verify()
    {
        var benchmark = new ArraySliceStyleBenchmark();
        benchmark.Setup();
        if (benchmark.ArrayRangeOperator() != benchmark.SpanSlice())
        {
            throw new InvalidOperationException("Verify failed. ArraySliceStyle");
        }
    }
}
