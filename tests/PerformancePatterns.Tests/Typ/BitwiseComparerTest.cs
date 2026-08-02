namespace PerformancePatterns.Tests.Typ;

using PerformancePatterns.Typ;

using Xunit;

public sealed class BitwiseComparerTest
{
    [Fact]
    public void EqualBitsAreEqual()
    {
        var comparer = BitwiseComparer<PackedValue>.Instance;
        var x = new PackedValue(1, 2);
        var y = new PackedValue(1, 2);

        Assert.True(comparer.Equals(x, y));
        Assert.Equal(comparer.GetHashCode(x), comparer.GetHashCode(y));
        Assert.Equal(0, comparer.Compare(x, y));
    }

    [Fact]
    public void DifferentBitsAreNotEqual()
    {
        var comparer = BitwiseComparer<PackedValue>.Instance;

        Assert.False(comparer.Equals(new PackedValue(1, 2), new PackedValue(1, 3)));
    }

    [Fact]
    public void IgnoresLyingCustomEquals()
    {
        // Detects a difference in bit pattern even for a type whose Equals always returns true
        var comparer = BitwiseComparer<AlwaysEqualValue>.Instance;
        var x = new AlwaysEqualValue(1);
        var y = new AlwaysEqualValue(2);

        Assert.True(x.Equals(y));
        Assert.False(comparer.Equals(x, y));
    }

    [Fact]
    public void WorksAsDictionaryComparer()
    {
        var dict = new Dictionary<PackedValue, string>(BitwiseComparer<PackedValue>.Instance)
        {
            [new PackedValue(1, 2)] = "first",
            [new PackedValue(3, 4)] = "second",
        };

        Assert.True(dict.TryGetValue(new PackedValue(3, 4), out var value));
        Assert.Equal("second", value);
        Assert.False(dict.ContainsKey(new PackedValue(3, 5)));
    }

    [Fact]
    public void CompareOrdersByByteSequence()
    {
        var comparer = BitwiseComparer<byte>.Instance;

        Assert.True(comparer.Compare(1, 2) < 0);
        Assert.True(comparer.Compare(2, 1) > 0);
    }

    [Fact]
    public void PrimitiveParityWithDefault()
    {
        var comparer = BitwiseComparer<long>.Instance;

        Assert.True(comparer.Equals(123456789L, 123456789L));
        Assert.False(comparer.Equals(1L, 2L));
    }

    // 16 bytes with no padding
    private readonly record struct PackedValue(long A, long B);

    private readonly struct AlwaysEqualValue(int value) : IEquatable<AlwaysEqualValue>
    {
        public readonly int Value = value;

        public override bool Equals(object? obj) => obj is AlwaysEqualValue;

        // Deliberately lies that values are always equal (used to verify the bitwise comparison ignores it)
        public bool Equals(AlwaysEqualValue other) => (Value & 0) == (other.Value & 0);

        public override int GetHashCode() => 0;
    }
}
