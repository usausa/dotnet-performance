namespace PerformancePatterns.Buf;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// BUF-04: IMemoryOwner&lt;T&gt; implementation that gives an ArrayPool rental a using scope.
/// Hides the gap between the requested length and the actual rented length (a power of two), exposing Span / Memory of the exact length.
/// Double Dispose is made harmless by an Interlocked guard (CON-01).
/// If everything stays inside synchronous code, BUF-05 (TemporaryBuffer) avoids even the owner allocation —
/// this type targets async I/O boundaries that require Memory&lt;T&gt;.
/// </summary>
public sealed class MemoryOwner<T> : IMemoryOwner<T>
{
    private T[]? array;

    private MemoryOwner(T[] array, int length)
    {
        this.array = array;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MemoryOwner<T> Allocate(int length) =>
        new(ArrayPool<T>.Shared.Rent(length), length);

    public int Length { get; }

    public Memory<T> Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var local = array ?? ThrowDisposed();
            return local.AsMemory(0, Length);
        }
    }

    public Span<T> Span
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var local = array ?? ThrowDisposed();
            return local.AsSpan(0, Length);
        }
    }

    public void Dispose()
    {
        // CON-01: Lock-free run-once guard (makes double and concurrent Dispose harmless)
        var toReturn = Interlocked.Exchange(ref array, null);
        if (toReturn is not null)
        {
            ArrayPool<T>.Shared.Return(toReturn, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T[] ThrowDisposed() => throw new ObjectDisposedException(nameof(MemoryOwner<T>));
}
