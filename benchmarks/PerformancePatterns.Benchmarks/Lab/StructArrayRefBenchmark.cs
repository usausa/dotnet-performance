namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-02 study: an array of class elements vs an array of struct elements (copy / ref access)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StructArrayRefBenchmark
{
    private ClassEntry[] classEntries = default!;

    private StructEntry[] structEntries = default!;

    [GlobalSetup]
    public void Setup()
    {
        classEntries = new ClassEntry[1024];
        structEntries = new StructEntry[1024];
        for (var i = 0; i < 1024; i++)
        {
            classEntries[i] = new ClassEntry { A = i, B = i * 2 };
            structEntries[i] = new StructEntry { A = i, B = i * 2 };
        }
    }

    [Benchmark(Baseline = true)]
    public long ClassArray()
    {
        var total = 0L;
        foreach (var entry in classEntries)
        {
            total += entry.A + entry.B;
        }

        return total;
    }

    [Benchmark]
    public long StructArrayCopy()
    {
        var total = 0L;
        for (var i = 0; i < structEntries.Length; i++)
        {
            var entry = structEntries[i];   // 16-byte value copy
            total += entry.A + entry.B;
        }

        return total;
    }

    [Benchmark]
    public long StructArrayRef()
    {
        var total = 0L;
        for (var i = 0; i < structEntries.Length; i++)
        {
            ref var entry = ref structEntries[i];   // Reference access with no copy
            total += entry.A + entry.B;
        }

        return total;
    }
}

internal sealed class ClassEntry
{
    public long A { get; set; }

    public long B { get; set; }
}

internal struct StructEntry
{
    public long A;

    public long B;
}
