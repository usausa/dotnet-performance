namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// SEQ-06 study: what a Stream subclass pays for the members it does not override
// Question: Stream's default implementations are written for a blocking device - ReadByte/WriteByte allocate a
// one-byte array per call, CopyTo rents a staging buffer and copies twice, and the async members push the synchronous
// work through the thread pool behind a semaphore. For a memory-backed stream, how much do the overrides recover?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public sealed class StreamOverrideBenchmark : IDisposable
{
    private const int PayloadSize = 64 * 1024;

    private const int Operations = 1000;

    private const int AsyncOperations = 100;

    private const int ChunkSize = 64;

    private readonly byte[] chunk = new byte[ChunkSize];

    private DefaultMemoryStream defaultStream = default!;

    private OverridingMemoryStream overridingStream = default!;

    [GlobalSetup]
    public void Setup()
    {
        defaultStream = new DefaultMemoryStream(PayloadSize);
        overridingStream = new OverridingMemoryStream(PayloadSize);
        for (var i = 0; i < chunk.Length; i++)
        {
            chunk[i] = unchecked((byte)i);
        }

        defaultStream.SetLength(PayloadSize);
        overridingStream.SetLength(PayloadSize);
    }

    [GlobalCleanup]
    public void Cleanup() => Dispose();

    public void Dispose()
    {
        defaultStream?.Dispose();
        overridingStream?.Dispose();
    }

    // ❌ Stream.ReadByte allocates new byte[1] per call
    [Benchmark(Baseline = true, OperationsPerInvoke = Operations)]
    public int ReadByteDefault() => ReadBytes(defaultStream);

    // ✅ Read the byte straight out of the backing array
    [Benchmark(OperationsPerInvoke = Operations)]
    public int ReadByteOverride() => ReadBytes(overridingStream);

    // ❌ Stream.WriteByte allocates a one-element array per call
    [Benchmark(OperationsPerInvoke = Operations)]
    public long WriteByteDefault() => WriteBytes(defaultStream);

    // ✅ Write through the span path
    [Benchmark(OperationsPerInvoke = Operations)]
    public long WriteByteOverride() => WriteBytes(overridingStream);

    // ❌ Stream.CopyTo rents a staging buffer and copies the content twice
    [Benchmark]
    public long CopyToDefault() => CopyAll(defaultStream);

    // ✅ Write once from the backing array
    [Benchmark]
    public long CopyToOverride() => CopyAll(overridingStream);

    // ❌ The default async members run the synchronous work on the thread pool
    [Benchmark(OperationsPerInvoke = AsyncOperations)]
    public Task<long> WriteAsyncDefault() => WriteAsyncLoop(defaultStream, chunk);

    // ✅ Complete synchronously and return a completed task
    [Benchmark(OperationsPerInvoke = AsyncOperations)]
    public Task<long> WriteAsyncOverride() => WriteAsyncLoop(overridingStream, chunk);

    [Benchmark(OperationsPerInvoke = AsyncOperations)]
    public Task<long> FlushAsyncDefault() => FlushAsyncLoop(defaultStream);

    [Benchmark(OperationsPerInvoke = AsyncOperations)]
    public Task<long> FlushAsyncOverride() => FlushAsyncLoop(overridingStream);

    public static void Verify()
    {
        using var benchmark = new StreamOverrideBenchmark();
        benchmark.Setup();
        if ((benchmark.ReadByteDefault() != benchmark.ReadByteOverride()) ||
            (benchmark.WriteByteDefault() != benchmark.WriteByteOverride()) ||
            (benchmark.CopyToDefault() != benchmark.CopyToOverride()) ||
            (benchmark.WriteAsyncDefault().GetAwaiter().GetResult() != benchmark.WriteAsyncOverride().GetAwaiter().GetResult()) ||
            (benchmark.FlushAsyncDefault().GetAwaiter().GetResult() != benchmark.FlushAsyncOverride().GetAwaiter().GetResult()))
        {
            throw new InvalidOperationException("Verify failed. StreamOverride");
        }

        // The overrides must also agree byte for byte with the default implementations
        using var expected = new MemoryStream();
        using var actual = new MemoryStream();
        using var source = new DefaultMemoryStream(ChunkSize * 4);
        using var overriding = new OverridingMemoryStream(ChunkSize * 4);
        for (var i = 0; i < ChunkSize * 4; i++)
        {
            source.WriteByte(unchecked((byte)i));
            overriding.WriteByte(unchecked((byte)i));
        }

        source.Position = 0;
        overriding.Position = 0;
        source.CopyTo(expected);
        overriding.CopyTo(actual);
        if (!expected.ToArray().AsSpan().SequenceEqual(actual.ToArray()))
        {
            throw new InvalidOperationException("Verify failed. StreamOverride content");
        }

        // Reading past the end must report -1 from both forms
        source.Position = source.Length;
        overriding.Position = overriding.Length;
        if ((source.ReadByte() != -1) || (overriding.ReadByte() != -1))
        {
            throw new InvalidOperationException("Verify failed. StreamOverride end of stream");
        }
    }

    private static int ReadBytes(Stream stream)
    {
        stream.Position = 0;
        var total = 0;
        for (var i = 0; i < Operations; i++)
        {
            total += stream.ReadByte();
        }

        return total;
    }

    private static long WriteBytes(Stream stream)
    {
        stream.Position = 0;
        for (var i = 0; i < Operations; i++)
        {
            stream.WriteByte(unchecked((byte)i));
        }

        return stream.Position;
    }

    private static long CopyAll(Stream source)
    {
        source.Position = 0;
        using var destination = new MemoryStream(PayloadSize);
        source.CopyTo(destination);
        return destination.Length;
    }

    private static async Task<long> WriteAsyncLoop(Stream stream, byte[] chunk)
    {
        stream.Position = 0;
        for (var i = 0; i < AsyncOperations; i++)
        {
            await stream.WriteAsync(chunk.AsMemory()).ConfigureAwait(false);
        }

        return stream.Position;
    }

    private static async Task<long> FlushAsyncLoop(Stream stream)
    {
        for (var i = 0; i < AsyncOperations; i++)
        {
            await stream.FlushAsync().ConfigureAwait(false);
        }

        return stream.Position;
    }
}

internal abstract class MemoryBackedStream : Stream
{
    private readonly byte[] store;

    protected MemoryBackedStream(int capacity)
    {
        store = new byte[capacity];
    }

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => Written;

    public override long Position
    {
        get => Offset;
        set => Offset = (int)value;
    }

    protected Span<byte> Raw => store.AsSpan();

    protected int Written { get; set; }

    protected int Offset { get; set; }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var available = Math.Min(Written - Offset, buffer.Length);
        if (available <= 0)
        {
            return 0;
        }

        store.AsSpan(Offset, available).CopyTo(buffer);
        Offset += available;
        return available;
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        buffer.CopyTo(store.AsSpan(Offset));
        Advance(buffer.Length);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        Offset = origin switch
        {
            SeekOrigin.Begin => (int)offset,
            SeekOrigin.Current => Offset + (int)offset,
            _ => Written + (int)offset,
        };
        return Offset;
    }

    public override void SetLength(long value) => Written = (int)value;

    protected void Advance(int count)
    {
        Offset += count;
        if (Offset > Written)
        {
            Written = Offset;
        }
    }
}

// Overrides only the abstract members, so every convenience member falls back to Stream's default implementation
internal sealed class DefaultMemoryStream(int capacity) : MemoryBackedStream(capacity)
{
}

// Overrides the members MemoryStream also overrides
internal sealed class OverridingMemoryStream(int capacity) : MemoryBackedStream(capacity)
{
    public override int ReadByte()
    {
        if (Offset >= Written)
        {
            return -1;
        }

        var value = Raw[Offset];
        Offset++;
        return value;
    }

    public override void WriteByte(byte value)
    {
        Raw[Offset] = value;
        Advance(1);
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        var remaining = Written - Offset;
        if (remaining <= 0)
        {
            return;
        }

        var origin = Offset;
        Offset = Written;
        destination.Write(Raw.Slice(origin, remaining));
    }

    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        CopyTo(destination, bufferSize);
        return Task.CompletedTask;
    }

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<int>(cancellationToken)
            : Task.FromResult(Read(buffer.AsSpan(offset, count)));

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? ValueTask.FromCanceled<int>(cancellationToken)
            : new ValueTask<int>(Read(buffer.Span));

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        Write(buffer.AsSpan(offset, count));
        return Task.CompletedTask;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled(cancellationToken);
        }

        Write(buffer.Span);
        return ValueTask.CompletedTask;
    }
}
