namespace PerformancePatterns.Seq;

using System.Runtime.CompilerServices;

/// <summary>
/// SEQ-01: Span へ逐次的に書き込む軽量 ref struct カーソル。
/// <c>Slide()</c> で書き込み先スライスを先に確保し、後からデータを埋めることができる(長さプレフィックス等)。
/// unmanaged 型の書き込みは <c>WriteUnmanaged&lt;TValue&gt;()</c> 拡張(byte 特化)を使用する。
/// </summary>
public ref struct SpanWriter<T>
{
    private readonly Span<T> destination;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanWriter(Span<T> destination)
    {
        this.destination = destination;
    }

    public int Position { get; private set; }

    public readonly int Remaining => destination.Length - Position;

    public readonly Span<T> Written => destination[..Position];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T value)
    {
        destination[Position] = value;
        Position++;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(ReadOnlySpan<T> values)
    {
        values.CopyTo(destination[Position..]);
        Position += values.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<T> Slide(int length)
    {
        var result = destination.Slice(Position, length);
        Position += length;
        return result;
    }
}
