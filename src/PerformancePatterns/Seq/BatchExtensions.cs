namespace PerformancePatterns.Seq;

using System.Runtime.CompilerServices;

/// <summary>
/// SEQ-04: Lazily evaluated chunking. Without materializing the whole sequence, a struct enumerator (STK-03)
/// yields fixed-size groups in order with no allocation.
/// The Span overload only slices (no copy) and the array overload returns ArraySegment, so unlike Enumerable.Chunk
/// there is no per-chunk array allocation or copy.
/// </summary>
public static class BatchExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanBatchEnumerable<T> Batch<T>(this ReadOnlySpan<T> source, int size) => new(source, size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanBatchEnumerable<T> Batch<T>(this Span<T> source, int size) => new(source, size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayBatchEnumerable<T> Batch<T>(this T[] source, int size) => new(source, size);

    internal static void ThrowIfInvalidSize(int size)
    {
        if (size < 1)
        {
            Throw();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Throw() => throw new ArgumentOutOfRangeException(nameof(size), "Batch size must be 1 or greater.");
    }
}

/// <summary>
/// Chunking for ReadOnlySpan. A ref struct enumerator that works through foreach duck typing.
/// </summary>
public readonly ref struct SpanBatchEnumerable<T>
{
    private readonly ReadOnlySpan<T> source;

    private readonly int size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanBatchEnumerable(ReadOnlySpan<T> source, int size)
    {
        BatchExtensions.ThrowIfInvalidSize(size);
        this.source = source;
        this.size = size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(source, size);

    public ref struct Enumerator
    {
        private readonly int size;

        private ReadOnlySpan<T> remaining;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ReadOnlySpan<T> source, int size)
        {
            remaining = source;
            this.size = size;
        }

        public ReadOnlySpan<T> Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (remaining.IsEmpty)
            {
                return false;
            }

            var take = Math.Min(size, remaining.Length);
            Current = remaining[..take];
            remaining = remaining[take..];
            return true;
        }
    }
}

/// <summary>
/// Chunking for arrays. Because it returns ArraySegment, a chunk can be used as either a Span or a Memory.
/// </summary>
public readonly struct ArrayBatchEnumerable<T> : IEquatable<ArrayBatchEnumerable<T>>
{
    private readonly T[] source;

    private readonly int size;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArrayBatchEnumerable(T[] source, int size)
    {
        BatchExtensions.ThrowIfInvalidSize(size);
        this.source = source;
        this.size = size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(source, size);

    public bool Equals(ArrayBatchEnumerable<T> other) => ReferenceEquals(source, other.source) && (size == other.size);

    public override bool Equals(object? obj) => obj is ArrayBatchEnumerable<T> other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(source, size);

    public static bool operator ==(ArrayBatchEnumerable<T> left, ArrayBatchEnumerable<T> right) => left.Equals(right);

    public static bool operator !=(ArrayBatchEnumerable<T> left, ArrayBatchEnumerable<T> right) => !left.Equals(right);

    public struct Enumerator : IEquatable<Enumerator>
    {
        private readonly T[] source;

        private readonly int size;

        private int offset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(T[] source, int size)
        {
            this.source = source;
            this.size = size;
        }

        public ArraySegment<T> Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            if (offset >= source.Length)
            {
                return false;
            }

            var take = Math.Min(size, source.Length - offset);
            Current = new ArraySegment<T>(source, offset, take);
            offset += take;
            return true;
        }

        public readonly bool Equals(Enumerator other) =>
            ReferenceEquals(source, other.source) && (size == other.size) && (offset == other.offset) && (Current == other.Current);

        public override readonly bool Equals(object? obj) => obj is Enumerator other && Equals(other);

        public override readonly int GetHashCode() => HashCode.Combine(source, size, offset);

        public static bool operator ==(Enumerator left, Enumerator right) => left.Equals(right);

        public static bool operator !=(Enumerator left, Enumerator right) => !left.Equals(right);
    }
}
