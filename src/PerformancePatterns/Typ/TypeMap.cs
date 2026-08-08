namespace PerformancePatterns.Typ;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// TYP-01: Replaces a Type-keyed dictionary with array index access through a static slot assigned per type.
/// The path where the type argument is known (<see cref="TryGetValue{T}"/>) becomes a plain index access with no hashing and no collision resolution.
/// Entries are a struct array accessed by ref (MEM-02), and every write is copy-on-write.
/// </summary>
public sealed class TypeMap<TValue>
{
#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    // Fallback that resolves a slot from a runtime Type (without the AOT-incompatible MakeGenericType)
    private readonly Dictionary<Type, int> slotOfType = [];

    private Entry[] entries = [];

    public int Count { get; private set; }

    /// <summary>Fast path where the type argument is known. The JIT can treat the slot number as a constant.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetValue<T>([MaybeNullWhen(false)] out TValue value)
        => TryGetBySlot(TypeSlot<T>.Index, out value);

    /// <summary>Fallback path for when the type is only known at run time.</summary>
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

            // copy-on-write: readers can safely keep observing the pre-swap array
            var next = new Entry[slot >= current.Length ? CalculateSize(slot) : current.Length];
            current.AsSpan().CopyTo(next);

            ref var entry = ref next[slot];
            if (!entry.HasValue)
            {
                Count++;
            }

            entry.Value = value;
            entry.HasValue = true;

            Volatile.Write(ref entries, next);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryGetBySlot(int slot, [MaybeNullWhen(false)] out TValue value)
    {
        var current = Volatile.Read(ref entries);
        if ((uint)slot < (uint)current.Length)
        {
            // MEM-02: Take the struct element by ref to avoid a copy
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

    // Grow in steps of 8 (slots are numbered densely, so this is more memory efficient than doubling)
    private static int CalculateSize(int slot) => ((slot >> 3) << 3) + 8;

    private struct Entry
    {
        public TValue? Value;

        public bool HasValue;
    }
}
