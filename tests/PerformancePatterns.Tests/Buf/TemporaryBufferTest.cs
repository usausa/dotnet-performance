namespace PerformancePatterns.Tests.Buf;

using PerformancePatterns.Buf;

using Xunit;

public sealed class TemporaryBufferTest
{
    [Fact]
    public void StackPathProvidesExactLength()
    {
        using var buffer = new TemporaryBuffer<int>(stackalloc int[16], 10);

        Assert.Equal(10, buffer.Span.Length);

        buffer.Span.Fill(123);
        Assert.Equal(123, buffer.Span[0]);
        Assert.Equal(123, buffer.Span[9]);
    }

    [Fact]
    public void PooledPathProvidesExactLength()
    {
        using var buffer = new TemporaryBuffer<int>(1000);

        Assert.Equal(1000, buffer.Span.Length);

        buffer.Span.Fill(456);
        Assert.Equal(456, buffer.Span[0]);
        Assert.Equal(456, buffer.Span[999]);
    }

    [Fact]
    public void ZeroLengthIsSupported()
    {
        using var stack = new TemporaryBuffer<byte>(stackalloc byte[8], 0);
        using var pooled = new TemporaryBuffer<byte>(0);

        Assert.Equal(0, stack.Span.Length);
        Assert.Equal(0, pooled.Span.Length);
    }

    [Fact]
    public void DisposeIsIdempotent()
    {
        var buffer = new TemporaryBuffer<byte>(256);
        buffer.Dispose();
        buffer.Dispose();
    }

    [Theory]
    [InlineData(500)]
    [InlineData(512)]
    [InlineData(520)]
    public void ThresholdSelectionPattern(int size)
    {
        // Verify that the threshold-switching idiom described in the catalog works as written
        using var buffer = size <= 512
            ? new TemporaryBuffer<byte>(stackalloc byte[512], size)
            : new TemporaryBuffer<byte>(size);

        Assert.Equal(size, buffer.Span.Length);
    }
}
