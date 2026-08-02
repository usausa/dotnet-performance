namespace PerformancePatterns.Tests.Buf;

using PerformancePatterns.Buf;

using Xunit;

public sealed class MemoryOwnerTest
{
    [Fact]
    public void AllocateProvidesExactLength()
    {
        // 要求長がプールの丸め(2 の累乗)に一致しないサイズで確認
        using var owner = MemoryOwner<byte>.Allocate(1000);

        Assert.Equal(1000, owner.Length);
        Assert.Equal(1000, owner.Span.Length);
        Assert.Equal(1000, owner.Memory.Length);
    }

    [Fact]
    public void SpanAndMemoryShareStorage()
    {
        using var owner = MemoryOwner<int>.Allocate(16);
        owner.Span[3] = 42;

        Assert.Equal(42, owner.Memory.Span[3]);
    }

    [Fact]
    public void RoundTripThroughMemory()
    {
        using var owner = MemoryOwner<byte>.Allocate(64);
        for (var i = 0; i < owner.Length; i++)
        {
            owner.Span[i] = (byte)i;
        }

        var memory = owner.Memory;
        for (var i = 0; i < memory.Length; i++)
        {
            Assert.Equal((byte)i, memory.Span[i]);
        }
    }

    [Fact]
    public void AccessAfterDisposeThrows()
    {
        var owner = MemoryOwner<byte>.Allocate(16);
        owner.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = owner.Span);
        Assert.Throws<ObjectDisposedException>(() => _ = owner.Memory);
    }

    [Fact]
    public void DoubleDisposeIsHarmless()
    {
        var owner = MemoryOwner<byte>.Allocate(16);
        owner.Dispose();
        owner.Dispose();
    }

    [Fact]
    public void ZeroLengthIsSupported()
    {
        using var owner = MemoryOwner<byte>.Allocate(0);

        Assert.Equal(0, owner.Length);
        Assert.True(owner.Span.IsEmpty);
    }
}
