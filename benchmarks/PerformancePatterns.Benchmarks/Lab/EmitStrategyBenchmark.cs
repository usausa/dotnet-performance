namespace PerformancePatterns.Benchmarks.Lab;

using System.Reflection.Emit;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// GEN-01 study: target strategies for an Emit-generated factory
// closure array (object[] + castclass) vs direct Holder field read, chained child delegates vs inline expansion, Callvirt vs Call
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class EmitStrategyBenchmark
{
    private GenDepA depA = default!;

    private GenDepB depB = default!;

    private Func<object> directLambda = default!;

    private Func<object> closureArrayFactory = default!;

    private Func<object> holderFieldFactory = default!;

    private Func<object> chainedCallvirtFactory = default!;

    private Func<object> chainedCallFactory = default!;

    [GlobalSetup]
    public void Setup()
    {
        depA = new GenDepA();
        depB = new GenDepB();

        var a = depA;
        var b = depB;
        directLambda = () => new GenService(a, b);

        var ctor = typeof(GenService).GetConstructor([typeof(GenDepA), typeof(GenDepB)])!;
        var module = typeof(EmitStrategyBenchmark).Module;

        // Closure array target: ldelem + castclass on every call
        var arrayMethod = new DynamicMethod("CreateByArray", typeof(object), [typeof(object[])], module, true);
        var il = arrayMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldelem_Ref);
        il.Emit(OpCodes.Castclass, typeof(GenDepA));
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldelem_Ref);
        il.Emit(OpCodes.Castclass, typeof(GenDepB));
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Ret);
        closureArrayFactory = (Func<object>)arrayMethod.CreateDelegate(typeof(Func<object>), new object[] { depA, depB });

        // Holder field target: a direct read of a typed field (no castclass)
        var holderMethod = new DynamicMethod("CreateByHolder", typeof(object), [typeof(GenHolder)], module, true);
        il = holderMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, GenHolder.DepAField);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, GenHolder.DepBField);
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Ret);
        holderFieldFactory = (Func<object>)holderMethod.CreateDelegate(typeof(Func<object>), new GenHolder { DepA = depA, DepB = depB });

        // Chained child factories: the parent calls a child Func<object> and castclasses the result (the shape without inline expansion)
        var childA = CreateChildFactory(module, GenHolder.DepAField);
        var childB = CreateChildFactory(module, GenHolder.DepBField);
        var holder = new GenHolder { DepA = depA, DepB = depB };
        chainedCallvirtFactory = CreateChainedFactory(module, ctor, [childA.CreateDelegate(typeof(Func<object>), holder), childB.CreateDelegate(typeof(Func<object>), holder)], useCall: false);
        chainedCallFactory = CreateChainedFactory(module, ctor, [childA.CreateDelegate(typeof(Func<object>), holder), childB.CreateDelegate(typeof(Func<object>), holder)], useCall: true);
    }

    [Benchmark(Baseline = true)]
    public object DirectLambda() => directLambda();

    [Benchmark]
    public object EmitHolderField() => holderFieldFactory();

    [Benchmark]
    public object EmitClosureArray() => closureArrayFactory();

    [Benchmark]
    public object EmitChainedCallvirt() => chainedCallvirtFactory();

    [Benchmark]
    public object EmitChainedCall() => chainedCallFactory();

    private static DynamicMethod CreateChildFactory(System.Reflection.Module module, System.Reflection.FieldInfo field)
    {
        var method = new DynamicMethod("CreateChild", typeof(object), [typeof(GenHolder)], module, true);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, field);
        il.Emit(OpCodes.Ret);
        return method;
    }

    private static Func<object> CreateChainedFactory(System.Reflection.Module module, System.Reflection.ConstructorInfo ctor, Delegate[] children, bool useCall)
    {
        var invoke = typeof(Func<object>).GetMethod("Invoke")!;
        var method = new DynamicMethod(useCall ? "CreateChainedCall" : "CreateChainedCallvirt", typeof(object), [typeof(object[])], module, true);
        var il = method.GetILGenerator();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldelem_Ref);
        il.Emit(OpCodes.Castclass, typeof(Func<object>));
        il.Emit(useCall ? OpCodes.Call : OpCodes.Callvirt, invoke);
        il.Emit(OpCodes.Castclass, typeof(GenDepA));

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ldelem_Ref);
        il.Emit(OpCodes.Castclass, typeof(Func<object>));
        il.Emit(useCall ? OpCodes.Call : OpCodes.Callvirt, invoke);
        il.Emit(OpCodes.Castclass, typeof(GenDepB));

        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Ret);
        return (Func<object>)method.CreateDelegate(typeof(Func<object>), children.Cast<object>().ToArray());
    }
}

internal sealed class GenDepA;

internal sealed class GenDepB;

internal sealed class GenService(GenDepA a, GenDepB b)
{
    public GenDepA A { get; } = a;

    public GenDepB B { get; } = b;
}

internal sealed class GenHolder
{
    public GenDepA DepA { get; set; } = default!;

    public GenDepB DepB { get; set; } = default!;

    // The ldfld target for Emit (a compiler-generated backing field).
    // In generated code this corresponds to a dedicated Holder type, and it is used to measure a direct field read rather than a property call.
    public static System.Reflection.FieldInfo DepAField { get; } =
        typeof(GenHolder).GetField("<DepA>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

    public static System.Reflection.FieldInfo DepBField { get; } =
        typeof(GenHolder).GetField("<DepB>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
}
