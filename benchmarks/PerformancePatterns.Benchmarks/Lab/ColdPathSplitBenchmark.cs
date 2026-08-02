namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// JIT-04 検証: 稀パス(成長処理)をホットメソッドに同居させる vs NoInlining で分離する
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ColdPathSplitBenchmark
{
    private FatWriter fat = default!;

    private SplitWriter split = default!;

    [GlobalSetup]
    public void Setup()
    {
        fat = new FatWriter(2048);
        split = new SplitWriter(2048);
    }

    [Benchmark(Baseline = true)]
    public int FatMethod()
    {
        fat.Reset();
        for (var i = 0; i < 1024; i++)
        {
            fat.Write((byte)i);
        }

        return fat.Count;
    }

    [Benchmark]
    public int SplitColdPath()
    {
        split.Reset();
        for (var i = 0; i < 1024; i++)
        {
            split.Write((byte)i);
        }

        return split.Count;
    }
}

// 成長処理をホットメソッド内に同居させた形(メソッドが太り、呼び出し元へのインライン化が阻害される)
internal sealed class FatWriter(int capacity)
{
    private byte[] buffer = new byte[capacity];

    public int Count { get; private set; }

    public void Reset() => Count = 0;

    public void Write(byte value)
    {
        if (Count == buffer.Length)
        {
            var newBuffer = new byte[buffer.Length * 2];
            Array.Copy(buffer, newBuffer, buffer.Length);
            buffer = newBuffer;
            if (buffer.Length > 1 << 30)
            {
                throw new InvalidOperationException("Buffer too large.");
            }
        }

        buffer[Count] = value;
        Count++;
    }
}

// 成長処理を NoInlining の別メソッドへ分離した形(ホットパスが小さくなりインライン化される)
internal sealed class SplitWriter(int capacity)
{
    private byte[] buffer = new byte[capacity];

    public int Count { get; private set; }

    public void Reset() => Count = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(byte value)
    {
        if (Count == buffer.Length)
        {
            Grow();
        }

        buffer[Count] = value;
        Count++;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow()
    {
        var newBuffer = new byte[buffer.Length * 2];
        Array.Copy(buffer, newBuffer, buffer.Length);
        buffer = newBuffer;
        if (buffer.Length > 1 << 30)
        {
            throw new InvalidOperationException("Buffer too large.");
        }
    }
}
