namespace PerformancePatterns.Tests.Buf;

using PerformancePatterns.Buf;

using Xunit;

public sealed class BufferWriterSlimTest
{
    [Fact]
    public void StackOnlyPathKeepsContent()
    {
        using var writer = new BufferWriterSlim<byte>(stackalloc byte[16]);
        writer.Write([1, 2, 3]);
        writer.Write(4);

        Assert.Equal(4, writer.WrittenCount);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void GrowsBeyondInitialBuffer()
    {
        using var writer = new BufferWriterSlim<byte>(stackalloc byte[4]);
        for (var i = 0; i < 100; i++)
        {
            writer.Write((byte)i);
        }

        Assert.Equal(100, writer.WrittenCount);
        for (var i = 0; i < 100; i++)
        {
            Assert.Equal((byte)i, writer.WrittenSpan[i]);
        }
    }

    [Fact]
    public void GrowPreservesExistingContent()
    {
        using var writer = new BufferWriterSlim<int>(stackalloc int[2]);
        writer.Write([10, 20]);
        writer.Write([30, 40, 50]);

        Assert.Equal([10, 20, 30, 40, 50], writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void GetSpanAdvanceProtocol()
    {
        using var writer = new BufferWriterSlim<char>(stackalloc char[8]);
        var span = writer.GetSpan(3);
        Assert.True(span.Length >= 3);
        "abc".CopyTo(span);
        writer.Advance(3);

        var more = writer.GetSpan(10);
        Assert.True(more.Length >= 10);
        "defghijklm".CopyTo(more);
        writer.Advance(10);

        Assert.Equal("abcdefghijklm", new string(writer.WrittenSpan));
    }

    [Fact]
    public void EmptyInitialBufferWorks()
    {
        using var writer = default(BufferWriterSlim<byte>);
        writer.Write([9, 8, 7]);

        Assert.Equal(new byte[] { 9, 8, 7 }, writer.WrittenSpan.ToArray());
    }

    [Fact]
    public void DisposeResetsState()
    {
        var writer = new BufferWriterSlim<byte>(stackalloc byte[4]);
        writer.Write([1, 2, 3, 4, 5]);
        writer.Dispose();

        Assert.Equal(0, writer.WrittenCount);
        Assert.True(writer.WrittenSpan.IsEmpty);
    }
}
