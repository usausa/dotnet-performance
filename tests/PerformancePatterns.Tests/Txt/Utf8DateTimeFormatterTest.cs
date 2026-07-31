namespace PerformancePatterns.Tests.Txt;

using System.Globalization;
using System.Text;

using PerformancePatterns.Txt;

using Xunit;

public sealed class Utf8DateTimeFormatterTest
{
    [Theory]
    [InlineData(1, 1, 1, 0, 0, 0)]
    [InlineData(999, 12, 31, 23, 59, 59)]
    [InlineData(2000, 2, 29, 12, 0, 0)]
    [InlineData(2026, 8, 1, 1, 2, 3)]
    [InlineData(9999, 12, 31, 23, 59, 59)]
    public void ParityWithToString(int year, int month, int day, int hour, int minute, int second)
    {
        var value = new DateTime(year, month, day, hour, minute, second);
        AssertParity(value);
    }

    [Fact]
    public void ParityOverTimeSweep()
    {
        foreach (var hour in new[] { 0, 9, 10, 23 })
        {
            foreach (var minute in new[] { 0, 9, 10, 59 })
            {
                foreach (var second in new[] { 0, 9, 10, 59 })
                {
                    AssertParity(new DateTime(2026, 8, 1, hour, minute, second));
                }
            }
        }
    }

    [Fact]
    public void TooSmallDestinationReturnsFalse()
    {
        Span<byte> buffer = stackalloc byte[Utf8DateTimeFormatter.FormattedLength - 1];

        Assert.False(Utf8DateTimeFormatter.TryFormat(DateTime.UtcNow, buffer, out var written));
        Assert.Equal(0, written);
    }

    private static void AssertParity(DateTime value)
    {
        Span<byte> buffer = stackalloc byte[Utf8DateTimeFormatter.FormattedLength];

        Assert.True(Utf8DateTimeFormatter.TryFormat(value, buffer, out var written));
        Assert.Equal(Utf8DateTimeFormatter.FormattedLength, written);
        Assert.Equal(value.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture), Encoding.ASCII.GetString(buffer));
    }
}
