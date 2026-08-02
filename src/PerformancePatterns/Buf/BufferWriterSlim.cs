namespace PerformancePatterns.Buf;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// BUF-03: スタックファーストの逐次書き込みバッファ。
/// 初期バッファ(stackalloc)が足りる間は完全にアロケーションフリーで、
/// 超過時のみ ArrayPool からレンタルして内容を引き継ぐ。
/// Grow はコールドパス分離(JIT-04)、返却時クリアは参照有無で分岐(JIT-05)。
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

    // 成長はコールドパスとして分離し、呼び出し側のインライン化を妨げない(JIT-04)
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
