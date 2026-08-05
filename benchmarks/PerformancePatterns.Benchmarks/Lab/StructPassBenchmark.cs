namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-04 study: passing struct arguments by value / by in, and defensive copies with a non-readonly struct
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StructPassBenchmark
{
    private const int N = 100;

    private Readonly8 size8;

    private Readonly16 size16;

    private Readonly32 size32;

    private Readonly64 size64;

    private Readonly128 size128;

    private Readonly256 size256;

    private ReadonlyMember32 readonlyMember;

    private MutableMember32 mutableMember;

    [GlobalSetup]
    public void Setup()
    {
        size8 = new Readonly8(1);
        size16 = new Readonly16(1, 2);
        size32 = new Readonly32(1, 2, 3, 4);
        size64 = new Readonly64(1, 2, 3, 4, 5, 6, 7, 8);
        size128 = new Readonly128(new Readonly64(1, 2, 3, 4, 5, 6, 7, 8), new Readonly64(9, 10, 11, 12, 13, 14, 15, 16));
        size256 = new Readonly256(size128, new Readonly128(new Readonly64(17, 18, 19, 20, 21, 22, 23, 24), new Readonly64(25, 26, 27, 28, 29, 30, 31, 32)));
        readonlyMember = new ReadonlyMember32 { A = 1, B = 2, C = 3, D = 4 };
        mutableMember = new MutableMember32 { A = 1, B = 2, C = 3, D = 4 };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public long Size8ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue8(size8);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size8ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn8(in size8);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size16ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue16(size16);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size16ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn16(in size16);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size32ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue32(size32);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size32ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn32(in size32);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size64ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue64(size64);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size64ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn64(in size64);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size128ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue128(size128);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size128ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn128(in size128);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size256ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue256(size256);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size256ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn256(in size256);
        }

        return total;
    }

    // Defensive copy or not: a readonly member does not cause one
    [Benchmark(OperationsPerInvoke = N)]
    public long InWithReadonlyMember()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByInReadonlyMember(in readonlyMember);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long InWithMutableMember()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByInMutableMember(in mutableMember);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByValue8(Readonly8 value) => value.A;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn8(in Readonly8 value) => value.A;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByValue16(Readonly16 value) => value.A + value.B;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn16(in Readonly16 value) => value.A + value.B;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByValue32(Readonly32 value) => value.A + value.B + value.C + value.D;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn32(in Readonly32 value) => value.A + value.B + value.C + value.D;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByValue64(Readonly64 value) => value.A + value.B + value.C + value.D + value.E + value.F + value.G + value.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn64(in Readonly64 value) => value.A + value.B + value.C + value.D + value.E + value.F + value.G + value.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByValue128(Readonly128 value) => value.Lo.A + value.Lo.H + value.Hi.A + value.Hi.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn128(in Readonly128 value) => value.Lo.A + value.Lo.H + value.Hi.A + value.Hi.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByValue256(Readonly256 value) => value.Lo.Lo.A + value.Lo.Hi.H + value.Hi.Lo.A + value.Hi.Hi.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn256(in Readonly256 value) => value.Lo.Lo.A + value.Lo.Hi.H + value.Hi.Lo.A + value.Hi.Hi.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByInReadonlyMember(in ReadonlyMember32 value) => value.Sum + value.Sum + value.Sum + value.Sum;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByInMutableMember(in MutableMember32 value) => value.Sum + value.Sum + value.Sum + value.Sum;
}

public readonly struct Readonly8(long a)
{
    public long A { get; } = a;
}

public readonly struct Readonly16(long a, long b)
{
    public long A { get; } = a;

    public long B { get; } = b;
}

public readonly struct Readonly32(long a, long b, long c, long d)
{
    public long A { get; } = a;

    public long B { get; } = b;

    public long C { get; } = c;

    public long D { get; } = d;
}

public readonly struct Readonly64(long a, long b, long c, long d, long e, long f, long g, long h)
{
    public long A { get; } = a;

    public long B { get; } = b;

    public long C { get; } = c;

    public long D { get; } = d;

    public long E { get; } = e;

    public long F { get; } = f;

    public long G { get; } = g;

    public long H { get; } = h;
}

public readonly struct Readonly128(Readonly64 lo, Readonly64 hi)
{
    public Readonly64 Lo { get; } = lo;

    public Readonly64 Hi { get; } = hi;
}

public readonly struct Readonly256(Readonly128 lo, Readonly128 hi)
{
    public Readonly128 Lo { get; } = lo;

    public Readonly128 Hi { get; } = hi;
}

// readonly member: no defensive copy even when passed by in
public struct ReadonlyMember32
{
    public long A { get; set; }

    public long B { get; set; }

    public long C { get; set; }

    public long D { get; set; }

    public readonly long Sum => A + B + C + D;
}

// Non-readonly member (it cannot be readonly because memoization writes state).
// Passing by in causes a defensive copy on every access, which besides the cost also brings
// a correctness trap: the memoized value is written to the copy and does not survive to the next call.
public struct MutableMember32
{
    private long memo;

    public readonly bool IsComputed => memo != 0;

    public long A { get; set; }

    public long B { get; set; }

    public long C { get; set; }

    public long D { get; set; }

    public long Sum
    {
        get
        {
            if (memo != 0)
            {
                return memo;
            }

            var computed = A + B + C + D;
            memo = computed;
            return computed;
        }
    }
}
