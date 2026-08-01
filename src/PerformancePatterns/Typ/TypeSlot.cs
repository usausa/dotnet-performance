namespace PerformancePatterns.Typ;

/// <summary>
/// TYP-01: 型ごとに一意な連番(スロット番号)を採番する。
/// </summary>
public static class TypeSlot
{
    private static int nextIndex = -1;

    public static int Allocate() => Interlocked.Increment(ref nextIndex);

    public static int AllocatedCount => Volatile.Read(ref nextIndex) + 1;
}

/// <summary>
/// TYP-01: 型 <typeparamref name="T"/> のスロット番号。static 初期化で型ごとに一度だけ採番される。
/// </summary>
public static class TypeSlot<T>
{
    public static readonly int Index = TypeSlot.Allocate();
}
