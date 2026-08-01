namespace PerformancePatterns.Benchmarks.Seq;

using System.Buffers.Binary;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Seq;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SpanReaderBenchmark
{
    private const int EntryCount = 16;

    private byte[] packet = default!;

    [GlobalSetup]
    public void Setup()
    {
        // uint magic + int count + count 件の (long id, int value)
        packet = CreatePacket(EntryCount);
    }

    public static byte[] CreatePacket(int count)
    {
        var buffer = new byte[sizeof(uint) + sizeof(int) + (count * (sizeof(long) + sizeof(int)))];
        var offset = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(offset), 0xCAFEBABEu);
        offset += sizeof(uint);
        BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), count);
        offset += sizeof(int);
        for (var i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(buffer.AsSpan(offset), 1000L + i);
            offset += sizeof(long);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(offset), i * 3);
            offset += sizeof(int);
        }

        return buffer;
    }

    [Benchmark(Baseline = true)]
    public long BinaryReaderParse()
    {
        using var stream = new MemoryStream(packet);
        using var reader = new BinaryReader(stream);
        var total = (long)reader.ReadUInt32();
        var count = reader.ReadInt32();
        for (var i = 0; i < count; i++)
        {
            total += reader.ReadInt64();
            total += reader.ReadInt32();
        }

        return total;
    }

    [Benchmark]
    public long ManualOffsetParse()
    {
        var span = packet.AsSpan();
        var offset = 0;
        var total = (long)BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset, sizeof(uint)));
        offset += sizeof(uint);
        var count = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, sizeof(int)));
        offset += sizeof(int);
        for (var i = 0; i < count; i++)
        {
            total += BinaryPrimitives.ReadInt64LittleEndian(span.Slice(offset, sizeof(long)));
            offset += sizeof(long);
            total += BinaryPrimitives.ReadInt32LittleEndian(span.Slice(offset, sizeof(int)));
            offset += sizeof(int);
        }

        return total;
    }

    [Benchmark]
    public long SpanReaderParse()
    {
        // ReadUnmanaged はネイティブエンディアン(x64 = リトルエンディアン)
        var reader = new SpanReader<byte>(packet);
        var total = (long)reader.ReadUnmanaged<uint>();
        var count = reader.ReadUnmanaged<int>();
        for (var i = 0; i < count; i++)
        {
            total += reader.ReadUnmanaged<long>();
            total += reader.ReadUnmanaged<int>();
        }

        return total;
    }
}
