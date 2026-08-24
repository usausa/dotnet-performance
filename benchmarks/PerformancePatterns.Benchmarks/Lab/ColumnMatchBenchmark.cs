namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// TXT-10 (counter-case): matching that needs a conversion before it can dispatch.
// Shape: resolve a DB reader's column names to ordinals, matching OrdinalIgnoreCase per SQL identifier rules.
// The generated form is a guarded Equals(OrdinalIgnoreCase) chain at <=16 groups and a sampling-hash switch
// above that; the alternative is to normalize the probe first so a plain ordinal switch can be used.
// Measures both column counts and both naming conventions (the convention is applied at generation time,
// so the baked literals are already the converted names).

public enum ColumnCasing
{
    // The reader returns the same spelling as the baked literals
    AsDeclared,

    // The reader folds identifiers to upper case (as some providers do)
    AllUpper,
}

internal static class ColumnMatchKeys
{
    // Non-interned copies: a reader returns fresh strings, so reference equality must not short-circuit
    public static string[] ToReaderNames(string[] keys, ColumnCasing casing) =>
        casing == ColumnCasing.AllUpper
            ? [.. keys.Select(static x => x.ToUpperInvariant())]
            : [.. keys.Select(static x => new string(x.AsSpan()))];
}

// 8 columns, PascalCase literals: the generated narrow form (chain) is the baseline
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ColumnMatchPascal8Benchmark
{
    private const int Columns = 8;

    private const int BufferLength = 64;

    private static readonly string[] Keys =
    [
        "Id", "Name", "Amount", "Quantity",
        "UnitPrice", "CreatedAt", "UpdatedAt", "IsActive"
    ];

    private string[] names = default!;

    [Params(ColumnCasing.AsDeclared, ColumnCasing.AllUpper)]
    public ColumnCasing Casing { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        names = ColumnMatchKeys.ToReaderNames(Keys, Casing);
    }

    [Benchmark(Baseline = true)]
    public long GeneratedChain() => ResolveChain(names);

    [Benchmark]
    public long UpperStringSwitch() => ResolveByMatcher(names, static x => MatchUpperString(x));

    [Benchmark]
    public long UpperSpanSwitch() => ResolveByMatcher(names, static x => MatchUpperSpan(x));

    [Benchmark]
    public long AsciiUpperSpanSwitch() => ResolveByMatcher(names, static x => MatchAsciiUpperSpan(x));

    [Benchmark]
    public long PlainSpanSwitch() => ResolveByMatcher(names, static x => MatchDeclaredSpan(x));

    // The same chain as the baseline, reached through the matcher harness so the per-column call
    // overhead matches every other variant. The 24 column classes already run their baseline this way,
    // so without this method the 8 column rows are the only ones that are not like for like.
    [Benchmark]
    public long ChainViaMatcher() => ResolveByMatcher(names, static x => MatchChain(x));

    public static void Verify()
    {
        foreach (var casing in new[] { ColumnCasing.AsDeclared, ColumnCasing.AllUpper })
        {
            var benchmark = new ColumnMatchPascal8Benchmark { Casing = casing };
            benchmark.Setup();

            // Every column is present in declaration order, so ordinal i binds to group i
            var expected = (long)(Columns * (Columns - 1) / 2);
            long[] results =
            [
                benchmark.GeneratedChain(),
                benchmark.UpperStringSwitch(),
                benchmark.UpperSpanSwitch(),
                benchmark.AsciiUpperSpanSwitch(),
                benchmark.ChainViaMatcher(),
            ];
            foreach (var result in results)
            {
                if (result != expected)
                {
                    throw new InvalidOperationException("Verify failed. ColumnMatchPascal8Benchmark " + casing);
                }
            }

            // The plain switch has no case handling, so it only resolves when the reader's spelling matches
            if ((casing == ColumnCasing.AsDeclared) && (benchmark.PlainSpanSwitch() != expected))
            {
                throw new InvalidOperationException("Verify failed. ColumnMatchPascal8Benchmark PlainSpanSwitch");
            }
        }

        // Non-ASCII input must fall back to the generated matcher, preserving OrdinalIgnoreCase semantics
        if (MatchAsciiUpperSpan("\u00c4bc") != -1)
        {
            throw new InvalidOperationException("Verify failed. ColumnMatchPascal8Benchmark non-ASCII fallback");
        }
    }

    // The scan the generated __From performs: walk the reader's columns once, first match wins, stop when all resolved
    private static long ResolveByMatcher(string[] names, Func<string, int> matcher)
    {
        Span<int> ordinals = stackalloc int[Columns];
        ordinals.Fill(-1);
        var resolved = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var index = matcher(names[i]);
            if ((index >= 0) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                resolved++;
                if (resolved == Columns)
                {
                    break;
                }
            }
        }

        var total = 0L;
        foreach (var ordinal in ordinals)
        {
            total += ordinal;
        }

        return total;
    }

    private static long ResolveChain(string[] names)
    {
        var ord0 = -1;
        var ord1 = -1;
        var ord2 = -1;
        var ord3 = -1;
        var ord4 = -1;
        var ord5 = -1;
        var ord6 = -1;
        var ord7 = -1;
        var resolved = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            if ((ord0 < 0) && string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase))
            {
                ord0 = i;
                resolved++;
            }
            else if ((ord1 < 0) && string.Equals(name, "Name", StringComparison.OrdinalIgnoreCase))
            {
                ord1 = i;
                resolved++;
            }
            else if ((ord2 < 0) && string.Equals(name, "Amount", StringComparison.OrdinalIgnoreCase))
            {
                ord2 = i;
                resolved++;
            }
            else if ((ord3 < 0) && string.Equals(name, "Quantity", StringComparison.OrdinalIgnoreCase))
            {
                ord3 = i;
                resolved++;
            }
            else if ((ord4 < 0) && string.Equals(name, "UnitPrice", StringComparison.OrdinalIgnoreCase))
            {
                ord4 = i;
                resolved++;
            }
            else if ((ord5 < 0) && string.Equals(name, "CreatedAt", StringComparison.OrdinalIgnoreCase))
            {
                ord5 = i;
                resolved++;
            }
            else if ((ord6 < 0) && string.Equals(name, "UpdatedAt", StringComparison.OrdinalIgnoreCase))
            {
                ord6 = i;
                resolved++;
            }
            else if ((ord7 < 0) && string.Equals(name, "IsActive", StringComparison.OrdinalIgnoreCase))
            {
                ord7 = i;
                resolved++;
            }

            if (resolved == 8)
            {
                break;
            }
        }

        return (long)ord0 + ord1 + ord2 + ord3 + ord4 + ord5 + ord6 + ord7;
    }

    // Normalize first, then dispatch: (a) allocating
    private static int MatchUpperString(string name) => MatchUpperStringLabels(name.ToUpperInvariant());

    // (b) allocation free - normalize into a stack buffer, then a span switch
    [SkipLocalsInit]
    private static int MatchUpperSpan(string name)
    {
        if ((uint)name.Length > BufferLength)
        {
            return MatchChain(name);
        }

        Span<char> buffer = stackalloc char[BufferLength];
        var written = name.AsSpan().ToUpperInvariant(buffer);
        return MatchUpperSpanLabels(buffer[..written]);
    }

    // (c) SIMD ASCII normalization; non-ASCII or oversized input falls back so the semantics stay exact
    [SkipLocalsInit]
    private static int MatchAsciiUpperSpan(string name)
    {
        Span<char> buffer = stackalloc char[BufferLength];
        if (((uint)name.Length <= BufferLength) &&
            (Ascii.ToUpper(name.AsSpan(), buffer, out var written) == OperationStatus.Done))
        {
            return MatchUpperSpanLabels(buffer[..written]);
        }

        return MatchChain(name);
    }

    // Ceiling reference: no case handling at all
    private static int MatchDeclaredSpan(string name) => MatchDeclaredSpanLabels(name);

    private static int MatchUpperStringLabels(string name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNITPRICE" => 4,
        "CREATEDAT" => 5,
        "UPDATEDAT" => 6,
        "ISACTIVE" => 7,
        _ => -1,
    };

    private static int MatchUpperSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNITPRICE" => 4,
        "CREATEDAT" => 5,
        "UPDATEDAT" => 6,
        "ISACTIVE" => 7,
        _ => -1,
    };

    private static int MatchDeclaredSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "Id" => 0,
        "Name" => 1,
        "Amount" => 2,
        "Quantity" => 3,
        "UnitPrice" => 4,
        "CreatedAt" => 5,
        "UpdatedAt" => 6,
        "IsActive" => 7,
        _ => -1,
    };

    // The generated narrow form as a matcher, reused as the fallback for the normalized paths
    private static int MatchChain(string name)
    {
        if (string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(name, "Name", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(name, "Amount", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(name, "Quantity", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(name, "UnitPrice", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(name, "CreatedAt", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (string.Equals(name, "UpdatedAt", StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        if (string.Equals(name, "IsActive", StringComparison.OrdinalIgnoreCase))
        {
            return 7;
        }

        return -1;
    }
}

// 8 columns, snake_case literals: the same shape under a different naming convention
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ColumnMatchSnake8Benchmark
{
    private const int Columns = 8;

    private const int BufferLength = 64;

    private static readonly string[] Keys =
    [
        "id", "name", "amount", "quantity",
        "unit_price", "created_at", "updated_at", "is_active"
    ];

    private string[] names = default!;

    [Params(ColumnCasing.AsDeclared, ColumnCasing.AllUpper)]
    public ColumnCasing Casing { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        names = ColumnMatchKeys.ToReaderNames(Keys, Casing);
    }

    [Benchmark(Baseline = true)]
    public long GeneratedChain() => ResolveChain(names);

    [Benchmark]
    public long UpperStringSwitch() => ResolveByMatcher(names, static x => MatchUpperString(x));

    [Benchmark]
    public long UpperSpanSwitch() => ResolveByMatcher(names, static x => MatchUpperSpan(x));

    [Benchmark]
    public long AsciiUpperSpanSwitch() => ResolveByMatcher(names, static x => MatchAsciiUpperSpan(x));

    [Benchmark]
    public long PlainSpanSwitch() => ResolveByMatcher(names, static x => MatchDeclaredSpan(x));

    // The same chain as the baseline, reached through the matcher harness so the per-column call
    // overhead matches every other variant. The 24 column classes already run their baseline this way,
    // so without this method the 8 column rows are the only ones that are not like for like.
    [Benchmark]
    public long ChainViaMatcher() => ResolveByMatcher(names, static x => MatchChain(x));

    public static void Verify()
    {
        foreach (var casing in new[] { ColumnCasing.AsDeclared, ColumnCasing.AllUpper })
        {
            var benchmark = new ColumnMatchSnake8Benchmark { Casing = casing };
            benchmark.Setup();

            // Every column is present in declaration order, so ordinal i binds to group i
            var expected = (long)(Columns * (Columns - 1) / 2);
            long[] results =
            [
                benchmark.GeneratedChain(),
                benchmark.UpperStringSwitch(),
                benchmark.UpperSpanSwitch(),
                benchmark.AsciiUpperSpanSwitch(),
                benchmark.ChainViaMatcher(),
            ];
            foreach (var result in results)
            {
                if (result != expected)
                {
                    throw new InvalidOperationException("Verify failed. ColumnMatchSnake8Benchmark " + casing);
                }
            }

            // The plain switch has no case handling, so it only resolves when the reader's spelling matches
            if ((casing == ColumnCasing.AsDeclared) && (benchmark.PlainSpanSwitch() != expected))
            {
                throw new InvalidOperationException("Verify failed. ColumnMatchSnake8Benchmark PlainSpanSwitch");
            }
        }

        // Non-ASCII input must fall back to the generated matcher, preserving OrdinalIgnoreCase semantics
        if (MatchAsciiUpperSpan("\u00c4bc") != -1)
        {
            throw new InvalidOperationException("Verify failed. ColumnMatchSnake8Benchmark non-ASCII fallback");
        }
    }

    // The scan the generated __From performs: walk the reader's columns once, first match wins, stop when all resolved
    private static long ResolveByMatcher(string[] names, Func<string, int> matcher)
    {
        Span<int> ordinals = stackalloc int[Columns];
        ordinals.Fill(-1);
        var resolved = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var index = matcher(names[i]);
            if ((index >= 0) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                resolved++;
                if (resolved == Columns)
                {
                    break;
                }
            }
        }

        var total = 0L;
        foreach (var ordinal in ordinals)
        {
            total += ordinal;
        }

        return total;
    }

    private static long ResolveChain(string[] names)
    {
        var ord0 = -1;
        var ord1 = -1;
        var ord2 = -1;
        var ord3 = -1;
        var ord4 = -1;
        var ord5 = -1;
        var ord6 = -1;
        var ord7 = -1;
        var resolved = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var name = names[i];
            if ((ord0 < 0) && string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
            {
                ord0 = i;
                resolved++;
            }
            else if ((ord1 < 0) && string.Equals(name, "name", StringComparison.OrdinalIgnoreCase))
            {
                ord1 = i;
                resolved++;
            }
            else if ((ord2 < 0) && string.Equals(name, "amount", StringComparison.OrdinalIgnoreCase))
            {
                ord2 = i;
                resolved++;
            }
            else if ((ord3 < 0) && string.Equals(name, "quantity", StringComparison.OrdinalIgnoreCase))
            {
                ord3 = i;
                resolved++;
            }
            else if ((ord4 < 0) && string.Equals(name, "unit_price", StringComparison.OrdinalIgnoreCase))
            {
                ord4 = i;
                resolved++;
            }
            else if ((ord5 < 0) && string.Equals(name, "created_at", StringComparison.OrdinalIgnoreCase))
            {
                ord5 = i;
                resolved++;
            }
            else if ((ord6 < 0) && string.Equals(name, "updated_at", StringComparison.OrdinalIgnoreCase))
            {
                ord6 = i;
                resolved++;
            }
            else if ((ord7 < 0) && string.Equals(name, "is_active", StringComparison.OrdinalIgnoreCase))
            {
                ord7 = i;
                resolved++;
            }

            if (resolved == 8)
            {
                break;
            }
        }

        return (long)ord0 + ord1 + ord2 + ord3 + ord4 + ord5 + ord6 + ord7;
    }

    // Normalize first, then dispatch: (a) allocating
    private static int MatchUpperString(string name) => MatchUpperStringLabels(name.ToUpperInvariant());

    // (b) allocation free - normalize into a stack buffer, then a span switch
    [SkipLocalsInit]
    private static int MatchUpperSpan(string name)
    {
        if ((uint)name.Length > BufferLength)
        {
            return MatchChain(name);
        }

        Span<char> buffer = stackalloc char[BufferLength];
        var written = name.AsSpan().ToUpperInvariant(buffer);
        return MatchUpperSpanLabels(buffer[..written]);
    }

    // (c) SIMD ASCII normalization; non-ASCII or oversized input falls back so the semantics stay exact
    [SkipLocalsInit]
    private static int MatchAsciiUpperSpan(string name)
    {
        Span<char> buffer = stackalloc char[BufferLength];
        if (((uint)name.Length <= BufferLength) &&
            (Ascii.ToUpper(name.AsSpan(), buffer, out var written) == OperationStatus.Done))
        {
            return MatchUpperSpanLabels(buffer[..written]);
        }

        return MatchChain(name);
    }

    // Ceiling reference: no case handling at all
    private static int MatchDeclaredSpan(string name) => MatchDeclaredSpanLabels(name);

    private static int MatchUpperStringLabels(string name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNIT_PRICE" => 4,
        "CREATED_AT" => 5,
        "UPDATED_AT" => 6,
        "IS_ACTIVE" => 7,
        _ => -1,
    };

    private static int MatchUpperSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNIT_PRICE" => 4,
        "CREATED_AT" => 5,
        "UPDATED_AT" => 6,
        "IS_ACTIVE" => 7,
        _ => -1,
    };

    private static int MatchDeclaredSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "amount" => 2,
        "quantity" => 3,
        "unit_price" => 4,
        "created_at" => 5,
        "updated_at" => 6,
        "is_active" => 7,
        _ => -1,
    };

    // The generated narrow form as a matcher, reused as the fallback for the normalized paths
    private static int MatchChain(string name)
    {
        if (string.Equals(name, "id", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(name, "name", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(name, "amount", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(name, "quantity", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(name, "unit_price", StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (string.Equals(name, "created_at", StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (string.Equals(name, "updated_at", StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        if (string.Equals(name, "is_active", StringComparison.OrdinalIgnoreCase))
        {
            return 7;
        }

        return -1;
    }
}

// 24 columns, PascalCase literals: the generated wide form (sampling-hash switch) is the baseline
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ColumnMatchPascal24Benchmark
{
    private const int Columns = 24;

    private const int BufferLength = 64;

    private static readonly string[] Keys =
    [
        "Id", "Name", "Amount", "Quantity",
        "UnitPrice", "CreatedAt", "UpdatedAt", "IsActive",
        "CategoryId", "CustomerId", "OrderDate", "ShippedDate",
        "Description", "Notes", "Discount", "TotalAmount",
        "Status", "Email", "Address", "City",
        "Country", "ZipCode", "PhoneNumber", "ModifiedBy"
    ];

    private string[] names = default!;

    [Params(ColumnCasing.AsDeclared, ColumnCasing.AllUpper)]
    public ColumnCasing Casing { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        names = ColumnMatchKeys.ToReaderNames(Keys, Casing);
    }

    [Benchmark(Baseline = true)]
    public long GeneratedHash() => ResolveByMatcher(names, static x => MatchHash(x));

    [Benchmark]
    public long UpperStringSwitch() => ResolveByMatcher(names, static x => MatchUpperString(x));

    [Benchmark]
    public long UpperSpanSwitch() => ResolveByMatcher(names, static x => MatchUpperSpan(x));

    [Benchmark]
    public long AsciiUpperSpanSwitch() => ResolveByMatcher(names, static x => MatchAsciiUpperSpan(x));

    [Benchmark]
    public long PlainSpanSwitch() => ResolveByMatcher(names, static x => MatchDeclaredSpan(x));

    public static void Verify()
    {
        foreach (var casing in new[] { ColumnCasing.AsDeclared, ColumnCasing.AllUpper })
        {
            var benchmark = new ColumnMatchPascal24Benchmark { Casing = casing };
            benchmark.Setup();

            // Every column is present in declaration order, so ordinal i binds to group i
            var expected = (long)(Columns * (Columns - 1) / 2);
            long[] results =
            [
                benchmark.GeneratedHash(),
                benchmark.UpperStringSwitch(),
                benchmark.UpperSpanSwitch(),
                benchmark.AsciiUpperSpanSwitch(),
            ];
            foreach (var result in results)
            {
                if (result != expected)
                {
                    throw new InvalidOperationException("Verify failed. ColumnMatchPascal24Benchmark " + casing);
                }
            }

            // The plain switch has no case handling, so it only resolves when the reader's spelling matches
            if ((casing == ColumnCasing.AsDeclared) && (benchmark.PlainSpanSwitch() != expected))
            {
                throw new InvalidOperationException("Verify failed. ColumnMatchPascal24Benchmark PlainSpanSwitch");
            }
        }

        // Non-ASCII input must fall back to the generated matcher, preserving OrdinalIgnoreCase semantics
        if (MatchAsciiUpperSpan("\u00c4bc") != -1)
        {
            throw new InvalidOperationException("Verify failed. ColumnMatchPascal24Benchmark non-ASCII fallback");
        }
    }

    // The scan the generated __From performs: walk the reader's columns once, first match wins, stop when all resolved
    private static long ResolveByMatcher(string[] names, Func<string, int> matcher)
    {
        Span<int> ordinals = stackalloc int[Columns];
        ordinals.Fill(-1);
        var resolved = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var index = matcher(names[i]);
            if ((index >= 0) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                resolved++;
                if (resolved == Columns)
                {
                    break;
                }
            }
        }

        var total = 0L;
        foreach (var ordinal in ordinals)
        {
            total += ordinal;
        }

        return total;
    }

    // Normalize first, then dispatch: (a) allocating
    private static int MatchUpperString(string name) => MatchUpperStringLabels(name.ToUpperInvariant());

    // (b) allocation free - normalize into a stack buffer, then a span switch
    [SkipLocalsInit]
    private static int MatchUpperSpan(string name)
    {
        if ((uint)name.Length > BufferLength)
        {
            return MatchHash(name);
        }

        Span<char> buffer = stackalloc char[BufferLength];
        var written = name.AsSpan().ToUpperInvariant(buffer);
        return MatchUpperSpanLabels(buffer[..written]);
    }

    // (c) SIMD ASCII normalization; non-ASCII or oversized input falls back so the semantics stay exact
    [SkipLocalsInit]
    private static int MatchAsciiUpperSpan(string name)
    {
        Span<char> buffer = stackalloc char[BufferLength];
        if (((uint)name.Length <= BufferLength) &&
            (Ascii.ToUpper(name.AsSpan(), buffer, out var written) == OperationStatus.Done))
        {
            return MatchUpperSpanLabels(buffer[..written]);
        }

        return MatchHash(name);
    }

    // Ceiling reference: no case handling at all
    private static int MatchDeclaredSpan(string name) => MatchDeclaredSpanLabels(name);

    private static int MatchUpperStringLabels(string name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNITPRICE" => 4,
        "CREATEDAT" => 5,
        "UPDATEDAT" => 6,
        "ISACTIVE" => 7,
        "CATEGORYID" => 8,
        "CUSTOMERID" => 9,
        "ORDERDATE" => 10,
        "SHIPPEDDATE" => 11,
        "DESCRIPTION" => 12,
        "NOTES" => 13,
        "DISCOUNT" => 14,
        "TOTALAMOUNT" => 15,
        "STATUS" => 16,
        "EMAIL" => 17,
        "ADDRESS" => 18,
        "CITY" => 19,
        "COUNTRY" => 20,
        "ZIPCODE" => 21,
        "PHONENUMBER" => 22,
        "MODIFIEDBY" => 23,
        _ => -1,
    };

    private static int MatchUpperSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNITPRICE" => 4,
        "CREATEDAT" => 5,
        "UPDATEDAT" => 6,
        "ISACTIVE" => 7,
        "CATEGORYID" => 8,
        "CUSTOMERID" => 9,
        "ORDERDATE" => 10,
        "SHIPPEDDATE" => 11,
        "DESCRIPTION" => 12,
        "NOTES" => 13,
        "DISCOUNT" => 14,
        "TOTALAMOUNT" => 15,
        "STATUS" => 16,
        "EMAIL" => 17,
        "ADDRESS" => 18,
        "CITY" => 19,
        "COUNTRY" => 20,
        "ZIPCODE" => 21,
        "PHONENUMBER" => 22,
        "MODIFIEDBY" => 23,
        _ => -1,
    };

    private static int MatchDeclaredSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "Id" => 0,
        "Name" => 1,
        "Amount" => 2,
        "Quantity" => 3,
        "UnitPrice" => 4,
        "CreatedAt" => 5,
        "UpdatedAt" => 6,
        "IsActive" => 7,
        "CategoryId" => 8,
        "CustomerId" => 9,
        "OrderDate" => 10,
        "ShippedDate" => 11,
        "Description" => 12,
        "Notes" => 13,
        "Discount" => 14,
        "TotalAmount" => 15,
        "Status" => 16,
        "Email" => 17,
        "Address" => 18,
        "City" => 19,
        "Country" => 20,
        "ZipCode" => 21,
        "PhoneNumber" => 22,
        "ModifiedBy" => 23,
        _ => -1,
    };

    // The generated wide form: switch on (length << 16) ^ (upper(first) << 8) ^ (upper(mid) << 4) ^ upper(last),
    // confirmed by Equals(OrdinalIgnoreCase). Hash constants are baked at generation time.
    private static int MatchHash(string name)
    {
        var length = name.Length;
        if (length == 0)
        {
            return -1;
        }

        return ((length << 16) ^ (char.ToUpperInvariant(name[0]) << 8) ^ (char.ToUpperInvariant(name[length >> 1]) << 4) ^ char.ToUpperInvariant(name[length - 1])) switch
        {
            150788 => string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase) ? 0 : -1,
            280089 => string.Equals(name, "City", StringComparison.OrdinalIgnoreCase) ? 19 : -1,
            281237 => string.Equals(name, "Name", StringComparison.OrdinalIgnoreCase) ? 1 : -1,
            344412 => string.Equals(name, "Email", StringComparison.OrdinalIgnoreCase) ? 17 : -1,
            346899 => string.Equals(name, "Notes", StringComparison.OrdinalIgnoreCase) ? 13 : -1,
            410628 => string.Equals(name, "Amount", StringComparison.OrdinalIgnoreCase) ? 2 : -1,
            415251 => string.Equals(name, "Status", StringComparison.OrdinalIgnoreCase) ? 16 : -1,
            476275 => string.Equals(name, "Address", StringComparison.OrdinalIgnoreCase) ? 18 : -1,
            477113 => string.Equals(name, "Country", StringComparison.OrdinalIgnoreCase) ? 20 : -1,
            482933 => string.Equals(name, "ZipCode", StringComparison.OrdinalIgnoreCase) ? 21 : -1,
            540836 => string.Equals(name, "Discount", StringComparison.OrdinalIgnoreCase) ? 14 : -1,
            543749 => string.Equals(name, "IsActive", StringComparison.OrdinalIgnoreCase) ? 7 : -1,
            545817 => string.Equals(name, "Quantity", StringComparison.OrdinalIgnoreCase) ? 3 : -1,
            607764 => string.Equals(name, "CreatedAt", StringComparison.OrdinalIgnoreCase) ? 5 : -1,
            608869 => string.Equals(name, "OrderDate", StringComparison.OrdinalIgnoreCase) ? 10 : -1,
            610324 => string.Equals(name, "UpdatedAt", StringComparison.OrdinalIgnoreCase) ? 6 : -1,
            610373 => string.Equals(name, "UnitPrice", StringComparison.OrdinalIgnoreCase) ? 4 : -1,
            673684 => string.Equals(name, "CustomerId", StringComparison.OrdinalIgnoreCase) ? 9 : -1,
            673716 => string.Equals(name, "CategoryId", StringComparison.OrdinalIgnoreCase) ? 8 : -1,
            674249 => string.Equals(name, "ModifiedBy", StringComparison.OrdinalIgnoreCase) ? 23 : -1,
            737502 => string.Equals(name, "Description", StringComparison.OrdinalIgnoreCase) ? 12 : -1,
            741444 => string.Equals(name, "TotalAmount", StringComparison.OrdinalIgnoreCase) ? 15 : -1,
            742578 => string.Equals(name, "PhoneNumber", StringComparison.OrdinalIgnoreCase) ? 22 : -1,
            743189 => string.Equals(name, "ShippedDate", StringComparison.OrdinalIgnoreCase) ? 11 : -1,
            _ => -1,
        };
    }
}

// 24 columns, snake_case literals
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ColumnMatchSnake24Benchmark
{
    private const int Columns = 24;

    private const int BufferLength = 64;

    private static readonly string[] Keys =
    [
        "id", "name", "amount", "quantity",
        "unit_price", "created_at", "updated_at", "is_active",
        "category_id", "customer_id", "order_date", "shipped_date",
        "description", "notes", "discount", "total_amount",
        "status", "email", "address", "city",
        "country", "zip_code", "phone_number", "modified_by"
    ];

    private string[] names = default!;

    [Params(ColumnCasing.AsDeclared, ColumnCasing.AllUpper)]
    public ColumnCasing Casing { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        names = ColumnMatchKeys.ToReaderNames(Keys, Casing);
    }

    [Benchmark(Baseline = true)]
    public long GeneratedHash() => ResolveByMatcher(names, static x => MatchHash(x));

    [Benchmark]
    public long UpperStringSwitch() => ResolveByMatcher(names, static x => MatchUpperString(x));

    [Benchmark]
    public long UpperSpanSwitch() => ResolveByMatcher(names, static x => MatchUpperSpan(x));

    [Benchmark]
    public long AsciiUpperSpanSwitch() => ResolveByMatcher(names, static x => MatchAsciiUpperSpan(x));

    [Benchmark]
    public long PlainSpanSwitch() => ResolveByMatcher(names, static x => MatchDeclaredSpan(x));

    public static void Verify()
    {
        foreach (var casing in new[] { ColumnCasing.AsDeclared, ColumnCasing.AllUpper })
        {
            var benchmark = new ColumnMatchSnake24Benchmark { Casing = casing };
            benchmark.Setup();

            // Every column is present in declaration order, so ordinal i binds to group i
            var expected = (long)(Columns * (Columns - 1) / 2);
            long[] results =
            [
                benchmark.GeneratedHash(),
                benchmark.UpperStringSwitch(),
                benchmark.UpperSpanSwitch(),
                benchmark.AsciiUpperSpanSwitch(),
            ];
            foreach (var result in results)
            {
                if (result != expected)
                {
                    throw new InvalidOperationException("Verify failed. ColumnMatchSnake24Benchmark " + casing);
                }
            }

            // The plain switch has no case handling, so it only resolves when the reader's spelling matches
            if ((casing == ColumnCasing.AsDeclared) && (benchmark.PlainSpanSwitch() != expected))
            {
                throw new InvalidOperationException("Verify failed. ColumnMatchSnake24Benchmark PlainSpanSwitch");
            }
        }

        // Non-ASCII input must fall back to the generated matcher, preserving OrdinalIgnoreCase semantics
        if (MatchAsciiUpperSpan("\u00c4bc") != -1)
        {
            throw new InvalidOperationException("Verify failed. ColumnMatchSnake24Benchmark non-ASCII fallback");
        }
    }

    // The scan the generated __From performs: walk the reader's columns once, first match wins, stop when all resolved
    private static long ResolveByMatcher(string[] names, Func<string, int> matcher)
    {
        Span<int> ordinals = stackalloc int[Columns];
        ordinals.Fill(-1);
        var resolved = 0;
        for (var i = 0; i < names.Length; i++)
        {
            var index = matcher(names[i]);
            if ((index >= 0) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                resolved++;
                if (resolved == Columns)
                {
                    break;
                }
            }
        }

        var total = 0L;
        foreach (var ordinal in ordinals)
        {
            total += ordinal;
        }

        return total;
    }

    // Normalize first, then dispatch: (a) allocating
    private static int MatchUpperString(string name) => MatchUpperStringLabels(name.ToUpperInvariant());

    // (b) allocation free - normalize into a stack buffer, then a span switch
    [SkipLocalsInit]
    private static int MatchUpperSpan(string name)
    {
        if ((uint)name.Length > BufferLength)
        {
            return MatchHash(name);
        }

        Span<char> buffer = stackalloc char[BufferLength];
        var written = name.AsSpan().ToUpperInvariant(buffer);
        return MatchUpperSpanLabels(buffer[..written]);
    }

    // (c) SIMD ASCII normalization; non-ASCII or oversized input falls back so the semantics stay exact
    [SkipLocalsInit]
    private static int MatchAsciiUpperSpan(string name)
    {
        Span<char> buffer = stackalloc char[BufferLength];
        if (((uint)name.Length <= BufferLength) &&
            (Ascii.ToUpper(name.AsSpan(), buffer, out var written) == OperationStatus.Done))
        {
            return MatchUpperSpanLabels(buffer[..written]);
        }

        return MatchHash(name);
    }

    // Ceiling reference: no case handling at all
    private static int MatchDeclaredSpan(string name) => MatchDeclaredSpanLabels(name);

    private static int MatchUpperStringLabels(string name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNIT_PRICE" => 4,
        "CREATED_AT" => 5,
        "UPDATED_AT" => 6,
        "IS_ACTIVE" => 7,
        "CATEGORY_ID" => 8,
        "CUSTOMER_ID" => 9,
        "ORDER_DATE" => 10,
        "SHIPPED_DATE" => 11,
        "DESCRIPTION" => 12,
        "NOTES" => 13,
        "DISCOUNT" => 14,
        "TOTAL_AMOUNT" => 15,
        "STATUS" => 16,
        "EMAIL" => 17,
        "ADDRESS" => 18,
        "CITY" => 19,
        "COUNTRY" => 20,
        "ZIP_CODE" => 21,
        "PHONE_NUMBER" => 22,
        "MODIFIED_BY" => 23,
        _ => -1,
    };

    private static int MatchUpperSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "ID" => 0,
        "NAME" => 1,
        "AMOUNT" => 2,
        "QUANTITY" => 3,
        "UNIT_PRICE" => 4,
        "CREATED_AT" => 5,
        "UPDATED_AT" => 6,
        "IS_ACTIVE" => 7,
        "CATEGORY_ID" => 8,
        "CUSTOMER_ID" => 9,
        "ORDER_DATE" => 10,
        "SHIPPED_DATE" => 11,
        "DESCRIPTION" => 12,
        "NOTES" => 13,
        "DISCOUNT" => 14,
        "TOTAL_AMOUNT" => 15,
        "STATUS" => 16,
        "EMAIL" => 17,
        "ADDRESS" => 18,
        "CITY" => 19,
        "COUNTRY" => 20,
        "ZIP_CODE" => 21,
        "PHONE_NUMBER" => 22,
        "MODIFIED_BY" => 23,
        _ => -1,
    };

    private static int MatchDeclaredSpanLabels(ReadOnlySpan<char> name) => name switch
    {
        "id" => 0,
        "name" => 1,
        "amount" => 2,
        "quantity" => 3,
        "unit_price" => 4,
        "created_at" => 5,
        "updated_at" => 6,
        "is_active" => 7,
        "category_id" => 8,
        "customer_id" => 9,
        "order_date" => 10,
        "shipped_date" => 11,
        "description" => 12,
        "notes" => 13,
        "discount" => 14,
        "total_amount" => 15,
        "status" => 16,
        "email" => 17,
        "address" => 18,
        "city" => 19,
        "country" => 20,
        "zip_code" => 21,
        "phone_number" => 22,
        "modified_by" => 23,
        _ => -1,
    };

    // The generated wide form: switch on (length << 16) ^ (upper(first) << 8) ^ (upper(mid) << 4) ^ upper(last),
    // confirmed by Equals(OrdinalIgnoreCase). Hash constants are baked at generation time.
    private static int MatchHash(string name)
    {
        var length = name.Length;
        if (length == 0)
        {
            return -1;
        }

        return ((length << 16) ^ (char.ToUpperInvariant(name[0]) << 8) ^ (char.ToUpperInvariant(name[length >> 1]) << 4) ^ char.ToUpperInvariant(name[length - 1])) switch
        {
            150788 => string.Equals(name, "id", StringComparison.OrdinalIgnoreCase) ? 0 : -1,
            280089 => string.Equals(name, "city", StringComparison.OrdinalIgnoreCase) ? 19 : -1,
            281237 => string.Equals(name, "name", StringComparison.OrdinalIgnoreCase) ? 1 : -1,
            344412 => string.Equals(name, "email", StringComparison.OrdinalIgnoreCase) ? 17 : -1,
            346899 => string.Equals(name, "notes", StringComparison.OrdinalIgnoreCase) ? 13 : -1,
            410628 => string.Equals(name, "amount", StringComparison.OrdinalIgnoreCase) ? 2 : -1,
            415251 => string.Equals(name, "status", StringComparison.OrdinalIgnoreCase) ? 16 : -1,
            476275 => string.Equals(name, "address", StringComparison.OrdinalIgnoreCase) ? 18 : -1,
            477113 => string.Equals(name, "country", StringComparison.OrdinalIgnoreCase) ? 20 : -1,
            540836 => string.Equals(name, "discount", StringComparison.OrdinalIgnoreCase) ? 14 : -1,
            545817 => string.Equals(name, "quantity", StringComparison.OrdinalIgnoreCase) ? 3 : -1,
            548469 => string.Equals(name, "zip_code", StringComparison.OrdinalIgnoreCase) ? 21 : -1,
            609653 => string.Equals(name, "is_active", StringComparison.OrdinalIgnoreCase) ? 7 : -1,
            673540 => string.Equals(name, "created_at", StringComparison.OrdinalIgnoreCase) ? 5 : -1,
            674485 => string.Equals(name, "order_date", StringComparison.OrdinalIgnoreCase) ? 10 : -1,
            675909 => string.Equals(name, "unit_price", StringComparison.OrdinalIgnoreCase) ? 4 : -1,
            676100 => string.Equals(name, "updated_at", StringComparison.OrdinalIgnoreCase) ? 6 : -1,
            737502 => string.Equals(name, "description", StringComparison.OrdinalIgnoreCase) ? 12 : -1,
            739220 => string.Equals(name, "customer_id", StringComparison.OrdinalIgnoreCase) ? 9 : -1,
            739252 => string.Equals(name, "category_id", StringComparison.OrdinalIgnoreCase) ? 8 : -1,
            739785 => string.Equals(name, "modified_by", StringComparison.OrdinalIgnoreCase) ? 23 : -1,
            806980 => string.Equals(name, "total_amount", StringComparison.OrdinalIgnoreCase) ? 15 : -1,
            808114 => string.Equals(name, "phone_number", StringComparison.OrdinalIgnoreCase) ? 22 : -1,
            808709 => string.Equals(name, "shipped_date", StringComparison.OrdinalIgnoreCase) ? 11 : -1,
            _ => -1,
        };
    }
}
