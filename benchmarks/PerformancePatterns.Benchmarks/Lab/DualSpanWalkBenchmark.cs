namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-01 検証: 複数 Span の同時走査(単一 Span では標準 for が最適 = R-02)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class DualSpanWalkBenchmark
{
    private int[] first = default!;

    private int[] second = default!;

    [GlobalSetup]
    public void Setup()
    {
        first = new int[1024];
        second = new int[1024];
        for (var i = 0; i < 1024; i++)
        {
            first[i] = i;
            second[i] = i * 3;
        }
    }

    // 素朴形: second[i] 側の境界チェックが残る
    [Benchmark(Baseline = true)]
    public long Indexed()
    {
        ReadOnlySpan<int> a = first;
        ReadOnlySpan<int> b = second;
        var total = 0L;
        for (var i = 0; i < a.Length; i++)
        {
            total += a[i] + b[i];
        }

        return total;
    }

    // 事前スライスで両方の境界チェックを除去させる(安全な代替)
    [Benchmark]
    public long IndexedPreSliced()
    {
        ReadOnlySpan<int> a = first;
        var b = second.AsSpan(0, a.Length);
        var total = 0L;
        for (var i = 0; i < a.Length; i++)
        {
            total += a[i] + b[i];
        }

        return total;
    }

    // 手動 ref 走査(MEM-01 の本命形)
    [Benchmark]
    public long RefWalk()
    {
        ReadOnlySpan<int> a = first;
        ReadOnlySpan<int> b = second;
        ref var ra = ref MemoryMarshal.GetReference(a);
        ref var rb = ref MemoryMarshal.GetReference(b);
        var total = 0L;
        for (var i = 0; i < a.Length; i++)
        {
            total += Unsafe.Add(ref ra, i) + Unsafe.Add(ref rb, i);
        }

        return total;
    }
}
