namespace PerformancePatterns.Benchmarks.Lab;

using System.Reflection;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-03 study: accessing a private field (UnsafeAccessor vs reflection)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class UnsafeAccessorBenchmark
{
    private const int N = 100;

    private static readonly FieldInfo CountField =
        typeof(AccessTarget).GetField("count", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private AccessTarget target = default!;

    [GlobalSetup]
    public void Setup() => target = new AccessTarget(42);

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public long PublicProperty()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += target.Count;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long UnsafeAccessorField()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += GetCount(target);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long ReflectionGetValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            // FieldInfo.GetValue boxes a value type before returning it
            total += (int)CountField.GetValue(target)!;
        }

        return total;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "count")]
    private static extern ref int GetCount(AccessTarget instance);
}

// Target type with a private field (standing in for the internal state of an external library).
// Count is the baseline for normal access, and count is the private field the accessors target (held independently)
internal sealed class AccessTarget
{
    private readonly int count;

    public AccessTarget(int value)
    {
        count = value;
        Count = value;
    }

    public int Count { get; }

    // Reads count from C# (the primary use is through UnsafeAccessor / reflection)
    public int GetCountDirect() => count;
}
