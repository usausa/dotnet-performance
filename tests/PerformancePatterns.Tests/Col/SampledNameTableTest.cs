namespace PerformancePatterns.Tests.Col;

using PerformancePatterns.Col;

using Xunit;

public sealed class SampledNameTableTest
{
    [Fact]
    public void LookupMatchesDictionary()
    {
        var source = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Id"] = 1,
            ["Name"] = 2,
            ["CreatedAt"] = 3,
            ["UpdatedAt"] = 4,
            ["Flag"] = 5,
        };
        var table = new SampledNameTable<int>(source);

        Assert.Equal(source.Count, table.Count);
        foreach (var pair in source)
        {
            Assert.True(table.TryGetValue(pair.Key.AsSpan(), out var value));
            Assert.Equal(pair.Value, value);
        }
    }

    [Fact]
    public void UnknownKeyReturnsFalse()
    {
        var table = new SampledNameTable<int>(new Dictionary<string, int>(StringComparer.Ordinal) { ["Id"] = 1 });

        Assert.False(table.TryGetValue("Ix".AsSpan(), out var value));
        Assert.Equal(0, value);
        Assert.False(table.TryGetValue([], out _));
    }

    [Fact]
    public void CollidingKeysAreResolvedByFullComparison()
    {
        // Keys whose length and first / middle / last characters are identical, so their hashes collide
        Assert.Equal(
            SampledNameTable<int>.CalculateHash("AxxxBxxxC".AsSpan()),
            SampledNameTable<int>.CalculateHash("AyyyByyyC".AsSpan()));

        var table = new SampledNameTable<int>(new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["AxxxBxxxC"] = 1,
            ["AyyyByyyC"] = 2,
        });

        Assert.True(table.TryGetValue("AxxxBxxxC".AsSpan(), out var first));
        Assert.Equal(1, first);
        Assert.True(table.TryGetValue("AyyyByyyC".AsSpan(), out var second));
        Assert.Equal(2, second);
    }

    [Fact]
    public void LookupIsCaseSensitive()
    {
        var table = new SampledNameTable<int>(new Dictionary<string, int>(StringComparer.Ordinal) { ["Name"] = 1 });

        Assert.True(table.TryGetValue("Name".AsSpan(), out _));
        Assert.False(table.TryGetValue("name".AsSpan(), out _));
    }

    [Fact]
    public void WorksWithNonInternedProbes()
    {
        var names = new[] { "Alpha", "Beta", "Gamma", "Delta", "Epsilon" };
        var table = new SampledNameTable<string>(names.Select(static x => new KeyValuePair<string, string>(x, x)));

        foreach (var name in names)
        {
            // Runtime-built copy (confirms the lookup does not rely on reference equality)
            var probe = new string(name.AsSpan());
            Assert.True(table.TryGetValue(probe.AsSpan(), out var value));
            Assert.Equal(name, value);
        }
    }

    [Fact]
    public void EmptySourceIsSupported()
    {
        var table = new SampledNameTable<int>([]);

        Assert.Equal(0, table.Count);
        Assert.False(table.TryGetValue("Id".AsSpan(), out _));
    }
}
