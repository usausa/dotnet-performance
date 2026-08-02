namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// SEQ-05 study: line splitting for streamed input
// Compares the naive "rescan everything and compact after every line" approach with "incremental search plus lazy compaction".
// (the two-segment wraparound of a ring buffer is not measured — a flat buffer isolates the effect of the incremental search)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RingSplitBenchmark
{
    private const byte Delimiter = (byte)'\n';

    private const int LineLength = 2048;

    private const int LineCount = 16;

    private const int ChunkSize = 256;

    private byte[] source = default!;

    private byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        // 16 lines of 2 KB each (a line spans many chunks, which is where the rescan cost shows up)
        source = new byte[LineLength * LineCount];
        for (var i = 0; i < LineCount; i++)
        {
            var line = source.AsSpan(i * LineLength, LineLength);
            line.Fill((byte)('a' + (i % 26)));
            line[^1] = Delimiter;
        }

        buffer = new byte[LineLength * 2];
    }

    [Benchmark(Baseline = true)]
    public long NaiveRescanCompact()
    {
        var count = 0;
        var total = 0L;
        var offset = 0;
        while (offset < source.Length)
        {
            var chunkLength = Math.Min(ChunkSize, source.Length - offset);
            source.AsSpan(offset, chunkLength).CopyTo(buffer.AsSpan(count));
            count += chunkLength;
            offset += chunkLength;

            // Rescan the whole buffer from the start every time
            int index;
            while ((index = buffer.AsSpan(0, count).IndexOf(Delimiter)) >= 0)
            {
                total += buffer[0] + (long)index;

                // Compact the remainder to the front after every line
                buffer.AsSpan(index + 1, count - index - 1).CopyTo(buffer);
                count -= index + 1;
            }
        }

        return total;
    }

    [Benchmark]
    public long IncrementalDeferredCompact()
    {
        var start = 0;
        var count = 0;
        var search = 0;
        var total = 0L;
        var offset = 0;
        while (offset < source.Length)
        {
            var chunkLength = Math.Min(ChunkSize, source.Length - offset);

            // Compact to the front only when there is not enough free space (lazy compaction)
            if (count + chunkLength > buffer.Length)
            {
                buffer.AsSpan(start, count - start).CopyTo(buffer);
                count -= start;
                search -= start;
                start = 0;
            }

            source.AsSpan(offset, chunkLength).CopyTo(buffer.AsSpan(count));
            count += chunkLength;
            offset += chunkLength;

            // Look only past the position already scanned (incremental search)
            while (true)
            {
                var span = buffer.AsSpan(search, count - search);
                var index = span.IndexOf(Delimiter);
                if (index < 0)
                {
                    search = count;
                    break;
                }

                var lineStart = start;
                var lineEnd = search + index;
                total += buffer[lineStart] + (long)(lineEnd - lineStart);

                start = lineEnd + 1;
                search = start;
            }
        }

        return total;
    }
}
