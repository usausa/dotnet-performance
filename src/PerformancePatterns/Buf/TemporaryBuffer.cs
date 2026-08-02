namespace PerformancePatterns.Buf;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// BUF-05: Tiered strategy for temporary buffers. Combines stackalloc for small sizes and
/// ArrayPool for large ones into a single ref struct with a using scope.
/// <code>
/// using var buffer = size &lt;= 512
///     ? new TemporaryBuffer&lt;byte&gt;(stackalloc byte[512], size)
///     : new TemporaryBuffer&lt;byte&gt;(size);
/// Process(buffer.Span);
/// </code>
/// </summary>
public ref struct TemporaryBuffer<T>
{
    private T[]? pooled;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TemporaryBuffer(Span<T> initial, int length)
    {
        Span = initial[..length];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TemporaryBuffer(int length)
    {
        pooled = ArrayPool<T>.Shared.Rent(length);
        Span = pooled.AsSpan(0, length);
    }

    public Span<T> Span { get; }

    public void Dispose()
    {
        var toReturn = pooled;
        if (toReturn is not null)
        {
            pooled = null;
            ArrayPool<T>.Shared.Return(toReturn);
        }
    }
}
