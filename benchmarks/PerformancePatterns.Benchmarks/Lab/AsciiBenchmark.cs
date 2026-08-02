namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 3: ASCII-specialized processing (the Ascii class in .NET 8)
// Question: where ASCII input is guaranteed (HTTP header names and the like),
// how should string.Equals(OrdinalIgnoreCase) / Ascii.EqualsIgnoreCase / a handwritten | 0x20 comparison be chosen between?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class AsciiBenchmark
{
    private string[] lowerStrings = default!;

    private string[] mixedStrings = default!;

    private byte[][] lowerBytes = default!;

    private byte[][] mixedBytes = default!;

    [GlobalSetup]
    public void Setup()
    {
        var headers = new[]
        {
            "content-type", "content-length", "accept-encoding", "cache-control",
            "user-agent", "host", "connection", "authorization",
        };

        lowerStrings = new string[headers.Length];
        mixedStrings = new string[headers.Length];
        lowerBytes = new byte[headers.Length][];
        mixedBytes = new byte[headers.Length][];
        for (var i = 0; i < headers.Length; i++)
        {
            var lower = new string(headers[i].AsSpan());
            var chars = lower.ToCharArray();
            for (var j = 0; j < chars.Length; j += 2)
            {
                chars[j] = char.ToUpperInvariant(chars[j]);
            }

            var mixed = new string(chars);
            lowerStrings[i] = lower;
            mixedStrings[i] = mixed;
            lowerBytes[i] = Encoding.ASCII.GetBytes(lower);
            mixedBytes[i] = Encoding.ASCII.GetBytes(mixed);
        }
    }

    [Benchmark(Baseline = true)]
    public int StringEqualsIgnoreCase()
    {
        var count = 0;
        for (var i = 0; i < lowerStrings.Length; i++)
        {
            if (string.Equals(lowerStrings[i], mixedStrings[i], StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int AsciiEqualsIgnoreCase()
    {
        var count = 0;
        for (var i = 0; i < lowerBytes.Length; i++)
        {
            if (Ascii.EqualsIgnoreCase(lowerBytes[i], mixedBytes[i]))
            {
                count++;
            }
        }

        return count;
    }

    [Benchmark]
    public int ManualOr20Compare()
    {
        var count = 0;
        for (var i = 0; i < lowerBytes.Length; i++)
        {
            if (EqualsIgnoreCaseManual(lowerBytes[i], mixedBytes[i]))
            {
                count++;
            }
        }

        return count;
    }

    // | 0x20 normalization, assuming only letters differ in case (limited to closed use cases because '@' and '`' collide)
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool EqualsIgnoreCaseManual(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if ((left[i] | 0x20) != (right[i] | 0x20))
            {
                return false;
            }
        }

        return true;
    }
}
