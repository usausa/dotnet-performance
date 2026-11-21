namespace PerformancePatterns.Benchmarks.Lab;

using System.Reflection;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// GEN-03 study: reflection invocation without the argument array
// Question: GEN-01 (Emit) is unavailable under Native AOT, so the AOT path keeps calling ConstructorInfo.Invoke /
// PropertyInfo.SetValue, which allocate an object?[] per call (24 + 8n bytes, and SetValue allocates one inside the
// runtime on AOT and Mono). ConstructorInvoker / MethodInvoker (.NET 8+) and CreateDelegate<T> both avoid it - at
// what cost per call?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ReflectionConstructBenchmark
{
    private static readonly ConstructorInfo Constructor = typeof(AccessorTarget).GetConstructor([typeof(int)])!;

    private static readonly ConstructorInvoker Invoker = ConstructorInvoker.Create(Constructor);

    private static readonly Func<int, AccessorTarget> TypedFactory = typeof(AccessorTarget)
        .GetMethod(nameof(AccessorTarget.Create), BindingFlags.Public | BindingFlags.Static)!
        .CreateDelegate<Func<int, AccessorTarget>>();

    // Boxed once so that the measured difference is the argument array, not the boxing of the value
    private static readonly object BoxedValue = 42;

    // ❌ An object?[] per call, and a thrown exception is wrapped in TargetInvocationException
    [Benchmark(Baseline = true)]
    public object InvokeWithArgumentArray() => Constructor.Invoke([BoxedValue]);

    // ✅ No array - the invoker takes up to 4 arguments directly, or a Span beyond that
    [Benchmark]
    public object InvokeWithConstructorInvoker() => Invoker.Invoke(BoxedValue)!;

    // ✅ A typed delegate built once - nothing is boxed either
    [Benchmark]
    public object InvokeWithTypedDelegate() => TypedFactory(42);

    public static void Verify()
    {
        var benchmark = new ReflectionConstructBenchmark();
        var created = new[]
        {
            benchmark.InvokeWithArgumentArray(),
            benchmark.InvokeWithConstructorInvoker(),
            benchmark.InvokeWithTypedDelegate(),
        };
        foreach (var value in created)
        {
            if (value is not AccessorTarget { Value: 42 })
            {
                throw new InvalidOperationException("Verify failed. ReflectionConstruct");
            }
        }

        // The argument array path wraps the callee exception, the invoker path does not
        var throwing = typeof(ThrowingTarget).GetConstructor([typeof(int)])!;
        try
        {
            throwing.Invoke([BoxedValue]);
            throw new InvalidOperationException("Verify failed. ReflectionConstruct wrapping");
        }
        catch (TargetInvocationException)
        {
            // Expected
        }

        try
        {
            ConstructorInvoker.Create(throwing).Invoke(BoxedValue);
            throw new InvalidOperationException("Verify failed. ReflectionConstruct unwrapped");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected: the invoker lets the original exception through
        }
    }
}

// GEN-03 study: property accessors through reflection
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ReflectionAccessorBenchmark
{
    private static readonly PropertyInfo Property = typeof(AccessorTarget).GetProperty(nameof(AccessorTarget.Value))!;

    private static readonly MethodInvoker SetterInvoker = MethodInvoker.Create(Property.SetMethod!);

    private static readonly Action<AccessorTarget, int> TypedSetter = Property.SetMethod!.CreateDelegate<Action<AccessorTarget, int>>();

    private static readonly object BoxedValue = 42;

    private readonly AccessorTarget target = new();

    // ❌ SetValue builds the argument array inside the runtime (CoreCLR elides it, Native AOT and Mono do not)
    [Benchmark(Baseline = true)]
    public int SetWithSetValue()
    {
        Property.SetValue(target, BoxedValue);
        return target.Value;
    }

    // ✅ MethodInvoker takes the argument directly
    [Benchmark]
    public int SetWithMethodInvoker()
    {
        SetterInvoker.Invoke(target, BoxedValue);
        return target.Value;
    }

    // ✅ A typed delegate built once - no boxing at all
    [Benchmark]
    public int SetWithTypedDelegate()
    {
        TypedSetter(target, 42);
        return target.Value;
    }

    public static void Verify()
    {
        var benchmark = new ReflectionAccessorBenchmark();
        if ((benchmark.SetWithSetValue() != 42) ||
            (benchmark.SetWithMethodInvoker() != 42) ||
            (benchmark.SetWithTypedDelegate() != 42))
        {
            throw new InvalidOperationException("Verify failed. ReflectionAccessor");
        }
    }
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ReflectionGetterBenchmark
{
    private static readonly PropertyInfo Property = typeof(AccessorTarget).GetProperty(nameof(AccessorTarget.Value))!;

    private static readonly MethodInvoker GetterInvoker = MethodInvoker.Create(Property.GetMethod!);

    private static readonly Func<AccessorTarget, int> TypedGetter = Property.GetMethod!.CreateDelegate<Func<AccessorTarget, int>>();

    private readonly AccessorTarget target = new(42);

    // ❌ GetValue boxes the value it returns, and builds an argument array inside the runtime on AOT and Mono
    [Benchmark(Baseline = true)]
    public int GetWithGetValue() => (int)Property.GetValue(target)!;

    // ✅ MethodInvoker takes no argument array, but still boxes the value
    [Benchmark]
    public int GetWithMethodInvoker() => (int)GetterInvoker.Invoke(target)!;

    // ✅ A typed delegate built once - no boxing at all
    [Benchmark]
    public int GetWithTypedDelegate() => TypedGetter(target);

    public static void Verify()
    {
        var benchmark = new ReflectionGetterBenchmark();
        if ((benchmark.GetWithGetValue() != 42) ||
            (benchmark.GetWithMethodInvoker() != 42) ||
            (benchmark.GetWithTypedDelegate() != 42))
        {
            throw new InvalidOperationException("Verify failed. ReflectionGetter");
        }
    }
}

public sealed class AccessorTarget
{
    public AccessorTarget()
    {
    }

    public AccessorTarget(int value)
    {
        Value = value;
    }

    public int Value { get; set; }

    public static AccessorTarget Create(int value) => new(value);
}

public sealed class ThrowingTarget
{
    public ThrowingTarget(int value) => throw new ArgumentOutOfRangeException(nameof(value));
}
