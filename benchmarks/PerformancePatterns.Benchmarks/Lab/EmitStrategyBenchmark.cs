namespace PerformancePatterns.Benchmarks.Lab;

using System.Reflection.Emit;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// GEN-01 検証: Emit 生成ファクトリのターゲット戦略
// closure 配列(object[] + castclass) vs Holder フィールド直読み、子デリゲート連鎖 vs インライン展開、Callvirt vs Call
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

        // closure 配列ターゲット: ldelem + castclass が毎回入る
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

        // Holder フィールドターゲット: 型付きフィールドの直読み(castclass 不要)
        var holderMethod = new DynamicMethod("CreateByHolder", typeof(object), [typeof(GenHolder)], module, true);
        il = holderMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, GenHolder.DepAField);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldfld, GenHolder.DepBField);
        il.Emit(OpCodes.Newobj, ctor);
        il.Emit(OpCodes.Ret);
        holderFieldFactory = (Func<object>)holderMethod.CreateDelegate(typeof(Func<object>), new GenHolder { DepA = depA, DepB = depB });

        // 子ファクトリ連鎖: 親が子 Func<object> を呼んで castclass する(インライン展開しない場合の形)
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

    // Emit の ldfld ターゲット(コンパイラ生成のバッキングフィールド)。
    // 生成コードでは専用 Holder 型に相当し、プロパティ呼び出しではなくフィールド直読みを測るために使う。
    public static System.Reflection.FieldInfo DepAField { get; } =
        typeof(GenHolder).GetField("<DepA>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;

    public static System.Reflection.FieldInfo DepBField { get; } =
        typeof(GenHolder).GetField("<DepB>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
}
