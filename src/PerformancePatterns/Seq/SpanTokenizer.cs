namespace PerformancePatterns.Seq;

using System.Runtime.CompilerServices;

/// <summary>
/// SEQ-02: Zero-allocation tokenizer that splits a span of any IEquatable type on a separator element.
/// A combined example of STK-01 (ref struct), STK-03 (struct iterator) and JIT-02 (IEquatable constraint).
/// Split semantics match string.Split(separator): empty tokens are included, and empty input yields a single empty token.
/// </summary>
public ref struct SpanTokenizer<T>
    where T : IEquatable<T>
{
    private readonly ReadOnlySpan<T> source;

    private readonly T separator;

    private int tokenStart;

    private int tokenLength;

    private int nextStart;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanTokenizer(ReadOnlySpan<T> source, T separator)
    {
        this.source = source;
        this.separator = separator;
        tokenStart = 0;
        tokenLength = 0;
        nextStart = 0;
    }

    public readonly ReadOnlySpan<T> Current
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => source.Slice(tokenStart, tokenLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly SpanTokenizer<T> GetEnumerator() => this;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool MoveNext()
    {
        var start = nextStart;
        if (start > source.Length)
        {
            return false;
        }

        // JIT-02: The IEquatable<T> constraint makes IndexOf resolve to a type-specialized implementation (SIMD for primitives)
        var index = source[start..].IndexOf(separator);
        tokenStart = start;
        if (index < 0)
        {
            tokenLength = source.Length - start;
            nextStart = source.Length + 1;
        }
        else
        {
            tokenLength = index;
            nextStart = start + index + 1;
        }

        return true;
    }
}
