namespace PerformancePatterns.Tests.Seq;

using System.Buffers.Binary;

using PerformancePatterns.Seq;

using Xunit;

public sealed class SpanReaderWriterTest
{
    [Fact]
    public void RoundTripUnmanagedValues()
    {
        Span<byte> buffer = stackalloc byte[64];
        var writer = new SpanWriter<byte>(buffer);
        writer.WriteUnmanaged(0x12345678u);
        writer.WriteUnmanaged(-12345);
        writer.WriteUnmanaged(1234567890123456789L);
        writer.WriteUnmanaged(3.5);

        Assert.Equal(24, writer.Position);

        var reader = new SpanReader<byte>(writer.Written);
        Assert.Equal(0x12345678u, reader.ReadUnmanaged<uint>());
        Assert.Equal(-12345, reader.ReadUnmanaged<int>());
        Assert.Equal(1234567890123456789L, reader.ReadUnmanaged<long>());
        Assert.Equal(3.5, reader.ReadUnmanaged<double>());
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void ReadUnmanagedMatchesNativeEndianness()
    {
        Span<byte> buffer = stackalloc byte[4];
        var writer = new SpanWriter<byte>(buffer);
        writer.WriteUnmanaged(0x12345678u);

        var expected = BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(buffer)
            : BinaryPrimitives.ReadUInt32BigEndian(buffer);

        var reader = new SpanReader<byte>(buffer);
        Assert.Equal(expected, reader.ReadUnmanaged<uint>());
    }

    [Fact]
    public void ReadSingleReturnsReference()
    {
        ReadOnlySpan<int> source = [10, 20, 30];
        var reader = new SpanReader<int>(source);

        Assert.Equal(10, reader.Read());
        Assert.Equal(20, reader.Read());
        Assert.Equal(1, reader.Remaining);
        Assert.Equal(30, reader.Read());
    }

    [Fact]
    public void ReadSpanAdvancesPosition()
    {
        ReadOnlySpan<byte> source = [1, 2, 3, 4, 5];
        var reader = new SpanReader<byte>(source);

        Assert.True(reader.Read(2) is [1, 2]);
        Assert.Equal(2, reader.Position);
        Assert.True(reader.Read(3) is [3, 4, 5]);
        Assert.Equal(0, reader.Remaining);
    }

    [Fact]
    public void SlideAllowsDeferredFill()
    {
        // 長さプレフィックスを後から埋めるシナリオ
        Span<byte> buffer = stackalloc byte[16];
        var writer = new SpanWriter<byte>(buffer);
        var lengthSlot = writer.Slide(sizeof(int));
        writer.Write("abc"u8);
        BinaryPrimitives.WriteInt32LittleEndian(lengthSlot, writer.Position - sizeof(int));

        var reader = new SpanReader<byte>(writer.Written);
        Assert.Equal(3, BinaryPrimitives.ReadInt32LittleEndian(reader.Read(sizeof(int))));
        Assert.True(reader.Read(3) is [(byte)'a', (byte)'b', (byte)'c']);
    }

    [Fact]
    public void OverrunThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(static () =>
        {
            var reader = new SpanReader<byte>([1, 2]);
            reader.Read(3);
        });

        Assert.Throws<ArgumentOutOfRangeException>(static () =>
        {
            Span<byte> buffer = stackalloc byte[2];
            var writer = new SpanWriter<byte>(buffer);
            writer.WriteUnmanaged(0x12345678u);
        });
    }
}
