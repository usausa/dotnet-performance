namespace PerformancePatterns.Tests.Typ;

using PerformancePatterns.Typ;

using Xunit;

public sealed class TypeMapTest
{
    [Fact]
    public void GenericPathRoundTrip()
    {
        var map = new TypeMap<string>();
        map.Set<int>("int");
        map.Set<string>("string");

        Assert.True(map.TryGetValue<int>(out var intValue));
        Assert.Equal("int", intValue);
        Assert.True(map.TryGetValue<string>(out var stringValue));
        Assert.Equal("string", stringValue);
        Assert.Equal(2, map.Count);
    }

    [Fact]
    public void RuntimeTypePathMatchesGenericPath()
    {
        var map = new TypeMap<int>();
        map.Set<TypeMapTest>(42);

        // Resolution from a Type known only at run time (looks up the same slot as the generic overload)
        var runtimeType = GetRuntimeType();

        Assert.True(map.TryGetValue<TypeMapTest>(out var byGeneric));
        Assert.True(map.TryGetValue(runtimeType, out var byType));
        Assert.Equal(byGeneric, byType);
        Assert.Equal(42, byType);
    }

    [Fact]
    public void UnregisteredTypeReturnsFalse()
    {
        var map = new TypeMap<string>();
        map.Set<int>("int");

        var unknownType = Guid.Empty.GetType();

        Assert.False(map.TryGetValue<Guid>(out var byGeneric));
        Assert.Null(byGeneric);
        Assert.False(map.TryGetValue(unknownType, out var byType));
        Assert.Null(byType);
    }

    [Fact]
    public void OverwriteKeepsCount()
    {
        var map = new TypeMap<string>();
        map.Set<long>("first");
        map.Set<long>("second");

        Assert.True(map.TryGetValue<long>(out var value));
        Assert.Equal("second", value);
        Assert.Equal(1, map.Count);
    }

    [Fact]
    public void GrowsBeyondInitialSlots()
    {
        // Register many types to exercise the array growth path
        var map = new TypeMap<int>();
        map.Set<byte>(0);
        map.Set<sbyte>(1);
        map.Set<short>(2);
        map.Set<ushort>(3);
        map.Set<uint>(4);
        map.Set<ulong>(5);
        map.Set<float>(6);
        map.Set<double>(7);
        map.Set<decimal>(8);
        map.Set<DateTime>(9);

        Assert.Equal(10, map.Count);
        Assert.True(map.TryGetValue<byte>(out var first));
        Assert.Equal(0, first);
        Assert.True(map.TryGetValue<DateTime>(out var last));
        Assert.Equal(9, last);
    }

    [Fact]
    public void SlotIsStablePerType()
    {
        Assert.Equal(TypeSlot<TimeSpan>.Index, TypeSlot<TimeSpan>.Index);
        Assert.NotEqual(TypeSlot<TimeSpan>.Index, TypeSlot<DateTimeOffset>.Index);
        Assert.True(TypeSlot.AllocatedCount > 0);
    }

    private static Type GetRuntimeType() => typeof(TypeMapTest);
}
