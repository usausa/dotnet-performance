namespace PerformancePatterns.Buf;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// BUF-04: ArrayPool レンタルに using スコープを付与する IMemoryOwner&lt;T&gt; 実装。
/// 要求長と実際のレンタル長(2 の累乗)の差を隠蔽し、正確な長さの Span / Memory を提供する。
/// 二重 Dispose は Interlocked ガード(CON-02)で無害化。
/// 同期処理内で完結するなら BUF-05(TemporaryBuffer)の方が所有オブジェクトの確保すら不要 —
/// 本型は Memory&lt;T&gt; が必要な非同期 I/O 境界向け。
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
        // CON-02: ロックレスの 1 回実行ガード(二重 Dispose・競合 Dispose を無害化)
        var toReturn = Interlocked.Exchange(ref array, null);
        if (toReturn is not null)
        {
            ArrayPool<T>.Shared.Return(toReturn, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T[] ThrowDisposed() => throw new ObjectDisposedException(nameof(MemoryOwner<T>));
}
