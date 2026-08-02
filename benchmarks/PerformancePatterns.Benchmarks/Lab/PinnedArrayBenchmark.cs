namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 4: pinned buffers (POH)
// Question: what is the difference between pinning with fixed on every call and using a pointer into a POH-resident buffer directly,
// and what does the POH allocation itself cost (compared with a normal allocation)?
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
        // An array on the POH never moves, so a pointer can be used without fixed
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
