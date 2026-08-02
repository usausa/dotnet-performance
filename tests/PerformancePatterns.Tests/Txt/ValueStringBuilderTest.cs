namespace PerformancePatterns.Tests.Txt;

using System.Text;

using PerformancePatterns.Txt;

using Xunit;

public sealed class ValueStringBuilderTest
{
    [Fact]
    public void ParityWithStringBuilder()
    {
        var expected = new StringBuilder();
        using var builder = new ValueStringBuilder(stackalloc char[128]);

        for (var i = 0; i < 10; i++)
        {
            var part = "part" + i;
            expected.Append(part);
            expected.Append(':');
            builder.Append(part);
            builder.Append(':');
        }

        Assert.Equal(expected.ToString(), builder.ToStringAndClear());
    }

    [Fact]
    public void GrowFromStackToPool()
    {
        // Append far more than the 8-character initial buffer to exercise the Grow path
        var expected = new StringBuilder();
        using var builder = new ValueStringBuilder(stackalloc char[8]);

        for (var i = 0; i < 100; i++)
        {
            expected.Append('x');
            expected.Append("abc");
            builder.Append('x');
            builder.Append("abc");
        }

        Assert.Equal(400, builder.Length);
        Assert.Equal(expected.ToString(), builder.ToStringAndClear());
    }

    [Fact]
    public void PooledConstructorWorks()
    {
        using var builder = new ValueStringBuilder(64);
        builder.Append("hello");
        builder.Append(' ');
        builder.Append("world");

        Assert.Equal("hello world", builder.ToStringAndClear());
    }

    [Fact]
    public void AsSpanExposesCurrentContent()
    {
        using var builder = new ValueStringBuilder(stackalloc char[16]);
        builder.Append("abc");

        Assert.True(builder.AsSpan() is "abc");
    }

    [Fact]
    public void ToStringAndClearResetsState()
    {
        using var builder = new ValueStringBuilder(stackalloc char[16]);
        builder.Append("abc");

        Assert.Equal("abc", builder.ToStringAndClear());
        Assert.Equal(0, builder.Length);
        Assert.Equal(string.Empty, builder.ToStringAndClear());
    }

    [Fact]
    public void EmptyAppendIsSupported()
    {
        using var builder = new ValueStringBuilder(stackalloc char[4]);
        builder.Append(string.Empty);
        builder.Append([]);

        Assert.Equal(string.Empty, builder.ToStringAndClear());
    }
}
