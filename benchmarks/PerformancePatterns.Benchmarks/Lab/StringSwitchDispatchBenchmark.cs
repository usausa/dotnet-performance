namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Col;

// TXT-10: aggregating known-string matching into a switch, so Roslyn does the length / character bucketing.
// This file holds the shape questions - the lower bound (4 keys), the main win (16), and long keys.
// Scale (64 / 128 keys) is in StringSwitchScaleBenchmark.cs.

// Which probe series a run measures.
public enum ProbeSet
{
    Hit,
    Miss,
}

// Key sets for TXT-10. Ordinary PascalCase identifier names (column / property / enum-member shaped), so the
// length and character spread is what a real generated key set looks like rather than something tuned for one
// strategy. The smaller sets are prefixes of the same master list, which keeps the size comparison honest.
internal static class StringSwitchKeys
{
    private const string Replacements = "QWZXJVKY";

    private static readonly string[] Master =
    [
        "Id",
        "Name",
        "Code",
        "Type",
        "Status",
        "Amount",
        "Balance",
        "Currency",
        "CreatedAt",
        "UpdatedAt",
        "DeletedAt",
        "OwnerId",
        "ParentId",
        "Version",
        "IsEnabled",
        "Description",
        "Title",
        "Summary",
        "Body",
        "Author",
        "Editor",
        "Reviewer",
        "Approver",
        "Category",
        "Tag",
        "Label",
        "Priority",
        "Severity",
        "Source",
        "Target",
        "Origin",
        "Destination",
        "Latitude",
        "Longitude",
        "Altitude",
        "Distance",
        "Duration",
        "Interval",
        "Timeout",
        "Retry",
        "Attempt",
        "Sequence",
        "Position",
        "Offset",
        "Length",
        "Capacity",
        "Size",
        "Weight",
        "Height",
        "Width",
        "Depth",
        "Color",
        "Shape",
        "Format",
        "Encoding",
        "Culture",
        "Locale",
        "Timezone",
        "Country",
        "Region",
        "City",
        "Street",
        "PostalCode",
        "Phone",
        "Email",
        "Website",
        "Company",
        "Department",
        "Division",
        "Manager",
        "Employee",
        "Salary",
        "Bonus",
        "Discount",
        "TaxRate",
        "Subtotal",
        "Total",
        "Quantity",
        "UnitPrice",
        "Sku",
        "Barcode",
        "Serial",
        "Batch",
        "Lot",
        "Warehouse",
        "Shelf",
        "Bin",
        "Carrier",
        "Tracking",
        "Shipped",
        "Delivered",
        "Returned",
        "Cancelled",
        "Refunded",
        "Invoice",
        "Receipt",
        "Payment",
        "Method",
        "Provider",
        "Gateway",
        "Token",
        "Secret",
        "Signature",
        "Checksum",
        "Digest",
        "Nonce",
        "Salt",
        "Hash",
        "Cipher",
        "Algorithm",
        "KeySize",
        "Expiry",
        "IssuedAt",
        "NotBefore",
        "NotAfter",
        "Issuer",
        "Subject",
        "Audience",
        "Scope",
        "Role",
        "Permission",
        "Policy",
        "Rule",
        "Condition",
        "Effect",
        "Resource",
        "Session",
        "Request",
    ];

    private static readonly string[] Long =
    [
        "ResourceExpiryIntervalBarcodeDigestLengthPaymentAuthorCurr",
        "TargetSequenceTotalEmployeeDeletedAtAmountDepthSalaryBodyHa",
        "DepthShelfNameDestinationUnitPriceUnitPriceSubtotalCipherDep",
        "LatitudeEmployeeSignatureApproverAudiencePostalCodeInvoiceTra",
        "IssuerLocaleIdApproverCancelledCountryTypeRefundedTrackingIssu",
        "CarrierBonusPaymentSequenceNonceResourceSerialOriginDelive",
        "ApproverVersionNotBeforeIntervalSubtotalCultureAudienceInvo",
        "VersionLabelSeverityPermissionCodeAuthorShelfBalanceTimeoutS",
        "CancelledApproverTitleLabelUpdatedAtCategoryEmailEmployeeLabe",
        "AuthorCultureSkuShippedWeightTrackingEncodingWarehouseBinNotBe",
        "ManagerBinAmountAudienceDurationHashNameScopeReturnedShape",
        "DeliveredSessionEncodingTotalUpdatedAtBodyLotEditorCurrency",
        "LatitudeBarcodeDeletedAtCultureSaltShapeDepartmentIntervalRe",
        "TimezoneBinKeySizeDivisionUpdatedAtLengthSeverityLengthLotPar",
        "BarcodeBonusTitleTagStatusAmountParentIdShelfBarcodeKeySizeLon",
        "AudiencePolicyPostalCodeEncodingCompanyBinSizeBatchSkuSalt",
    ];

    public static string[] Small() => Master[..4];

    public static string[] Medium() => Master[..16];

    public static string[] Large64() => Master[..64];

    public static string[] Large128() => Master[..];

    public static string[] LongKey() => Long[..];

    // A miss probe keeps the length and replaces the character at index length/3 - a position the sampling hash
    // never reads (it samples first / middle / last). So a miss lands in the same bucket and has to be rejected by
    // the full compare: the pessimistic case for the sampling forms, the neutral case for the compiler's dispatch.
    public static string[] ToMiss(string[] keys)
    {
        var used = new HashSet<string>(keys, StringComparer.Ordinal);
        var result = new string[keys.Length];
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var index = key.Length / 3;
            foreach (var replacement in Replacements)
            {
                if (key[index] == replacement)
                {
                    continue;
                }

                var chars = key.ToCharArray();
                chars[index] = replacement;
                var candidate = new string(chars);
                if (used.Add(candidate))
                {
                    result[i] = candidate;
                    break;
                }
            }

            if (result[i] is null)
            {
                throw new InvalidOperationException("No miss probe for " + key);
            }
        }

        return result;
    }

    // Runtime-built copies, so no probe can short-circuit on reference equality with an interned literal
    public static string[] ToProbes(string[] source)
    {
        var probes = new string[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            probes[i] = new string(source[i].AsSpan());
        }

        return probes;
    }

    public static Dictionary<string, int> ToDictionary(string[] keys)
    {
        var dictionary = new Dictionary<string, int>(keys.Length, StringComparer.Ordinal);
        for (var i = 0; i < keys.Length; i++)
        {
            dictionary[keys[i]] = i;
        }

        return dictionary;
    }
}

// The hash a source generator bakes into scenario 1 output: length plus the first / middle / last character only.
// Byte-identical to SampledNameTable<TValue>.CalculateHash (src/PerformancePatterns/Col/SampledNameTable.cs);
// duplicated here because the generated form is standalone code with the case constants already folded in.
internal static class StringSwitchSamplingHash
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Calculate(ReadOnlySpan<char> value)
    {
        var length = value.Length;
        if (length == 0)
        {
            return 0;
        }

        ref var head = ref MemoryMarshal.GetReference(value);
        var first = Unsafe.Add(ref head, 0);
        var middle = Unsafe.Add(ref head, length >> 1);
        var last = Unsafe.Add(ref head, length - 1);
        return (length << 16) ^ (first << 8) ^ (middle << 4) ^ last;
    }
}

// Lower bound: 4 keys - does the Equals chain still beat a switch at the smallest size?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StringSwitchSmallBenchmark
{
    private const int Count = 4;

    private string[] probes = default!;

    [Params(ProbeSet.Hit, ProbeSet.Miss)]
    public ProbeSet Probe { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var keys = StringSwitchKeys.Small();
        probes = StringSwitchKeys.ToProbes(Probe == ProbeSet.Hit ? keys : StringSwitchKeys.ToMiss(StringSwitchKeys.Small()));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int EqualsChain()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveEqualsChain(items[i]);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SpanEqualsChain()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveSpanEqualsChain(items[i].AsSpan());
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int StringSwitch()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveStringSwitch(items[i]);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SpanSwitch()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveSpanSwitch(items[i].AsSpan());
        }

        return total;
    }

    // Current guidance for small sets: an Equals chain in declaration order
    private static int ResolveEqualsChain(string name)
    {
        if (string.Equals(name, "Id", StringComparison.Ordinal)) { return 0; }
        if (string.Equals(name, "Name", StringComparison.Ordinal)) { return 1; }
        if (string.Equals(name, "Code", StringComparison.Ordinal)) { return 2; }
        if (string.Equals(name, "Type", StringComparison.Ordinal)) { return 3; }
        return -1;
    }

    // The same chain over a span, so the span switch has a like-for-like baseline
    private static int ResolveSpanEqualsChain(ReadOnlySpan<char> name)
    {
        if (name.SequenceEqual("Id")) { return 0; }
        if (name.SequenceEqual("Name")) { return 1; }
        if (name.SequenceEqual("Code")) { return 2; }
        if (name.SequenceEqual("Type")) { return 3; }
        return -1;
    }

    // Roslyn turns this into a length bucket then character tests (an FNV hash switch once the set is large enough)
    private static int ResolveStringSwitch(string name) =>
        name switch
        {
            "Id" => 0,
            "Name" => 1,
            "Code" => 2,
            "Type" => 3,
            _ => -1,
        };

    // C# 11: a ReadOnlySpan<char> constant pattern gets the same treatment, so the generated form can take a span
    private static int ResolveSpanSwitch(ReadOnlySpan<char> name) =>
        name switch
        {
            "Id" => 0,
            "Name" => 1,
            "Code" => 2,
            "Type" => 3,
            _ => -1,
        };

    public static void Verify()
    {
        var keys = StringSwitchKeys.Small();
        string[][] sources = [keys, StringSwitchKeys.ToMiss(StringSwitchKeys.Small())];
        foreach (var source in sources)
        {
            foreach (var probe in StringSwitchKeys.ToProbes(source))
            {
                var expected = Array.IndexOf(keys, probe);
                if (!(expected == ResolveEqualsChain(probe)))
                {
                    throw new InvalidOperationException("Verify failed. EqualsChain " + probe);
                }

                if (!(expected == ResolveSpanEqualsChain(probe.AsSpan())))
                {
                    throw new InvalidOperationException("Verify failed. SpanEqualsChain " + probe);
                }

                if (!(expected == ResolveStringSwitch(probe)))
                {
                    throw new InvalidOperationException("Verify failed. StringSwitch " + probe);
                }

                if (!(expected == ResolveSpanSwitch(probe.AsSpan())))
                {
                    throw new InvalidOperationException("Verify failed. SpanSwitch " + probe);
                }
            }
        }
    }
}

// Reproduction: 16 keys - Equals chain / string switch / span switch / Dictionary / SampledNameTable
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StringSwitchBenchmark
{
    private const int Count = 16;

    private string[] probes = default!;

    private Dictionary<string, int> dictionary = default!;

    private SampledNameTable<int> table = default!;

    [Params(ProbeSet.Hit, ProbeSet.Miss)]
    public ProbeSet Probe { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var keys = StringSwitchKeys.Medium();
        dictionary = StringSwitchKeys.ToDictionary(keys);
        table = new SampledNameTable<int>(dictionary);
        probes = StringSwitchKeys.ToProbes(Probe == ProbeSet.Hit ? keys : StringSwitchKeys.ToMiss(StringSwitchKeys.Medium()));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int EqualsChain()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveEqualsChain(items[i]);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SpanEqualsChain()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveSpanEqualsChain(items[i].AsSpan());
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int StringSwitch()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveStringSwitch(items[i]);
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SpanSwitch()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveSpanSwitch(items[i].AsSpan());
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int DictionaryLookup()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += dictionary.TryGetValue(items[i], out var value) ? value : -1;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SampledTable()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += table.TryGetValue(items[i].AsSpan(), out var value) ? value : -1;
        }

        return total;
    }

    // Current guidance for small sets: an Equals chain in declaration order
    private static int ResolveEqualsChain(string name)
    {
        if (string.Equals(name, "Id", StringComparison.Ordinal)) { return 0; }
        if (string.Equals(name, "Name", StringComparison.Ordinal)) { return 1; }
        if (string.Equals(name, "Code", StringComparison.Ordinal)) { return 2; }
        if (string.Equals(name, "Type", StringComparison.Ordinal)) { return 3; }
        if (string.Equals(name, "Status", StringComparison.Ordinal)) { return 4; }
        if (string.Equals(name, "Amount", StringComparison.Ordinal)) { return 5; }
        if (string.Equals(name, "Balance", StringComparison.Ordinal)) { return 6; }
        if (string.Equals(name, "Currency", StringComparison.Ordinal)) { return 7; }
        if (string.Equals(name, "CreatedAt", StringComparison.Ordinal)) { return 8; }
        if (string.Equals(name, "UpdatedAt", StringComparison.Ordinal)) { return 9; }
        if (string.Equals(name, "DeletedAt", StringComparison.Ordinal)) { return 10; }
        if (string.Equals(name, "OwnerId", StringComparison.Ordinal)) { return 11; }
        if (string.Equals(name, "ParentId", StringComparison.Ordinal)) { return 12; }
        if (string.Equals(name, "Version", StringComparison.Ordinal)) { return 13; }
        if (string.Equals(name, "IsEnabled", StringComparison.Ordinal)) { return 14; }
        if (string.Equals(name, "Description", StringComparison.Ordinal)) { return 15; }
        return -1;
    }

    // The same chain over a span, so the span switch has a like-for-like baseline
    private static int ResolveSpanEqualsChain(ReadOnlySpan<char> name)
    {
        if (name.SequenceEqual("Id")) { return 0; }
        if (name.SequenceEqual("Name")) { return 1; }
        if (name.SequenceEqual("Code")) { return 2; }
        if (name.SequenceEqual("Type")) { return 3; }
        if (name.SequenceEqual("Status")) { return 4; }
        if (name.SequenceEqual("Amount")) { return 5; }
        if (name.SequenceEqual("Balance")) { return 6; }
        if (name.SequenceEqual("Currency")) { return 7; }
        if (name.SequenceEqual("CreatedAt")) { return 8; }
        if (name.SequenceEqual("UpdatedAt")) { return 9; }
        if (name.SequenceEqual("DeletedAt")) { return 10; }
        if (name.SequenceEqual("OwnerId")) { return 11; }
        if (name.SequenceEqual("ParentId")) { return 12; }
        if (name.SequenceEqual("Version")) { return 13; }
        if (name.SequenceEqual("IsEnabled")) { return 14; }
        if (name.SequenceEqual("Description")) { return 15; }
        return -1;
    }

    // Roslyn turns this into a length bucket then character tests (an FNV hash switch once the set is large enough)
    private static int ResolveStringSwitch(string name) =>
        name switch
        {
            "Id" => 0,
            "Name" => 1,
            "Code" => 2,
            "Type" => 3,
            "Status" => 4,
            "Amount" => 5,
            "Balance" => 6,
            "Currency" => 7,
            "CreatedAt" => 8,
            "UpdatedAt" => 9,
            "DeletedAt" => 10,
            "OwnerId" => 11,
            "ParentId" => 12,
            "Version" => 13,
            "IsEnabled" => 14,
            "Description" => 15,
            _ => -1,
        };

    // C# 11: a ReadOnlySpan<char> constant pattern gets the same treatment, so the generated form can take a span
    private static int ResolveSpanSwitch(ReadOnlySpan<char> name) =>
        name switch
        {
            "Id" => 0,
            "Name" => 1,
            "Code" => 2,
            "Type" => 3,
            "Status" => 4,
            "Amount" => 5,
            "Balance" => 6,
            "Currency" => 7,
            "CreatedAt" => 8,
            "UpdatedAt" => 9,
            "DeletedAt" => 10,
            "OwnerId" => 11,
            "ParentId" => 12,
            "Version" => 13,
            "IsEnabled" => 14,
            "Description" => 15,
            _ => -1,
        };

    public static void Verify()
    {
        var keys = StringSwitchKeys.Medium();
        var dictionary = StringSwitchKeys.ToDictionary(keys);
        var table = new SampledNameTable<int>(dictionary);
        string[][] sources = [keys, StringSwitchKeys.ToMiss(StringSwitchKeys.Medium())];
        foreach (var source in sources)
        {
            foreach (var probe in StringSwitchKeys.ToProbes(source))
            {
                var expected = Array.IndexOf(keys, probe);
                if (!(expected == ResolveEqualsChain(probe)))
                {
                    throw new InvalidOperationException("Verify failed. EqualsChain " + probe);
                }

                if (!(expected == ResolveSpanEqualsChain(probe.AsSpan())))
                {
                    throw new InvalidOperationException("Verify failed. SpanEqualsChain " + probe);
                }

                if (!(expected == ResolveStringSwitch(probe)))
                {
                    throw new InvalidOperationException("Verify failed. StringSwitch " + probe);
                }

                if (!(expected == ResolveSpanSwitch(probe.AsSpan())))
                {
                    throw new InvalidOperationException("Verify failed. SpanSwitch " + probe);
                }

                if (!(expected == (dictionary.TryGetValue(probe, out var d) ? d : -1)))
                {
                    throw new InvalidOperationException("Verify failed. DictionaryLookup " + probe);
                }

                if (!(expected == (table.TryGetValue(probe.AsSpan(), out var t) ? t : -1)))
                {
                    throw new InvalidOperationException("Verify failed. SampledTable " + probe);
                }
            }
        }
    }
}

// Long keys: 16 keys of 58-62 characters - FNV reads every character, the sampling hash reads length + 3
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StringSwitchLongKeyBenchmark
{
    private const int Count = 16;

    private string[] probes = default!;

    private SampledNameTable<int> table = default!;

    [Params(ProbeSet.Hit, ProbeSet.Miss)]
    public ProbeSet Probe { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var keys = StringSwitchKeys.LongKey();
        table = new SampledNameTable<int>(StringSwitchKeys.ToDictionary(keys));
        probes = StringSwitchKeys.ToProbes(Probe == ProbeSet.Hit ? keys : StringSwitchKeys.ToMiss(StringSwitchKeys.LongKey()));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Count)]
    public int SampledSwitch()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveSampledSwitch(items[i].AsSpan());
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SpanSwitch()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += ResolveSpanSwitch(items[i].AsSpan());
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Count)]
    public int SampledTable()
    {
        var total = 0;
        var items = probes;
        for (var i = 0; i < items.Length; i++)
        {
            total += table.TryGetValue(items[i].AsSpan(), out var value) ? value : -1;
        }

        return total;
    }

    // Current guidance above the threshold: sampling-hash switch with the constants folded in at generation time
    // (16 keys collapse to 16 distinct hashes for this key set)
    private static int ResolveSampledSwitch(ReadOnlySpan<char> name) =>
        StringSwitchSamplingHash.Calculate(name) switch
        {
            0x003A5632 when name.SequenceEqual("ResourceExpiryIntervalBarcodeDigestLengthPaymentAuthorCurr") => 0,
            0x003B52A1 when name.SequenceEqual("TargetSequenceTotalEmployeeDeletedAtAmountDepthSalaryBodyHa") => 1,
            0x003C4350 when name.SequenceEqual("DepthShelfNameDestinationUnitPriceUnitPriceSubtotalCipherDep") => 2,
            0x003D4B01 when name.SequenceEqual("LatitudeEmployeeSignatureApproverAudiencePostalCodeInvoiceTra") => 3,
            0x003E4D45 when name.SequenceEqual("IssuerLocaleIdApproverCancelledCountryTypeRefundedTrackingIssu") => 4,
            0x003A4585 when name.SequenceEqual("CarrierBonusPaymentSequenceNonceResourceSerialOriginDelive") => 5,
            0x003B460F when name.SequenceEqual("ApproverVersionNotBeforeIntervalSubtotalCultureAudienceInvo") => 6,
            0x003C5263 when name.SequenceEqual("VersionLabelSeverityPermissionCodeAuthorShelfBalanceTimeoutS") => 7,
            0x003D4575 when name.SequenceEqual("CancelledApproverTitleLabelUpdatedAtCategoryEmailEmployeeLabe") => 8,
            0x003E4775 when name.SequenceEqual("AuthorCultureSkuShippedWeightTrackingEncodingWarehouseBinNotBe") => 9,
            0x003A4BF5 when name.SequenceEqual("ManagerBinAmountAudienceDurationHashNameScopeReturnedShape") => 10,
            0x003B4129 when name.SequenceEqual("DeliveredSessionEncodingTotalUpdatedAtBodyLotEditorCurrency") => 11,
            0x003C4A35 when name.SequenceEqual("LatitudeBarcodeDeletedAtCultureSaltShapeDepartmentIntervalRe") => 12,
            0x003D5332 when name.SequenceEqual("TimezoneBinKeySizeDivisionUpdatedAtLengthSeverityLengthLotPar") => 13,
            0x003E452E when name.SequenceEqual("BarcodeBonusTitleTagStatusAmountParentIdShelfBarcodeKeySizeLon") => 14,
            0x003A47E4 when name.SequenceEqual("AudiencePolicyPostalCodeEncodingCompanyBinSizeBatchSkuSalt") => 15,
            _ => -1,
        };

    // The compiler's own dispatch over the same key set
    private static int ResolveSpanSwitch(ReadOnlySpan<char> name) =>
        name switch
        {
            "ResourceExpiryIntervalBarcodeDigestLengthPaymentAuthorCurr" => 0,
            "TargetSequenceTotalEmployeeDeletedAtAmountDepthSalaryBodyHa" => 1,
            "DepthShelfNameDestinationUnitPriceUnitPriceSubtotalCipherDep" => 2,
            "LatitudeEmployeeSignatureApproverAudiencePostalCodeInvoiceTra" => 3,
            "IssuerLocaleIdApproverCancelledCountryTypeRefundedTrackingIssu" => 4,
            "CarrierBonusPaymentSequenceNonceResourceSerialOriginDelive" => 5,
            "ApproverVersionNotBeforeIntervalSubtotalCultureAudienceInvo" => 6,
            "VersionLabelSeverityPermissionCodeAuthorShelfBalanceTimeoutS" => 7,
            "CancelledApproverTitleLabelUpdatedAtCategoryEmailEmployeeLabe" => 8,
            "AuthorCultureSkuShippedWeightTrackingEncodingWarehouseBinNotBe" => 9,
            "ManagerBinAmountAudienceDurationHashNameScopeReturnedShape" => 10,
            "DeliveredSessionEncodingTotalUpdatedAtBodyLotEditorCurrency" => 11,
            "LatitudeBarcodeDeletedAtCultureSaltShapeDepartmentIntervalRe" => 12,
            "TimezoneBinKeySizeDivisionUpdatedAtLengthSeverityLengthLotPar" => 13,
            "BarcodeBonusTitleTagStatusAmountParentIdShelfBarcodeKeySizeLon" => 14,
            "AudiencePolicyPostalCodeEncodingCompanyBinSizeBatchSkuSalt" => 15,
            _ => -1,
        };

    public static void Verify()
    {
        var keys = StringSwitchKeys.LongKey();
        var table = new SampledNameTable<int>(StringSwitchKeys.ToDictionary(keys));
        string[][] sources = [keys, StringSwitchKeys.ToMiss(StringSwitchKeys.LongKey())];
        foreach (var source in sources)
        {
            foreach (var probe in StringSwitchKeys.ToProbes(source))
            {
                var expected = Array.IndexOf(keys, probe);
                if (!(expected == ResolveSampledSwitch(probe.AsSpan())))
                {
                    throw new InvalidOperationException("Verify failed. SampledSwitch " + probe);
                }

                if (!(expected == ResolveSpanSwitch(probe.AsSpan())))
                {
                    throw new InvalidOperationException("Verify failed. SpanSwitch " + probe);
                }

                if (!(expected == (table.TryGetValue(probe.AsSpan(), out var v) ? v : -1)))
                {
                    throw new InvalidOperationException("Verify failed. SampledTable " + probe);
                }
            }
        }
    }
}
