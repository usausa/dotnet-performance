namespace PerformancePatterns.Typ;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// TYP-02: unmanaged 値型の等値・順序・ハッシュを生バイト列で行う比較子。
/// カスタム Equals を無視してビットパターンで判定でき、SIMD 最適化された
/// SequenceEqual / SequenceCompareTo に処理を委譲する。
/// パディングを含む構造体は未初期化パディングにより「論理的に等しいのに不一致」と
/// なりうるため、パディングのないレイアウトの型に限定して使用する。
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
