namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// JIT-06: static abstract interface members reached through a generic type parameter (T.Method()).
// Coanet (2023) reported that the cost depends on what T is, not on the member being static:
//   value-type T     - exact instantiation, the call is direct and inlinable (same as an instance constrained call)
//   reference-type T - shared canonical code; the target comes from the generic dictionary, so the call is indirect
//                      and cannot be inlined, unless the generic method itself is inlined into a caller that knows
//                      the exact T (then the JIT resolves the target at the inlined site)
// Shapes (all sum Compute(i) / Apply(i) over 1024 values; A adds 1, B adds 2):
//   Direct              - OpA.Compute(i), plain static call (reference)
//   StructSaim          - Saim<AddOp>(i) through a NoInlining generic method: exact code for a struct T
//   ClassSaimInlined    - SaimInlined<OpA>(i), AggressiveInlining generic method called with an exact class T
//   ClassSaimShared     - Saim<OpA>(i) through a NoInlining generic method: the shared __Canon body executes
//   ClassSaimSharedPoly - Saim<OpA> / Saim<OpB> alternating: two instantiations of the same shared body
//   InstanceMono        - op.Apply(i) through a NoInlining helper, one implementation (monomorphic interface call)
//   InstancePoly        - op.Apply(i) alternating between two implementations (polymorphic interface call)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StaticAbstractCallBenchmark
{
    private const int Count = 1024;

    private IOp opA = default!;
    private IOp opB = default!;

    [GlobalSetup]
    public void Setup()
    {
        // Created through a runtime branch so that the JIT cannot see a single possible type statically
        opA = Environment.TickCount >= int.MinValue ? new OpA() : new OpB();
        opB = Environment.TickCount >= int.MinValue ? new OpB() : new OpA();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int Direct()
    {
        var total = 0;
        for (var i = 0; i < Count; i++)
        {
            total += OpA.Compute(i);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int StructSaim()
    {
        var total = 0;
        for (var i = 0; i < Count; i++)
        {
            total += Saim<AddOp>(i);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int ClassSaimInlined()
    {
        var total = 0;
        for (var i = 0; i < Count; i++)
        {
            total += SaimInlined<OpA>(i);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int ClassSaimShared()
    {
        var total = 0;
        for (var i = 0; i < Count; i++)
        {
            total += Saim<OpA>(i);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int ClassSaimSharedPoly()
    {
        var total = 0;
        for (var i = 0; i < Count; i++)
        {
            total += (i & 1) == 0 ? Saim<OpA>(i) : Saim<OpB>(i);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int InstanceMono()
    {
        var total = 0;
        var op = opA;
        for (var i = 0; i < Count; i++)
        {
            total += Apply(op, i);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int InstancePoly()
    {
        var total = 0;
        var a = opA;
        var b = opB;
        for (var i = 0; i < Count; i++)
        {
            total += Apply((i & 1) == 0 ? a : b, i);
        }

        return total;
    }

    // NoInlining keeps the generic body a separate method: exact code for a struct T, shared code for a class T
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Saim<T>(int x)
        where T : IOp
        => T.Compute(x);

    // AggressiveInlining lets the body land in the caller, where T is exact even for a class
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SaimInlined<T>(int x)
        where T : IOp
        => T.Compute(x);

    // The interface call site lives in its own method so that it is the same site for mono and poly
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Apply(IOp op, int x) => op.Apply(x);

    // Mono variants add 1 to every value; the poly variants add 1 to even and 2 to odd values.
    public static void Verify()
    {
        var benchmark = new StaticAbstractCallBenchmark();
        benchmark.Setup();
        const int mono = (Count * (Count - 1) / 2) + Count;
        const int poly = (Count * (Count - 1) / 2) + (Count / 2) + Count;
        if ((benchmark.Direct() != mono) ||
            (benchmark.StructSaim() != mono) ||
            (benchmark.ClassSaimInlined() != mono) ||
            (benchmark.ClassSaimShared() != mono) ||
            (benchmark.InstanceMono() != mono) ||
            (benchmark.ClassSaimSharedPoly() != poly) ||
            (benchmark.InstancePoly() != poly))
        {
            throw new InvalidOperationException("Verify failed. StaticAbstractCall");
        }
    }

    private interface IOp
    {
        static abstract int Compute(int x);

        int Apply(int x);
    }

    private readonly struct AddOp : IOp
    {
        public static int Compute(int x) => x + 1;

        public int Apply(int x) => x + 1;
    }

    private sealed class OpA : IOp
    {
        public static int Compute(int x) => x + 1;

        public int Apply(int x) => x + 1;
    }

    private sealed class OpB : IOp
    {
        public static int Compute(int x) => x + 2;

        public int Apply(int x) => x + 2;
    }
}
