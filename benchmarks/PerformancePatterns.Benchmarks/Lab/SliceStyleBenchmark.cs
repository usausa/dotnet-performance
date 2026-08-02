namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-03 study: Slice(offset, length) vs the range operator [a..b]
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SliceStyleBenchmark
{
    private byte[] data = default!;

    [GlobalSetup]
    public void Setup()
    {
        data = new byte[4096];
        for (var i = 0; i < data.Length; i++)
        {
            data[i] = (byte)i;
        }
    }

    [Benchmark(Baseline = true)]
    public long SliceMethod()
    {
        ReadOnlySpan<byte> span = data;
        var total = 0L;
        for (var offset = 0; offset + 16 <= span.Length; offset += 16)
        {
            var chunk = span.Slice(offset, 16);
            total += chunk[0] + chunk[15];
        }

        return total;
    }

    [Benchmark]
    public long RangeOperator()
    {
        ReadOnlySpan<byte> span = data;
        var total = 0L;
        for (var offset = 0; offset + 16 <= span.Length; offset += 16)
        {
            var chunk = span[offset..(offset + 16)];
            total += chunk[0] + chunk[15];
        }

        return total;
    }
}
