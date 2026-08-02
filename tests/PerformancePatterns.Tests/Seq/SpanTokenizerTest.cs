namespace PerformancePatterns.Tests.Seq;

using PerformancePatterns.Seq;

using Xunit;

public sealed class SpanTokenizerTest
{
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("a,b,c")]
    [InlineData(",")]
    [InlineData("a,")]
    [InlineData(",a")]
    [InlineData("a,,b")]
    [InlineData(",,")]
    public void ParityWithStringSplit(string input)
    {
        Assert.Equal(input.Split(','), Tokenize(input));
    }

    [Fact]
    public void ParityWithStringSplitExhaustive()
    {
        // Verify agreement with string.Split for every {'a', ','} combination of length 0 to 10
        Span<char> buffer = stackalloc char[10];
        for (var length = 0; length <= buffer.Length; length++)
        {
            for (var bits = 0; bits < (1 << length); bits++)
            {
                for (var i = 0; i < length; i++)
                {
                    buffer[i] = ((bits >> i) & 1) != 0 ? ',' : 'a';
                }

                var input = new string(buffer[..length]);
                Assert.Equal(input.Split(','), Tokenize(input));
            }
        }
    }

    [Fact]
    public void TokenizeNonCharSpan()
    {
        ReadOnlySpan<int> source = [1, 2, 0, 3, 0, 0, 4];

        var result = new List<int[]>();
        foreach (var token in source.Tokenize(0))
        {
            result.Add(token.ToArray());
        }

        Assert.Equal(4, result.Count);
        Assert.Equal([1, 2], result[0]);
        Assert.Equal([3], result[1]);
        Assert.Empty(result[2]);
        Assert.Equal([4], result[3]);
    }

    [Fact]
    public void ForeachRestartsFromInitialState()
    {
        var tokenizer = "a,b,c".Tokenize(',');
        var first = Collect(tokenizer);
        var second = Collect(tokenizer);

        Assert.Equal(["a", "b", "c"], first);
        Assert.Equal(first, second);

        static List<string> Collect(SpanTokenizer<char> tokenizer)
        {
            var tokens = new List<string>();
            foreach (var token in tokenizer)
            {
                tokens.Add(token.ToString());
            }

            return tokens;
        }
    }

    private static List<string> Tokenize(string input)
    {
        var tokens = new List<string>();
        foreach (var token in input.Tokenize(','))
        {
            tokens.Add(token.ToString());
        }

        return tokens;
    }
}
