namespace PerformancePatterns.Typ;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// TYP-01: 型ごとに採番した静的スロットで、Type キーの辞書を配列インデクスアクセスに置き換える。
/// 型引数が既知のパス(<see cref="TryGetValue{T}"/>)はハッシュ計算・衝突解決なしの添字アクセスになる。
/// エントリは struct 配列 + ref アクセス(MEM-04)、成長は copy-on-write。
/// </summary>
public sealed class TypeMap<TValue>
{
#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    // 実行時 Type からスロットを引くフォールバック(AOT 非互換な MakeGenericType を使わない)
    private readonly Dictionary<Type, int> slotOfType = [];

    private Entry[] entries = [];

    public int Count { get; private set; }

    /// <summary>型引数が既知の高速パス。JIT はスロット番号を定数として扱える。</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue<T>([MaybeNullWhen(false)] out TValue value)
        => TryGetBySlot(TypeSlot<T>.Index, out value);

    /// <summary>実行時に型が決まる場合のフォールバックパス。</summary>
    public bool TryGetValue(Type type, [MaybeNullWhen(false)] out TValue value)
    {
        int slot;
        lock (sync)
        {
            if (!slotOfType.TryGetValue(type, out slot))
            {
                value = default;
                return false;
            }
        }

        return TryGetBySlot(slot, out value);
    }

    public void Set<T>(TValue value)
    {
        lock (sync)
        {
            var slot = TypeSlot<T>.Index;
            slotOfType[typeof(T)] = slot;

            var current = entries;
            if (slot >= current.Length)
            {
                // copy-on-write: 読み取り側は差し替え前の配列を見続けても安全
                var next = new Entry[CalculateSize(slot)];
                current.AsSpan().CopyTo(next);
                current = next;
            }

            ref var entry = ref current[slot];
            if (!entry.HasValue)
            {
                Count++;
            }

            entry.Value = value;
            entry.HasValue = true;

            Volatile.Write(ref entries, current);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetBySlot(int slot, [MaybeNullWhen(false)] out TValue value)
    {
        var current = Volatile.Read(ref entries);
        if ((uint)slot < (uint)current.Length)
        {
            // MEM-04: struct 要素を ref で受けてコピーを避ける
            ref var entry = ref current[slot];
            if (entry.HasValue)
            {
                value = entry.Value!;
                return true;
            }
        }

        value = default;
        return false;
    }

    // 8 刻みで確保する(スロットは密に採番されるため 2 倍成長よりメモリ効率が良い)
    private static int CalculateSize(int slot) => ((slot >> 3) << 3) + 8;

    private struct Entry
    {
        public TValue? Value;

        public bool HasValue;
    }
}
