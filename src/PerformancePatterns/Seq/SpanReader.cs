namespace PerformancePatterns.Seq;

using System.Runtime.CompilerServices;

/// <summary>
/// SEQ-01: Span を逐次的に読み取る軽量 ref struct カーソル。
/// 位置管理を構造体が担うため、呼び出し側での offset 手動管理が不要になる。
/// unmanaged 型の読み取りは <c>ReadUnmanaged&lt;TValue&gt;()</c> 拡張(byte 特化)を使用する。
/// </summary>
public ref struct SpanReader<T>
{
    private readonly ReadOnlySpan<T> source;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanReader(ReadOnlySpan<T> source)
    {
        this.source = source;
    }

    public int Position { get; private set; }

    public readonly int Remaining => source.Length - Position;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly T Read()
    {
        var index = Position;
        Position++;
        return ref source[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> Read(int length)
    {
        var result = source.Slice(Position, length);
        Position += length;
        return result;
    }
}
