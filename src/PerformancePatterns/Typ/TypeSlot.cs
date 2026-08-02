namespace PerformancePatterns.Typ;

/// <summary>
/// TYP-01: Assigns a unique sequential number (a slot number) to each type.
/// </summary>
public static class TypeSlot
{
    private static int nextIndex = -1;

    public static int Allocate() => Interlocked.Increment(ref nextIndex);

    public static int AllocatedCount => Volatile.Read(ref nextIndex) + 1;
}

/// <summary>
/// TYP-01: Slot number for type <typeparamref name="T"/>. Assigned exactly once per type by the static initializer.
/// </summary>
public static class TypeSlot<T>
{
    public static readonly int Index = TypeSlot.Allocate();
}
