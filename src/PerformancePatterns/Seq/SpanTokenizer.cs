namespace PerformancePatterns.Seq;

using System.Runtime.CompilerServices;

/// <summary>
/// SEQ-02: 任意の IEquatable な型のスパンを区切り要素で分割するゼロアロケーショントークナイザ。
/// STK-01(ref struct)/ STK-03(struct iterator)/ JIT-02(IEquatable 制約)の複合適用例。
/// 分割セマンティクスは string.Split(separator) と同一(空トークンを含む。空入力は空トークン 1 個)。
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

        // JIT-02: IEquatable<T> 制約により IndexOf は型特化(プリミティブでは SIMD)実装が選択される
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
