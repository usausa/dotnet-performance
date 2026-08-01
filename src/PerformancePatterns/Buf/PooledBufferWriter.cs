namespace PerformancePatterns.Buf;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// BUF-02: ArrayPool を後背とする IBufferWriter&lt;T&gt; 実装。
/// GetSpan / Advance でゼロコピー書き込みを受け、Dispose でプールへ返却する。
/// Grow はコールドパス分離(JIT-04)、返却時クリアは参照有無で分岐(JIT-05)。
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
        // 参照を含まない型ではクリア不要(JIT-05)
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

    // コールドパス: 分離してホット側のインライン化を保つ(JIT-04)
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
