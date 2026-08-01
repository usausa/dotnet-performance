namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー①: Unsafe.CopyBlockUnaligned
// 問い: Span.CopyTo / Array.Copy に対して優位になる条件はあるか(可変長・定数長)。
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

// 定数長: JIT が長さを既知としてコピーを mov 列に展開できるケース
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
