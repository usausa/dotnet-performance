namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 7-6 / 7-10: the cost of Memory<T> itself.
// The catalog treats Memory<T> only as "the thing you use across an await" (BUF-04) and never states that
// Memory<T>.Span is a real property call that has to resolve the backing store, nor that Memory<T> is the
// larger struct to slice. Both are standard guidance in the BCL memory documentation.
// Question A: how much does resolving .Span per iteration cost versus hoisting it once?
// Question B: does the backing store change that cost (array backed versus MemoryManager backed)?
// Question C: is slicing Memory<T> measurably worse than slicing the hoisted Span<T>?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class MemorySpanCostBenchmark : IDisposable
{
    private const int Length = 4096;

    private const int ChunkSize = 16;

    private byte[] array = default!;

    private Memory<byte> arrayMemory;

    private NativeMemoryManager<byte> manager = default!;

    private Memory<byte> managerMemory;

    [GlobalSetup]
    public void Setup()
    {
        array = new byte[Length];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = (byte)(i & 0x3F);
        }

        arrayMemory = array;

        manager = new NativeMemoryManager<byte>(Length);
        managerMemory = manager.Memory;
        array.AsSpan().CopyTo(managerMemory.Span);
    }

    [GlobalCleanup]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ((IDisposable)manager).Dispose();
        }
    }

    // --- Question A / C: chunked processing, the shape where slicing in a loop actually appears ---

    [Benchmark(Baseline = true)]
    public long ArraySpanHoistedChunks()
    {
        var span = arrayMemory.Span;
        var total = 0L;
        for (var offset = 0; offset < Length; offset += ChunkSize)
        {
            total += Sum(span.Slice(offset, ChunkSize));
        }

        return total;
    }

    [Benchmark]
    public long ArrayMemorySliceChunks()
    {
        var memory = arrayMemory;
        var total = 0L;
        for (var offset = 0; offset < Length; offset += ChunkSize)
        {
            total += Sum(memory.Slice(offset, ChunkSize).Span);
        }

        return total;
    }

    // --- Question A: the extreme shape, resolving .Span for every element ---

    [Benchmark]
    public long ArraySpanHoistedPerElement()
    {
        var span = arrayMemory.Span;
        var total = 0L;
        for (var i = 0; i < Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public long ArraySpanPerElement()
    {
        var total = 0L;
        for (var i = 0; i < Length; i++)
        {
            total += arrayMemory.Span[i];
        }

        return total;
    }

    // --- Question B: the same two shapes over a MemoryManager backed Memory ---

    [Benchmark]
    public long ManagerSpanHoistedChunks()
    {
        var span = managerMemory.Span;
        var total = 0L;
        for (var offset = 0; offset < Length; offset += ChunkSize)
        {
            total += Sum(span.Slice(offset, ChunkSize));
        }

        return total;
    }

    [Benchmark]
    public long ManagerMemorySliceChunks()
    {
        var memory = managerMemory;
        var total = 0L;
        for (var offset = 0; offset < Length; offset += ChunkSize)
        {
            total += Sum(memory.Slice(offset, ChunkSize).Span);
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new MemorySpanCostBenchmark();
        benchmark.Setup();
        try
        {
            var expected = benchmark.ArraySpanHoistedChunks();
            if ((benchmark.ArrayMemorySliceChunks() != expected) ||
                (benchmark.ArraySpanHoistedPerElement() != expected) ||
                (benchmark.ArraySpanPerElement() != expected) ||
                (benchmark.ManagerSpanHoistedChunks() != expected) ||
                (benchmark.ManagerMemorySliceChunks() != expected))
            {
                throw new InvalidOperationException("Verify failed. MemorySpanCost.");
            }
        }
        finally
        {
            benchmark.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Sum(ReadOnlySpan<byte> source)
    {
        var total = 0L;
        for (var i = 0; i < source.Length; i++)
        {
            total += source[i];
        }

        return total;
    }
}

// Study queue 7-13: handing a Memory<T> to an API that only accepts byte[] + offset + count.
// MemoryMarshal.TryGetArray is the no copy bridge and appears nowhere in the catalog.
// Question: how large is the copy that TryGetArray removes, and what does the fallback path cost when the
//           Memory is not array backed (the case that forces the copy to stay)?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class MemoryInteropBenchmark : IDisposable
{
    private const int Length = 4096;

    private Memory<byte> arrayMemory;

    private NativeMemoryManager<byte> manager = default!;

    private Memory<byte> managerMemory;

    [GlobalSetup]
    public void Setup()
    {
        var source = new byte[Length];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = (byte)(i & 0x3F);
        }

        arrayMemory = source;

        manager = new NativeMemoryManager<byte>(Length);
        managerMemory = manager.Memory;
        source.AsSpan().CopyTo(managerMemory.Span);
    }

    [GlobalCleanup]
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            ((IDisposable)manager).Dispose();
        }
    }

    [Benchmark(Baseline = true)]
    public long ToArrayCopy()
    {
        var buffer = arrayMemory.ToArray();
        return ConsumeLegacy(buffer, 0, buffer.Length);
    }

    [Benchmark]
    public long TryGetArraySegment()
    {
        if (!MemoryMarshal.TryGetArray<byte>(arrayMemory, out var segment))
        {
            return -1;
        }

        return ConsumeLegacy(segment.Array!, segment.Offset, segment.Count);
    }

    // The Memory is not array backed, so TryGetArray fails and the copy has to stay
    [Benchmark]
    public long ManagerFallbackCopy()
    {
        if (MemoryMarshal.TryGetArray<byte>(managerMemory, out var segment))
        {
            return ConsumeLegacy(segment.Array!, segment.Offset, segment.Count);
        }

        var buffer = managerMemory.ToArray();
        return ConsumeLegacy(buffer, 0, buffer.Length);
    }

    public static void Verify()
    {
        var benchmark = new MemoryInteropBenchmark();
        benchmark.Setup();
        try
        {
            var expected = benchmark.ToArrayCopy();
            if ((benchmark.TryGetArraySegment() != expected) || (benchmark.ManagerFallbackCopy() != expected))
            {
                throw new InvalidOperationException("Verify failed. MemoryInterop.");
            }

            // The point of the fallback branch: a MemoryManager backed Memory has no array to hand over
            if (MemoryMarshal.TryGetArray<byte>(benchmark.managerMemory, out _))
            {
                throw new InvalidOperationException("Verify failed. MemoryInterop expected TryGetArray to fail.");
            }
        }
        finally
        {
            benchmark.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ConsumeLegacy(byte[] buffer, int offset, int count)
    {
        var total = 0L;
        for (var i = 0; i < count; i++)
        {
            total += buffer[offset + i];
        }

        return total;
    }
}

// Minimal MemoryManager over unmanaged memory: the only way to publish a native buffer as Memory<T>
internal sealed unsafe class NativeMemoryManager<T> : MemoryManager<T>
    where T : unmanaged
{
    private readonly int length;

    private T* pointer;

    public NativeMemoryManager(int length)
    {
        this.length = length;
        pointer = (T*)NativeMemory.Alloc((nuint)length, (nuint)sizeof(T));
        new Span<T>(pointer, length).Clear();
    }

    public override Span<T> GetSpan() => new(pointer, length);

    public override MemoryHandle Pin(int elementIndex = 0) => new(pointer + elementIndex);

    public override void Unpin()
    {
        // Unmanaged memory never moves, so there is nothing to release
    }

    protected override void Dispose(bool disposing)
    {
        if (pointer is not null)
        {
            NativeMemory.Free(pointer);
            pointer = null;
        }
    }
}
