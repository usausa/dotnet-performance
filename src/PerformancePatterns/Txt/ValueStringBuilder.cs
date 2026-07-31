namespace PerformancePatterns.Txt;

using System.Buffers;
using System.Runtime.CompilerServices;

/// <summary>
/// TXT-02: stackalloc 初期バッファ + ArrayPool 拡張による低アロケーション文字列構築。
/// Grow はコールドパスとして NoInlining で分離する(JIT-04)。
/// <code>
/// var builder = new ValueStringBuilder(stackalloc char[128]);
/// builder.Append(name);
/// builder.Append(':');
/// var result = builder.ToStringAndClear();
/// </code>
/// </summary>
public ref struct ValueStringBuilder
{
    private char[]? pooled;

    private Span<char> buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueStringBuilder(Span<char> initialBuffer)
    {
        buffer = initialBuffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueStringBuilder(int initialCapacity)
    {
        pooled = ArrayPool<char>.Shared.Rent(initialCapacity);
        buffer = pooled;
    }

    public int Length { get; private set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<char> AsSpan() => buffer[..Length];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char c)
    {
        var index = Length;
        var current = buffer;
        if ((uint)index < (uint)current.Length)
        {
            current[index] = c;
            Length = index + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        var current = buffer;
        var index = Length;
        if (value.Length <= current.Length - index)
        {
            value.CopyTo(current[index..]);
            Length = index + value.Length;
        }
        else
        {
            GrowAndAppend(value);
        }
    }

    public override readonly string ToString() => new(buffer[..Length]);

    public string ToStringAndClear()
    {
        var result = new string(buffer[..Length]);
        Dispose();
        return result;
    }

    public void Dispose()
    {
        var toReturn = pooled;
        buffer = default;
        Length = 0;
        if (toReturn is not null)
        {
            pooled = null;
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }

    // コールドパス: 分離してホット側の Append をインライン可能に保つ(JIT-04)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        buffer[Length] = c;
        Length++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(ReadOnlySpan<char> value)
    {
        Grow(value.Length);
        value.CopyTo(buffer[Length..]);
        Length += value.Length;
    }

    private void Grow(int additional)
    {
        var required = Length + additional;
        var capacity = Math.Max(required, buffer.Length == 0 ? 16 : buffer.Length * 2);
        var newArray = ArrayPool<char>.Shared.Rent(capacity);
        buffer[..Length].CopyTo(newArray);

        var toReturn = pooled;
        pooled = newArray;
        buffer = newArray;
        if (toReturn is not null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }
}
