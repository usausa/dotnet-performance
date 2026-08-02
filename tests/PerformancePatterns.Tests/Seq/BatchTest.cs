namespace PerformancePatterns.Tests.Seq;

using PerformancePatterns.Seq;

using Xunit;

public sealed class BatchTest
{
    [Theory]
    [InlineData(10, 3)]
    [InlineData(10, 5)]
    [InlineData(10, 10)]
    [InlineData(10, 100)]
    [InlineData(1, 1)]
    public void ArrayBatchMatchesLinqChunk(int count, int size)
    {
        var source = Enumerable.Range(0, count).ToArray();
        var expected = source.Chunk(size).ToArray();

        var actual = new List<int[]>();
        foreach (var segment in source.Batch(size))
        {
            actual.Add([.. segment]);
        }

        Assert.Equal(expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i]);
        }
    }

    [Fact]
    public void SpanBatchProducesSameChunks()
    {
        var source = Enumerable.Range(0, 10).ToArray();
        var expected = source.Chunk(4).ToArray();

        var index = 0;
        foreach (var chunk in source.AsSpan().Batch(4))
        {
            Assert.Equal(expected[index], chunk.ToArray());
            index++;
        }

        Assert.Equal(expected.Length, index);
    }

    [Fact]
    public void SegmentsShareSourceArray()
    {
        var source = new[] { 1, 2, 3, 4 };
        foreach (var segment in source.Batch(2))
        {
            // Must be a view over the source array, not a copy
            Assert.Same(source, segment.Array);
        }
    }

    [Fact]
    public void EmptySourceYieldsNothing()
    {
        var count = 0;
        foreach (var segment in Array.Empty<int>().Batch(3))
        {
            count += segment.Count;
            count++;
        }

        foreach (var chunk in ReadOnlySpan<int>.Empty.Batch(3))
        {
            count += chunk.Length;
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public void TailChunkIsShorter()
    {
        var source = Enumerable.Range(0, 7).ToArray();
        var sizes = new List<int>();
        foreach (var segment in source.Batch(3))
        {
            sizes.Add(segment.Count);
        }

        Assert.Equal([3, 3, 1], sizes);
    }

    [Fact]
    public void InvalidSizeThrows()
    {
        var source = new[] { 1 };
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Batch(0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            foreach (var chunk in source.AsSpan().Batch(-1))
            {
                _ = chunk.Length;
            }
        });
    }
}
