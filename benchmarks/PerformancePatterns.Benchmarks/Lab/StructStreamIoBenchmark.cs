namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// SEQ-03 study: reading and writing a struct array over a Stream — field by field (BinaryReader/Writer) vs bulk reinterpretation
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StructStreamIoBenchmark
{
    private const int Count = 1024;

    private PacketRecord[] source = default!;

    private PacketRecord[] destination = default!;

    private byte[] writeBuffer = default!;

    private byte[] bulkImage = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new PacketRecord[Count];
        destination = new PacketRecord[Count];
        for (var i = 0; i < Count; i++)
        {
            source[i] = new PacketRecord { Id = i, Code = i * 3, Value = i * 7L };
        }

        writeBuffer = new byte[Count * 16];

        // Pre-built image for the read benchmarks
        bulkImage = MemoryMarshal.AsBytes(source.AsSpan()).ToArray();
    }

    [Benchmark(Baseline = true)]
    public long WriteFieldByField()
    {
        using var stream = new MemoryStream(writeBuffer);
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        foreach (ref readonly var record in source.AsSpan())
        {
            writer.Write(record.Id);
            writer.Write(record.Code);
            writer.Write(record.Value);
        }

        return stream.Position;
    }

    [Benchmark]
    public long WriteBulkCast()
    {
        using var stream = new MemoryStream(writeBuffer);
        stream.Write(MemoryMarshal.AsBytes(source.AsSpan()));
        return stream.Position;
    }

    [Benchmark]
    public long ReadFieldByField()
    {
        using var input = new MemoryStream(bulkImage, writable: false);
        using var reader = new BinaryReader(input, System.Text.Encoding.UTF8, leaveOpen: true);
        var span = destination.AsSpan();
        for (var i = 0; i < span.Length; i++)
        {
            span[i].Id = reader.ReadInt32();
            span[i].Code = reader.ReadInt32();
            span[i].Value = reader.ReadInt64();
        }

        return destination[Count - 1].Value;
    }

    [Benchmark]
    public long ReadBulkCast()
    {
        using var input = new MemoryStream(bulkImage, writable: false);
        input.ReadExactly(MemoryMarshal.AsBytes(destination.AsSpan()));
        return destination[Count - 1].Value;
    }
}

// 16-byte transfer record with no padding
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct PacketRecord
{
    public int Id;

    public int Code;

    public long Value;
}
