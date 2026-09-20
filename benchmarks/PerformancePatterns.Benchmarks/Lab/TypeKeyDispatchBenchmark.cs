namespace PerformancePatterns.Benchmarks.Lab;

using System.Collections.Concurrent;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TYP-07 follow-up, part 2: TypeKeyBenchmark measures the lookup alone. A registry looks up and then *dispatches on
// the result* (invokes a factory delegate, calls an interface), and with several types alternating the key type
// changed the picture on Zen 3: ConcurrentDictionary<RuntimeTypeHandle, _> lost to <Type, _> by 1.4-1.9x although
// its lookup alone is 0.80x. Keys that avoid the identity-hash FCall (IntPtr = TypeHandle.Value, or Type with a
// TypeHandle.Value comparer) showed no such penalty. This is the shape of ServiceRegistry.Activate in
// BunnyTail.DependencyInjection, where the regression was found.
//
//   Type         - ConcurrentDictionary<Type, Func<object>>, default comparer (identity hash via virtual call)
//   Handle       - ConcurrentDictionary<RuntimeTypeHandle, Func<object>> (identity hash via the value-type path)
//   Ptr          - ConcurrentDictionary<IntPtr, Func<object>> keyed by TypeHandle.Value (arithmetic hash, but the
//                  table no longer references the Type: unusable with collectible AssemblyLoadContexts)
//   TypeComparer - ConcurrentDictionary<Type, Func<object>> with a class comparer hashing TypeHandle.Value >> 3
//
//   Rotating: 5 distinct types in sequence, so the delegate call has 5 targets. Single: one type 5 times.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TypeKeyDispatchBenchmark
{
    private static readonly Type[] Types = [typeof(Target1), typeof(Target2), typeof(Target3), typeof(Target4), typeof(Target5)];

    private ConcurrentDictionary<Type, Func<object>> byType = default!;
    private ConcurrentDictionary<RuntimeTypeHandle, Func<object>> byHandle = default!;
    private ConcurrentDictionary<IntPtr, Func<object>> byPtr = default!;
    private ConcurrentDictionary<Type, Func<object>> byTypeComparer = default!;

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
        byTypeComparer = new(TypeHandleComparer.Instance);
        for (var i = 0; i < Types.Length; i++)
        {
            byType[Types[i]] = factories[i];
            byHandle[Types[i].TypeHandle] = factories[i];
            byPtr[Types[i].TypeHandle.Value] = factories[i];
            byTypeComparer[Types[i]] = factories[i];
        }
    }

    //--------------------------------------------------------------------------------
    // Rotating
    //--------------------------------------------------------------------------------

    [Benchmark(Baseline = true, OperationsPerInvoke = 5)]
    public object? TypeRotating()
    {
        object? last = null;
        var map = byType;
        foreach (var type in Types)
        {
            if (map.TryGetValue(type, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = 5)]
    public object? HandleRotating()
    {
        object? last = null;
        var map = byHandle;
        foreach (var type in Types)
        {
            if (map.TryGetValue(type.TypeHandle, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = 5)]
    public object? PtrRotating()
    {
        object? last = null;
        var map = byPtr;
        foreach (var type in Types)
        {
            if (map.TryGetValue(type.TypeHandle.Value, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = 5)]
    public object? TypeComparerRotating()
    {
        object? last = null;
        var map = byTypeComparer;
        foreach (var type in Types)
        {
            if (map.TryGetValue(type, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    //--------------------------------------------------------------------------------
    // Single
    //--------------------------------------------------------------------------------

    [Benchmark(OperationsPerInvoke = 5)]
    public object? TypeSingle()
    {
        object? last = null;
        var map = byType;
        var type = Types[0];
        for (var i = 0; i < 5; i++)
        {
            if (map.TryGetValue(type, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = 5)]
    public object? HandleSingle()
    {
        object? last = null;
        var map = byHandle;
        var type = Types[0];
        for (var i = 0; i < 5; i++)
        {
            if (map.TryGetValue(type.TypeHandle, out var factory))
            {
                last = factory();
            }
        }

        return last;
    }

    // Every map must hold every key, the rotating variants must end on Target5 and the single ones on Target1.
    public static void Verify()
    {
        var benchmark = new TypeKeyDispatchBenchmark();
        benchmark.Setup();
        foreach (var type in Types)
        {
            if (!benchmark.byType.ContainsKey(type) ||
                !benchmark.byHandle.ContainsKey(type.TypeHandle) ||
                !benchmark.byPtr.ContainsKey(type.TypeHandle.Value) ||
                !benchmark.byTypeComparer.ContainsKey(type))
            {
                throw new InvalidOperationException("Verify failed. TypeKeyDispatch keys");
            }
        }

        if ((benchmark.TypeRotating() is not Target5) ||
            (benchmark.HandleRotating() is not Target5) ||
            (benchmark.PtrRotating() is not Target5) ||
            (benchmark.TypeComparerRotating() is not Target5) ||
            (benchmark.TypeSingle() is not Target1) ||
            (benchmark.HandleSingle() is not Target1))
        {
            throw new InvalidOperationException("Verify failed. TypeKeyDispatch");
        }
    }

    private sealed class TypeHandleComparer : IEqualityComparer<Type>
    {
        public static readonly TypeHandleComparer Instance = new();

        public bool Equals(Type? x, Type? y) => ReferenceEquals(x, y);

        // Handles are pointer-aligned, so the low 3 bits are always zero: shift them out
        public int GetHashCode(Type obj) => (int)((ulong)obj.TypeHandle.Value.ToInt64() >> 3);
    }

    private sealed class Target1;

    private sealed class Target2;

    private sealed class Target3;

    private sealed class Target4;

    private sealed class Target5;
}
