namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// MEM-06 検証: 構造体引数の値渡し / in 渡しと、非 readonly 構造体での防御的コピー
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StructPassBenchmark
{
    private const int N = 100;

    private Readonly8 small;

    private Readonly32 medium;

    private Readonly64 large;

    private ReadonlyMember32 readonlyMember;

    private MutableMember32 mutableMember;

    [GlobalSetup]
    public void Setup()
    {
        small = new Readonly8(1);
        medium = new Readonly32(1, 2, 3, 4);
        large = new Readonly64(1, 2, 3, 4, 5, 6, 7, 8);
        readonlyMember = new ReadonlyMember32 { A = 1, B = 2, C = 3, D = 4 };
        mutableMember = new MutableMember32 { A = 1, B = 2, C = 3, D = 4 };
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public long Size8ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue8(small);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size8ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn8(in small);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size32ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue32(medium);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size32ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn32(in medium);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size64ByValue()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByValue64(large);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public long Size64ByIn()
    {
        var total = 0L;
        for (var i = 0; i < N; i++)
        {
            total += ByIn64(in large);
        }

        return total;
    }

    // 防御的コピーの有無: readonly メンバーなら発生しない
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
    private static long ByValue32(Readonly32 value) => value.A + value.B + value.C + value.D;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn32(in Readonly32 value) => value.A + value.B + value.C + value.D;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByValue64(Readonly64 value) => value.A + value.B + value.C + value.D + value.E + value.F + value.G + value.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByIn64(in Readonly64 value) => value.A + value.B + value.C + value.D + value.E + value.F + value.G + value.H;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByInReadonlyMember(in ReadonlyMember32 value) => value.Sum + value.Sum + value.Sum + value.Sum;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ByInMutableMember(in MutableMember32 value) => value.Sum + value.Sum + value.Sum + value.Sum;
}

public readonly struct Readonly8(long a)
{
    public long A { get; } = a;
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

// readonly メンバー: in 渡しでも防御的コピーが発生しない
public struct ReadonlyMember32
{
    public long A { get; set; }

    public long B { get; set; }

    public long C { get; set; }

    public long D { get; set; }

    public readonly long Sum => A + B + C + D;
}

// 非 readonly メンバー(メモ化のため状態を書き込むので readonly にできない)。
// in 渡しではアクセスごとに防御的コピーが発生し、コストに加えて
// 「メモ化がコピーへ書かれるため次回に残らない」という正しさの罠もある。
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
