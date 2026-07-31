namespace PerformancePatterns.Seq;

using System.Runtime.CompilerServices;

public static class SpanTokenizerExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanTokenizer<T> Tokenize<T>(this ReadOnlySpan<T> source, T separator)
        where T : IEquatable<T>
        => new(source, separator);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanTokenizer<char> Tokenize(this string source, char separator)
        => new(source.AsSpan(), separator);
}
