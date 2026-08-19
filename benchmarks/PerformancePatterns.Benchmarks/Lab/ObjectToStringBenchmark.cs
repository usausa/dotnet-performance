namespace PerformancePatterns.Benchmarks.Lab;

using System.Globalization;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TXT-11: a generic object -> string converter, with and without a type fast path in front of the
// IFormattable call. The question is not whether a type test is faster in principle, but whether the
// fast path stays small enough for the JIT to inline it - see benchmarks/results/TXT-11-ObjectToStringFastPath.md.
public enum ValueShape
{
    // One boxed type at the call site: Dynamic PGO can devirtualize the interface call
    Monomorphic,

    // The realistic converter shape: many boxed types through a single call site
    Mixed,
}

[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ObjectToStringBenchmark
{
    private const int Count = 16;

    private object[] values = default!;

    [Params(ValueShape.Monomorphic, ValueShape.Mixed)]
    public ValueShape Shape { get; set; }

    [GlobalSetup]
    public void Setup() => values = Build(Shape);

    public static object[] Build(ValueShape shape)
    {
        if (shape == ValueShape.Monomorphic)
        {
            var mono = new object[Count];
            for (var i = 0; i < Count; i++)
            {
                mono[i] = 1234567 + i;
            }

            return mono;
        }

        return
        [
            1234567, 7654321L, 3.5d, 2.25f, 12.75m, true, "already-a-string", (short)321,
            (byte)7, 'x', new DateTime(2026, 8, 19, 12, 34, 56, DateTimeKind.Utc),
            new TimeSpan(1, 2, 3), Guid.Parse("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
            1234567.89d, -42, DayOfWeek.Wednesday,
        ];
    }

    // Current form: one interface test, then a virtual IFormattable.ToString through the interface
    public static string ConvertInterface(object value) =>
        value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString()!;

    // Wide fast path: every supported type enumerated. 660-799 B at Tier1, over the inlining threshold,
    // so it stays out of line and pays a call per conversion
    public static string ConvertTypeSwitch(object value) =>
        value switch
        {
            int v => v.ToString(CultureInfo.InvariantCulture),
            long v => v.ToString(CultureInfo.InvariantCulture),
            double v => v.ToString(CultureInfo.InvariantCulture),
            decimal v => v.ToString(CultureInfo.InvariantCulture),
            DateTime v => v.ToString(CultureInfo.InvariantCulture),
            bool v => v.ToString(),
            string v => v,
            IFormattable v => v.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()!,
        };

    // Narrow fast path: only the types that dominate the call site, then the same fallback.
    // 216-315 B at Tier1 - small enough to be inlined into the caller
    public static string ConvertTypeSwitchNarrow(object value) =>
        value switch
        {
            int v => v.ToString(CultureInfo.InvariantCulture),
            string v => v,
            _ => value is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : value.ToString()!,
        };

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int InterfacePath()
    {
        var total = 0;
        var items = values;
        for (var i = 0; i < items.Length; i++)
        {
            total += ConvertInterface(items[i]).Length;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int TypeSwitch()
    {
        var total = 0;
        var items = values;
        for (var i = 0; i < items.Length; i++)
        {
            total += ConvertTypeSwitch(items[i]).Length;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int TypeSwitchNarrow()
    {
        var total = 0;
        var items = values;
        for (var i = 0; i < items.Length; i++)
        {
            total += ConvertTypeSwitchNarrow(items[i]).Length;
        }

        return total;
    }

    public static void Verify()
    {
        ValueShape[] shapes = [ValueShape.Monomorphic, ValueShape.Mixed];
        foreach (var shape in shapes)
        {
            foreach (var value in Build(shape))
            {
                var expected = ConvertInterface(value);
                if (!string.Equals(expected, ConvertTypeSwitch(value), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Verify failed. TypeSwitch " + value.GetType().Name);
                }

                if (!string.Equals(expected, ConvertTypeSwitchNarrow(value), StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Verify failed. TypeSwitchNarrow " + value.GetType().Name);
                }
            }
        }
    }
}
