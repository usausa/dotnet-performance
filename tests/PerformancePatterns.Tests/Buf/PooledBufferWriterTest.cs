namespace PerformancePatterns.Tests.Buf;

using System.Buffers.Binary;

using PerformancePatterns.Buf;

using Xunit;

public sealed class PooledBufferWriterTest
{
    [Fact]
    public void WriteAndReadBack()
    {
        using var writer = new PooledBufferWriter<byte>();

        var span = writer.GetSpan(sizeof(int));
        BinaryPrimitives.WriteInt32LittleEndian(span, 12345);
        writer.Advance(sizeof(int));

        span = writer.GetSpan(sizeof(long));
        BinaryPrimitives.WriteInt64LittleEndian(span, 987654321L);
        writer.Advance(sizeof(long));

        Assert.Equal(12, writer.WrittenCount);
        Assert.Equal(12345, BinaryPrimitives.ReadInt32LittleEndian(writer.WrittenSpan));
        Assert.Equal(987654321L, BinaryPrimitives.ReadInt64LittleEndian(writer.WrittenSpan[4..]));
    }

    [Fact]
    public void GrowsBeyondInitialCapacity()
    {
        using var writer = new PooledBufferWriter<byte>(16);

        for (var i = 0; i < 100; i++)
        {
            var span = writer.GetSpan(8);
            span[..8].Fill((byte)i);
            writer.Advance(8);
        }

        Assert.Equal(800, writer.WrittenCount);
        Assert.Equal(0, writer.WrittenSpan[0]);
        Assert.Equal(99, writer.WrittenSpan[^1]);
    }

    [Fact]
    public void ClearAllowsReuse()
    {
        using var writer = new PooledBufferWriter<byte>();
        writer.GetSpan(4)[..4].Fill(1);
        writer.Advance(4);

        writer.Clear();
        Assert.Equal(0, writer.WrittenCount);

        writer.GetSpan(2)[..2].Fill(2);
        writer.Advance(2);
        Assert.True(writer.WrittenSpan is [2, 2]);
    }

    [Fact]
    public void WorksAsIBufferWriter()
    {
        using var writer = new PooledBufferWriter<char>();
        IBufferWriterWrite(writer, "hello");
        IBufferWriterWrite(writer, " world");

        Assert.True(writer.WrittenSpan is "hello world");

        static void IBufferWriterWrite(System.Buffers.IBufferWriter<char> writer, string value)
        {
            var span = writer.GetSpan(value.Length);
            value.CopyTo(span);
            writer.Advance(value.Length);
        }
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        var writer = new PooledBufferWriter<byte>();
        writer.GetSpan(4);
        writer.Advance(2);
        writer.Dispose();
        writer.Dispose();

        Assert.Equal(0, writer.WrittenCount);
    }
}
