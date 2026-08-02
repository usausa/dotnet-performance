namespace PerformancePatterns.Buf;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// BUF-02: IBufferWriter&lt;T&gt; implementation backed by ArrayPool.
/// Accepts zero-copy writes through GetSpan / Advance and returns the buffer to the pool on Dispose.
/// Grow is split into a cold path (JIT-04), and the clear on return branches on whether T holds references (JIT-05).
/// </summary>
public sealed class PooledBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private T[] buffer;

    public PooledBufferWriter(int initialCapacity = 256)
    {
        buffer = ArrayPool<T>.Shared.Rent(initialCapacity);
    }

    public int WrittenCount { get; private set; }

    public ReadOnlySpan<T> WrittenSpan => buffer.AsSpan(0, WrittenCount);

    public ReadOnlyMemory<T> WrittenMemory => buffer.AsMemory(0, WrittenCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => WrittenCount += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return buffer.AsSpan(WrittenCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return buffer.AsMemory(WrittenCount);
    }

    public void Clear()
    {
        // No clearing needed for types that hold no references (JIT-05)
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(buffer, 0, WrittenCount);
        }

        WrittenCount = 0;
    }

    public void Dispose()
    {
        var toReturn = buffer;
        if (toReturn.Length > 0)
        {
            buffer = [];
            WrittenCount = 0;
            ArrayPool<T>.Shared.Return(toReturn, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int sizeHint)
    {
        if (sizeHint < 1)
        {
            sizeHint = 1;
        }

        if (buffer.Length - WrittenCount < sizeHint)
        {
            Grow(sizeHint);
        }
    }

    // Cold path: kept separate so the hot path stays inlineable (JIT-04)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int sizeHint)
    {
        var capacity = Math.Max(WrittenCount + sizeHint, buffer.Length * 2);
        var newBuffer = ArrayPool<T>.Shared.Rent(capacity);
        buffer.AsSpan(0, WrittenCount).CopyTo(newBuffer);

        var toReturn = buffer;
        buffer = newBuffer;
        ArrayPool<T>.Shared.Return(toReturn, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
    }
}
