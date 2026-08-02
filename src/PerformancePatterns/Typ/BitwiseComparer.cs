namespace PerformancePatterns.Typ;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// TYP-02: Comparer that decides equality, ordering and hash codes for unmanaged value types over the raw bytes.
/// It ignores any custom Equals and decides on the bit pattern, delegating to the SIMD-optimized
/// SequenceEqual / SequenceCompareTo.
/// A struct containing padding can be reported as unequal even when logically equal, because the padding is
/// uninitialized, so use this only with types whose layout has no padding.
/// </summary>
public sealed class BitwiseComparer<T> : IEqualityComparer<T>, IComparer<T>
    where T : unmanaged
{
    public static BitwiseComparer<T> Instance { get; } = new();

    private BitwiseComparer()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(T x, T y) => AsBytes(ref x).SequenceEqual(AsBytes(ref y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(T obj)
    {
        var hash = default(HashCode);
        hash.AddBytes(AsBytes(ref obj));
        return hash.ToHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(T x, T y) => AsBytes(ref x).SequenceCompareTo(AsBytes(ref y));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> AsBytes(ref T value) =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
}
