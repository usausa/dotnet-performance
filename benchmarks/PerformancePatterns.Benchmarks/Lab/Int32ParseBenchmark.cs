namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers.Binary;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// R-09 follow-up: an NDepend article (2026) reports that decoding little-endian Int32 from a byte array
// runs at 0.26x (Span) vs 0.19x (pointer) of a byte-by-byte baseline, i.e. pointers ~27% faster than
// Span. R-09's measurements say the opposite: MemoryMarshal.Cast is as fast as fixed or faster. The
// likely explanation is that the article's "Span" variant decodes element by element, so the four
// shapes are measured side by side here over the same 1024 values.
//
//   ByteByByte     - shift/or of four bytes per element (the article's baseline)
//   SpanPerElement - BinaryPrimitives.ReadInt32LittleEndian on a 4-byte slice per element
//   SpanCast       - MemoryMarshal.Cast<byte, int> once, then a plain int loop (R-09's form)
//   Pointer        - fixed + int* walk
//
// Each variant sums the decoded values so Verify can compare them.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class Int32ParseBenchmark
{
    private const int Count = 1024;

    private byte[] bytes = default!;

    [GlobalSetup]
    public void Setup()
    {
        bytes = new byte[Count * sizeof(int)];
        for (var i = 0; i < Count; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(i * sizeof(int)), i * 31);
        }
    }

    [Benchmark(Baseline = true)]
    public long ByteByByte()
    {
        var total = 0L;
        var source = bytes;
        for (var i = 0; i + 3 < source.Length; i += 4)
        {
            var value = source[i] | (source[i + 1] << 8) | (source[i + 2] << 16) | (source[i + 3] << 24);
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long SpanPerElement()
    {
        var total = 0L;
        var span = bytes.AsSpan();
        for (var i = 0; i + 3 < span.Length; i += 4)
        {
            total += BinaryPrimitives.ReadInt32LittleEndian(span.Slice(i, 4));
        }

        return total;
    }

    [Benchmark]
    public long SpanCast()
    {
        var total = 0L;
        var values = MemoryMarshal.Cast<byte, int>(bytes);
        for (var i = 0; i < values.Length; i++)
        {
            total += values[i];
        }

        return total;
    }

    [Benchmark]
    public unsafe long Pointer()
    {
        var total = 0L;
        fixed (byte* p = bytes)
        {
            var values = (int*)p;
            var count = bytes.Length / sizeof(int);
            for (var i = 0; i < count; i++)
            {
                total += values[i];
            }
        }

        return total;
    }
}
