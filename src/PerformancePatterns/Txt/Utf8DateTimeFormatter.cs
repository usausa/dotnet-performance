namespace PerformancePatterns.Txt;

using System.Runtime.CompilerServices;

/// <summary>
/// TXT-01: 2 桁ルックアップテーブルによる固定書式(yyyyMMddHHmmss)の UTF-8 整形。
/// 00〜99 の ASCII 表現を事前計算テーブルから 2 バイトずつコピーするため、
/// 除算 1 回 + テーブルコピーのみで各フィールドを整形できる。
/// </summary>
public static class Utf8DateTimeFormatter
{
    public const int FormattedLength = 14;

    private static readonly byte[] DigitTable = CreateDigitTable();

    public static bool TryFormat(DateTime value, Span<byte> destination, out int written)
    {
        if (destination.Length < FormattedLength)
        {
            written = 0;
            return false;
        }

        var year = value.Year;
        Write2(destination, year / 100);
        Write2(destination.Slice(2, 2), year % 100);
        Write2(destination.Slice(4, 2), value.Month);
        Write2(destination.Slice(6, 2), value.Day);
        Write2(destination.Slice(8, 2), value.Hour);
        Write2(destination.Slice(10, 2), value.Minute);
        Write2(destination.Slice(12, 2), value.Second);
        written = FormattedLength;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Write2(Span<byte> destination, int value)
        => DigitTable.AsSpan(value * 2, 2).CopyTo(destination);

    private static byte[] CreateDigitTable()
    {
        var table = new byte[100 * 2];
        for (var i = 0; i < 100; i++)
        {
            table[i * 2] = (byte)('0' + (i / 10));
            table[(i * 2) + 1] = (byte)('0' + (i % 10));
        }

        return table;
    }
}
