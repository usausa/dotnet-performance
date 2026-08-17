namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;
using System.IO.Pipelines;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// SEQ-05 study: reading an unknown-length stream.
// MemoryStream accumulation + ToArray (grow-copy chain) vs pooled-chunk ReadOnlySequence segments (one copy, no LOH)
// vs PipeReader (ASY-03), which is the shape SEQ-05 actually recommends.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StreamSequenceBenchmark
{
    private const int DataSize = 256 * 1024;

    private const int ChunkSize = 32 * 1024;

    private readonly ReusableSequenceBuilder builder = new();

    private byte[] sourceData = default!;

    [GlobalSetup]
    public void Setup()
    {
        sourceData = new byte[DataSize];
        var seed = 55555u;
        for (var i = 0; i < sourceData.Length; i++)
        {
            sourceData[i] = (byte)NextRandom(ref seed);
        }
    }

    [Benchmark(Baseline = true)]
    public long MemoryStreamToArray()
    {
        using var source = new MemoryStream(sourceData);
        using var destination = new MemoryStream();
        source.CopyTo(destination);
        var array = destination.ToArray();

        long total = 0;
        foreach (var value in array)
        {
            total += value;
        }

        return total;
    }

    [Benchmark]
    public long PooledSegments()
    {
        using var source = new MemoryStream(sourceData);

        while (true)
        {
            var chunk = ArrayPool<byte>.Shared.Rent(ChunkSize);
            var filled = 0;
            int read;
            while ((filled < chunk.Length) && ((read = source.Read(chunk, filled, chunk.Length - filled)) > 0))
            {
                filled += read;
            }

            if (filled == 0)
            {
                ArrayPool<byte>.Shared.Return(chunk);
                break;
            }

            builder.Add(chunk, filled);
            if (filled < chunk.Length)
            {
                break;
            }
        }

        var sequence = builder.Build();
        long total = 0;
        foreach (var memory in sequence)
        {
            foreach (var value in memory.Span)
            {
                total += value;
            }
        }

        builder.Reset();
        return total;
    }

    [Benchmark]
    public async Task<long> PipeReaderSequence()
    {
        using var source = new MemoryStream(sourceData);
        var reader = PipeReader.Create(source, new StreamPipeReaderOptions(bufferSize: ChunkSize));

        long total = 0;
        while (true)
        {
            var result = await reader.ReadAsync().ConfigureAwait(false);
            var sequence = result.Buffer;
            foreach (var memory in sequence)
            {
                foreach (var value in memory.Span)
                {
                    total += value;
                }
            }

            reader.AdvanceTo(sequence.End);
            if (result.IsCompleted)
            {
                break;
            }
        }

        await reader.CompleteAsync().ConfigureAwait(false);
        return total;
    }

    public static void Verify()
    {
        var benchmark = new StreamSequenceBenchmark();
        benchmark.Setup();

        var viaArray = benchmark.MemoryStreamToArray();
        var viaSegments = benchmark.PooledSegments();
        var viaPipe = benchmark.PipeReaderSequence().GetAwaiter().GetResult();
        if ((viaArray != viaSegments) || (viaArray != viaPipe) || (viaArray == 0))
        {
            throw new InvalidOperationException($"Stream sum variants disagree: {viaArray} / {viaSegments} / {viaPipe}.");
        }
    }

    private static uint NextRandom(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }
}

// SEQ-05 candidate: builds a multi-segment ReadOnlySequence over ArrayPool chunks.
// Segment objects are reused across Reset cycles; backing arrays are returned to the pool.
public sealed class ReusableSequenceBuilder
{
    private readonly List<SequenceSegment> segments = [];

    private int count;

    public void Add(byte[] buffer, int length)
    {
        SequenceSegment segment;
        if (count < segments.Count)
        {
            segment = segments[count];
        }
        else
        {
            segment = new SequenceSegment();
            segments.Add(segment);
        }

        count++;

        segment.SetMemory(buffer, length);
        if (count > 1)
        {
            segments[count - 2].LinkNext(segment);
        }
    }

    public ReadOnlySequence<byte> Build()
    {
        if (count == 0)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        var first = segments[0];
        var last = segments[count - 1];
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    public void Reset()
    {
        for (var i = 0; i < count; i++)
        {
            segments[i].Release();
        }

        count = 0;
    }

    private sealed class SequenceSegment : ReadOnlySequenceSegment<byte>
    {
        private byte[]? buffer;

        public void SetMemory(byte[] rented, int length)
        {
            buffer = rented;
            Memory = new ReadOnlyMemory<byte>(rented, 0, length);
            RunningIndex = 0;
            Next = null;
        }

        public void LinkNext(SequenceSegment next)
        {
            Next = next;
            next.RunningIndex = RunningIndex + Memory.Length;
        }

        public void Release()
        {
            var toReturn = buffer;
            if (toReturn is not null)
            {
                buffer = null;
                ArrayPool<byte>.Shared.Return(toReturn);
            }

            Memory = default;
            Next = null;
        }
    }
}
