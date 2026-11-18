namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class NullableBoxingBenchmark
{
    private int plain = 42;

    private int? nullable = 42;

    [Benchmark(Baseline = true)]
    public int PlainInterfaceCheck() => CompareViaInterface(plain);

    [Benchmark]
    public int NullableInterfaceCheck() => CompareViaInterface(nullable);

    [Benchmark]
    public int NullableExplicitBox()
    {
        var boxed = (object?)nullable;
        return boxed is int value ? value : 0;
    }

    public static void Verify()
    {
        var benchmark = new NullableBoxingBenchmark();
        if ((benchmark.PlainInterfaceCheck() != 1) ||
            (benchmark.NullableInterfaceCheck() != 1) ||
            (benchmark.NullableExplicitBox() != 42) ||
            (CompareViaInterface<int?>(null) != 0))
        {
            throw new InvalidOperationException("Verify failed. NullableBoxing");
        }

        benchmark.plain = 7;
        benchmark.nullable = null;
        if ((benchmark.NullableExplicitBox() != 0) || (benchmark.NullableInterfaceCheck() != 0))
        {
            throw new InvalidOperationException("Verify failed. NullableBoxing null");
        }
    }

    private static int CompareViaInterface<T>(T value) => value is IComparable comparable ? comparable.CompareTo(null) : 0;
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class GenericEnumEqualsBenchmark
{
    private readonly DayOfWeek left = DayOfWeek.Friday;

    private DayOfWeek right = DayOfWeek.Friday;

    [Benchmark(Baseline = true)]
    public bool EnumEquals() => EqualsViaEnum(left, right);

    [Benchmark]
    public bool ComparerEquals() => EqualsViaComparer(left, right);

    public static void Verify()
    {
        var benchmark = new GenericEnumEqualsBenchmark();
        if (!benchmark.EnumEquals() || !benchmark.ComparerEquals())
        {
            throw new InvalidOperationException("Verify failed. GenericEnumEquals");
        }

        benchmark.right = DayOfWeek.Monday;
        if (benchmark.EnumEquals() || benchmark.ComparerEquals())
        {
            throw new InvalidOperationException("Verify failed. GenericEnumEquals different");
        }
    }

    private static bool EqualsViaEnum<T>(T x, T y)
        where T : struct, Enum => x.Equals(y);

    private static bool EqualsViaComparer<T>(T x, T y)
        where T : struct, Enum => EqualityComparer<T>.Default.Equals(x, y);
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ComparerDevirtBenchmark
{
    private readonly EquatableKey probe = new(42);

    private int value = 42;

    [Benchmark(Baseline = true)]
    public bool ConstrainedExisting() => EqualsConstrained(probe, probe);

    [Benchmark]
    public bool DefaultComparerExisting() => EqualsDefault(probe, probe);

    [Benchmark]
    public bool ConstrainedNewInstance() => EqualsConstrained(new EquatableKey(value), probe);

    [Benchmark]
    public bool DefaultComparerNewInstance() => EqualsDefault(new EquatableKey(value), probe);

    public static void Verify()
    {
        var benchmark = new ComparerDevirtBenchmark();
        if (!benchmark.ConstrainedExisting() ||
            !benchmark.DefaultComparerExisting() ||
            !benchmark.ConstrainedNewInstance() ||
            !benchmark.DefaultComparerNewInstance())
        {
            throw new InvalidOperationException("Verify failed. ComparerDevirt");
        }

        benchmark.value = 7;
        if (benchmark.ConstrainedNewInstance() || benchmark.DefaultComparerNewInstance())
        {
            throw new InvalidOperationException("Verify failed. ComparerDevirt different");
        }
    }

    private static bool EqualsConstrained<T>(T x, T y)
        where T : IEquatable<T> => x.Equals(y);

    private static bool EqualsDefault<T>(T x, T y) => EqualityComparer<T>.Default.Equals(x, y);
}

public sealed class EquatableKey(int value) : IEquatable<EquatableKey>
{
    public int Value { get; } = value;

    public bool Equals(EquatableKey? other) => (other is not null) && (Value == other.Value);

    public override bool Equals(object? obj) => Equals(obj as EquatableKey);

    public override int GetHashCode() => Value;
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class GenericVirtualDevirtBenchmark
{
    private static readonly IEchoSource Shared = EchoSource.Create();

    private readonly int value = 42;

    [Benchmark(Baseline = true)]
    public int NonGenericShared() => Shared.EchoInt(value);

    [Benchmark]
    public int GenericShared() => Shared.Echo(value);

    [Benchmark]
    public int NonGenericLocalInstance()
    {
        var source = EchoSource.Create();
        return source.EchoInt(value);
    }

    [Benchmark]
    public int GenericLocalInstance()
    {
        var source = EchoSource.Create();
        return source.Echo(value);
    }

    public static void Verify()
    {
        var benchmark = new GenericVirtualDevirtBenchmark();
        if ((benchmark.NonGenericShared() != 43) ||
            (benchmark.GenericShared() != 42) ||
            (benchmark.NonGenericLocalInstance() != 43) ||
            (benchmark.GenericLocalInstance() != 42) ||
            (Shared.Echo("text") != "text"))
        {
            throw new InvalidOperationException("Verify failed. GenericVirtualDevirt");
        }
    }
}

public interface IEchoSource
{
    T Echo<T>(T value);

    int EchoInt(int value);
}

public sealed class EchoSource : IEchoSource
{
    public static IEchoSource Create() => new EchoSource();

    public T Echo<T>(T value) => value;

    public int EchoInt(int value) => value + 1;
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class ArrayCovarianceBenchmark
{
    private const int Length = 64;

    private readonly OpenItem openItem = new();

    private readonly SealedItem sealedItem = new();

    private readonly object[] objects = new object[Length];

    private readonly OpenItem[] openItems = new OpenItem[Length];

    private readonly SealedItem[] sealedItems = new SealedItem[Length];

    [Benchmark(Baseline = true)]
    public int StoreObjectArray()
    {
        var array = objects;
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = openItem;
        }

        return array.Length;
    }

    [Benchmark]
    public int StoreOpenElementArray()
    {
        var array = openItems;
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = openItem;
        }

        return array.Length;
    }

    [Benchmark]
    public int StoreSealedElementArray()
    {
        var array = sealedItems;
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = sealedItem;
        }

        return array.Length;
    }

    [Benchmark]
    public OpenItem StoreLocalNewArray()
    {
        var array = new OpenItem[Length];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = openItem;
        }

        return array[^1];
    }

    public static void Verify()
    {
        var benchmark = new ArrayCovarianceBenchmark();
        if ((benchmark.StoreObjectArray() != Length) ||
            (benchmark.StoreOpenElementArray() != Length) ||
            (benchmark.StoreSealedElementArray() != Length) ||
            !ReferenceEquals(benchmark.StoreLocalNewArray(), benchmark.openItem) ||
            !ReferenceEquals(benchmark.objects[^1], benchmark.openItem) ||
            !ReferenceEquals(benchmark.sealedItems[^1], benchmark.sealedItem))
        {
            throw new InvalidOperationException("Verify failed. ArrayCovariance");
        }
    }
}

public class OpenItem
{
}

public sealed class DerivedItem : OpenItem
{
}

public sealed class SealedItem
{
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class BitCastStructVectorBenchmark
{
    private DoublePair[] pairs = default!;

    [GlobalSetup]
    public void Setup()
    {
        pairs = new DoublePair[1024];
        for (var i = 0; i < pairs.Length; i++)
        {
            pairs[i] = new DoublePair(i * 0.5, i * 0.25);
        }
    }

    [Benchmark(Baseline = true)]
    public DoublePair FieldwiseAdd()
    {
        var accumulator = default(DoublePair);
        foreach (var pair in pairs)
        {
            accumulator = new DoublePair(accumulator.X + pair.X, accumulator.Y + pair.Y);
        }

        return accumulator;
    }

    [Benchmark]
    public DoublePair BitCastAdd()
    {
        var accumulator = default(DoublePair);
        foreach (var pair in pairs)
        {
            accumulator = Unsafe.BitCast<Vector128<double>, DoublePair>(
                Unsafe.BitCast<DoublePair, Vector128<double>>(accumulator) + Unsafe.BitCast<DoublePair, Vector128<double>>(pair));
        }

        return accumulator;
    }

    public static void Verify()
    {
        var benchmark = new BitCastStructVectorBenchmark();
        benchmark.Setup();
        if (benchmark.FieldwiseAdd() != benchmark.BitCastAdd())
        {
            throw new InvalidOperationException("Verify failed. BitCastStructVector");
        }
    }
}

public readonly record struct DoublePair(double X, double Y);

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net11_0)]
public class SpanSortComparerBenchmark
{
    private int[] source = default!;

    private int[] work = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new int[256];
        work = new int[256];
        var state = 12345U;
        for (var i = 0; i < source.Length; i++)
        {
            state = (state * 1664525U) + 1013904223U;
            source[i] = (int)(state >> 8);
        }
    }

    [Benchmark(Baseline = true)]
    public int SortDefault()
    {
        source.CopyTo(work, 0);
        work.AsSpan().Sort();
        return work[0];
    }

    [Benchmark]
    public int SortStructComparer()
    {
        source.CopyTo(work, 0);
        work.AsSpan().Sort(default(DescendingIntComparer));
        return work[0];
    }

    public static void Verify()
    {
        var benchmark = new SpanSortComparerBenchmark();
        benchmark.Setup();
        var ascending = benchmark.source.Order().ToArray();
        _ = benchmark.SortDefault();
        if (!benchmark.work.AsSpan().SequenceEqual(ascending))
        {
            throw new InvalidOperationException("Verify failed. SpanSortComparer default");
        }

        _ = benchmark.SortStructComparer();
        if (!benchmark.work.AsSpan().SequenceEqual(benchmark.source.OrderDescending().ToArray()))
        {
            throw new InvalidOperationException("Verify failed. SpanSortComparer struct");
        }
    }
}

public readonly struct DescendingIntComparer : IComparer<int>
{
    public int Compare(int x, int y) => y.CompareTo(x);
}
