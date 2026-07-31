namespace PerformancePatterns.Seq;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// SEQ-01: byte カーソルに対する unmanaged 型の読み書き。
/// メモリレイアウトをそのまま読み書きするため、エンディアン・パディングは呼び出し側の設計で保証する(SEQ-03 と同様)。
/// </summary>
public static class SpanReaderWriterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue ReadUnmanaged<TValue>(this ref SpanReader<byte> reader)
        where TValue : unmanaged
    {
        var span = reader.Read(Unsafe.SizeOf<TValue>());
        return Unsafe.ReadUnaligned<TValue>(ref MemoryMarshal.GetReference(span));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteUnmanaged<TValue>(this ref SpanWriter<byte> writer, TValue value)
        where TValue : unmanaged
    {
        var span = writer.Slide(Unsafe.SizeOf<TValue>());
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(span), value);
    }
}
