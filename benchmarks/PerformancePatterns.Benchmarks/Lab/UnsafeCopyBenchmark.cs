namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 1: Unsafe.CopyBlockUnaligned
// Question: are there conditions where it beats Span.CopyTo / Array.Copy (variable length and constant length)?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class CopyVariableBenchmark
{
    private byte[] source = default!;

    private byte[] destination = default!;

    [Params(16, 512, 4096)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = new byte[Size];
        destination = new byte[Size];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = (byte)i;
        }
    }

    [Benchmark(Baseline = true)]
    public byte SpanCopyTo()
    {
        source.AsSpan().CopyTo(destination);
        return destination[^1];
    }

    [Benchmark]
    public byte ArrayCopy()
    {
        Array.Copy(source, destination, Size);
        return destination[^1];
    }

    [Benchmark]
    public byte CopyBlockUnaligned()
    {
        Unsafe.CopyBlockUnaligned(
            ref MemoryMarshal.GetArrayDataReference(destination),
            ref MemoryMarshal.GetArrayDataReference(source),
            (uint)Size);
        return destination[^1];
    }
}

// Constant length: the case where the JIT knows the length and can expand the copy into a sequence of movs
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class CopyConstantBenchmark
{
    private const int Size = 16;

    private byte[] source = default!;

    private byte[] destination = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new byte[Size];
        destination = new byte[Size];
        for (var i = 0; i < source.Length; i++)
        {
            source[i] = (byte)(i + 1);
        }
    }

    [Benchmark(Baseline = true)]
    public byte SpanCopyTo16()
    {
        source.AsSpan(0, Size).CopyTo(destination);
        return destination[^1];
    }

    [Benchmark]
    public byte CopyBlockUnaligned16()
    {
        Unsafe.CopyBlockUnaligned(
            ref MemoryMarshal.GetArrayDataReference(destination),
            ref MemoryMarshal.GetArrayDataReference(source),
            Size);
        return destination[^1];
    }
}
