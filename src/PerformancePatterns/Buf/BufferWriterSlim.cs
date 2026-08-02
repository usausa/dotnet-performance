namespace PerformancePatterns.Buf;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// BUF-03: Stack-first sequential write buffer.
/// Completely allocation-free while the initial buffer (stackalloc) is large enough;
/// only on overflow does it rent from ArrayPool and carry the contents over.
/// Grow is split into a cold path (JIT-04), and the clear on return branches on whether T holds references (JIT-05).
/// <code>
/// using var writer = new BufferWriterSlim&lt;byte&gt;(stackalloc byte[256]);
/// writer.Write(header);
/// Send(writer.WrittenSpan);
/// </code>
/// </summary>
public ref struct BufferWriterSlim<T>
{
    private Span<T> buffer;

    private T[]? pooled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BufferWriterSlim(Span<T> initialBuffer)
    {
        buffer = initialBuffer;
    }

    public int WrittenCount { get; private set; }

    public readonly ReadOnlySpan<T> WrittenSpan => buffer[..WrittenCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> GetSpan(int sizeHint = 1)
    {
        if (sizeHint < 1)
        {
            sizeHint = 1;
        }

        if (buffer.Length - WrittenCount < sizeHint)
        {
            Grow(sizeHint);
        }

        return buffer[WrittenCount..];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => WrittenCount += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<T> value)
    {
        if (buffer.Length - WrittenCount < value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(buffer[WrittenCount..]);
        WrittenCount += value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T value)
    {
        if (buffer.Length == WrittenCount)
        {
            Grow(1);
        }

        buffer[WrittenCount] = value;
        WrittenCount++;
    }

    // Growth is split into a cold path so the caller can still be inlined (JIT-04)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int required)
    {
        var newSize = Math.Max(buffer.Length * 2, WrittenCount + required);
        var newArray = ArrayPool<T>.Shared.Rent(Math.Max(newSize, 16));
        buffer[..WrittenCount].CopyTo(newArray);

        var toReturn = pooled;
        pooled = newArray;
        buffer = newArray;
        if (toReturn is not null)
        {
            ArrayPool<T>.Shared.Return(toReturn, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }

    public void Dispose()
    {
        var toReturn = pooled;
        this = default;
        if (toReturn is not null)
        {
            ArrayPool<T>.Shared.Return(toReturn, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }
}
