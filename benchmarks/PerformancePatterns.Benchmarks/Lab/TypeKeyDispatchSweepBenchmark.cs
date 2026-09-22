namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Concurrent;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-07 follow-up, part 3: how many alternating delegate targets does it take before the RuntimeTypeHandle key
// loses to the Type key? TypeKeyDispatchBenchmark measures one type (handle 0.62x on Zen 3, 0.57x on Zen 5) and
// five rotating types (handle 2.24x on Zen 3, 0.82x on Zen 5). This sweeps the number of distinct targets in the
// same five-call sequence so the point where Zen 3 flips can be located: a flip at two targets points at the
// guarded delegate call itself, a flip only at four or five points at the length of the branch history the
// predictor has to hold. TargetCount 1 and 5 reproduce the Single and Rotating rows of TypeKeyDispatchBenchmark.
//
//   Type   - ConcurrentDictionary<Type, Func<object>>, default comparer
//   Handle - ConcurrentDictionary<RuntimeTypeHandle, Func<object>>
//   Ptr    - ConcurrentDictionary<IntPtr, Func<object>> keyed by TypeHandle.Value (no identity hash at all)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeKeyDispatchSweepBenchmark
{
    private const int CallsPerInvoke = 5;

    private static readonly Type[] AllTypes = [typeof(Target1), typeof(Target2), typeof(Target3), typeof(Target4), typeof(Target5)];

    private Type[] sequence = default!;
    private ConcurrentDictionary<Type, Func<object>> byType = default!;
    private ConcurrentDictionary<RuntimeTypeHandle, Func<object>> byHandle = default!;
    private ConcurrentDictionary<IntPtr, Func<object>> byPtr = default!;

    [Params(1, 2, 3, 5)]
    public int TargetCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Func<object>[] factories =
        [
            static () => new Target1(),
            static () => new Target2(),
            static () => new Target3(),
            static () => new Target4(),
            static () => new Target5()
        ];

        byType = new();
        byHandle = new();
        byPtr = new();
        for (var i = 0; i < AllTypes.Length; i++)
        {
            byType[AllTypes[i]] = factories[i];
            byHandle[AllTypes[i].TypeHandle] = factories[i];
            byPtr[AllTypes[i].TypeHandle.Value] = factories[i];
        }

        // Always five calls per invoke; only the number of distinct targets in the sequence changes
        sequence = new Type[CallsPerInvoke];
        for (var i = 0; i < sequence.Length; i++)
        {
            sequence[i] = AllTypes[i % TargetCount];
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = CallsPerInvoke)]
    public object? TypeRotating()
    {
        object? last = null;
        var map = byType;
        foreach (var type in sequence)
        {
            if (map.TryGetValue(type, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = CallsPerInvoke)]
    public object? HandleRotating()
    {
        object? last = null;
        var map = byHandle;
        foreach (var type in sequence)
        {
            if (map.TryGetValue(type.TypeHandle, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = CallsPerInvoke)]
    public object? PtrRotating()
    {
        object? last = null;
        var map = byPtr;
        foreach (var type in sequence)
        {
            if (map.TryGetValue(type.TypeHandle.Value, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    // For every TargetCount the last call of the sequence hits AllTypes[(5 - 1) % TargetCount]
    public static void Verify()
    {
        foreach (var count in new[] { 1, 2, 3, 5 })
        {
            var benchmark = new TypeKeyDispatchSweepBenchmark { TargetCount = count };
            benchmark.Setup();
            var expected = AllTypes[(CallsPerInvoke - 1) % count];
            if ((benchmark.TypeRotating()?.GetType() != expected) ||
                (benchmark.HandleRotating()?.GetType() != expected) ||
                (benchmark.PtrRotating()?.GetType() != expected))
            {
                throw new InvalidOperationException("Verify failed. TypeKeyDispatchSweep");
            }
        }
    }

    private sealed class Target1;

    private sealed class Target2;

    private sealed class Target3;

    private sealed class Target4;

    private sealed class Target5;
}
