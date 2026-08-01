namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー④: pinned バッファ(POH)
// 問い: 都度 fixed でピン止めするコストと、POH 常駐バッファのポインタ直接利用の差。
// および POH 確保自体のコスト(通常確保比)。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class PinnedArrayBenchmark
{
    private const int Size = 4096;

    private byte[] normalBuffer = default!;

    private byte[] pinnedBuffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        normalBuffer = new byte[Size];
        pinnedBuffer = GC.AllocateArray<byte>(Size, pinned: true);
    }

    [Benchmark(Baseline = true)]
    public unsafe int PinWithFixed()
    {
        fixed (byte* p = normalBuffer)
        {
            p[0] = 1;
            p[Size - 1] = 2;
            return p[0] + p[Size - 1];
        }
    }

    [Benchmark]
    public unsafe int PinnedPointerDirect()
    {
        // POH 上の配列は移動しないため fixed なしでポインタ利用できる
        var p = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(pinnedBuffer));
        p[0] = 1;
        p[Size - 1] = 2;
        return p[0] + p[Size - 1];
    }

    [Benchmark]
    public int AllocateNormal()
    {
        var buffer = new byte[Size];
        buffer[0] = 1;
        return buffer[0] + buffer[^1];
    }

    [Benchmark]
    public int AllocatePinned()
    {
        var buffer = GC.AllocateArray<byte>(Size, pinned: true);
        buffer[0] = 1;
        return buffer[0] + buffer[^1];
    }
}
