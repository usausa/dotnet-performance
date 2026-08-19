namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Col;

// TXT-10 (scale): where the compiler's own dispatch stops being competitive.
// Shared key sets and the sampling hash live in StringSwitchDispatchBenchmark.cs.

// Scale: 64 keys - the range where Roslyn switches to an FNV hash dispatch
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StringSwitch64Benchmark
{
    private const int Count = 64;

    private string[] probes = default!;

    private SampledNameTable<int> table = default!;

    [Params(ProbeSet.Hit, ProbeSet.Miss)]
    public ProbeSet Probe { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var keys = StringSwitchKeys.Large64();
        table = new SampledNameTable<int>(StringSwitchKeys.ToDictionary(keys));
        probes = StringSwitchKeys.ToProbes(Probe == ProbeSet.Hit ? keys : StringSwitchKeys.ToMiss(StringSwitchKeys.Large64()));
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
    // (64 keys collapse to 63 distinct hashes for this key set)
    private static int ResolveSampledSwitch(ReadOnlySpan<char> name) =>
        StringSwitchSamplingHash.Calculate(name) switch
        {
            0x00024F24 when name.SequenceEqual("Id") => 0,
            0x000448B5 when name.SequenceEqual("Name") => 1,
            0x00044525 when name.SequenceEqual("Code") => 2,
            0x00045365 when name.SequenceEqual("Type") => 3,
            0x00065433 when name.SequenceEqual("Status") => 4,
            0x00064624 when name.SequenceEqual("Amount") => 5,
            0x00074475 when name.SequenceEqual("Balance") => 6,
            0x00084529 when name.SequenceEqual("Currency") => 7,
            0x00094434 when name.SequenceEqual("CreatedAt") => 8,
            0x00095234 when name.SequenceEqual("UpdatedAt") => 9,
            0x00094334 when name.SequenceEqual("DeletedAt") => 10,
            0x00074934 when name.SequenceEqual("OwnerId") => 11,
            0x00085684 when name.SequenceEqual("ParentId") => 12,
            0x0007515E when name.SequenceEqual("Version") => 13,
            0x00094F74 when name.SequenceEqual("IsEnabled") => 14,
            0x000B42FE when name.SequenceEqual("Description") => 15,
            0x00055325 when name.SequenceEqual("Title") => 16,
            0x000755A9 when name.SequenceEqual("Summary") => 17,
            0x00044439 when name.SequenceEqual("Body") => 18,
            0x00044439 when name.SequenceEqual("City") => 60,
            0x000647F2 when name.SequenceEqual("Author") => 19,
            0x00064232 when name.SequenceEqual("Editor") => 20,
            0x00085422 when name.SequenceEqual("Reviewer") => 21,
            0x00084782 when name.SequenceEqual("Approver") => 22,
            0x00084509 when name.SequenceEqual("Category") => 23,
            0x00035277 when name.SequenceEqual("Tag") => 24,
            0x00054A4C when name.SequenceEqual("Label") => 25,
            0x00085759 when name.SequenceEqual("Priority") => 26,
            0x00085459 when name.SequenceEqual("Severity") => 27,
            0x00065445 when name.SequenceEqual("Source") => 28,
            0x00065204 when name.SequenceEqual("Target") => 29,
            0x0006491E when name.SequenceEqual("Origin") => 30,
            0x000B428E when name.SequenceEqual("Destination") => 31,
            0x00084B25 when name.SequenceEqual("Latitude") => 32,
            0x00094AF5 when name.SequenceEqual("Longitude") => 33,
            0x00084625 when name.SequenceEqual("Altitude") => 34,
            0x00084275 when name.SequenceEqual("Distance") => 35,
            0x0008432E when name.SequenceEqual("Duration") => 36,
            0x00084E4C when name.SequenceEqual("Interval") => 37,
            0x00075224 when name.SequenceEqual("Timeout") => 38,
            0x00055539 when name.SequenceEqual("Retry") => 39,
            0x00074724 when name.SequenceEqual("Attempt") => 40,
            0x00085535 when name.SequenceEqual("Sequence") => 41,
            0x0008572E when name.SequenceEqual("Position") => 42,
            0x00064844 when name.SequenceEqual("Offset") => 43,
            0x00064A18 when name.SequenceEqual("Length") => 44,
            0x00084549 when name.SequenceEqual("Capacity") => 45,
            0x000454C5 when name.SequenceEqual("Size") => 46,
            0x00065104 when name.SequenceEqual("Weight") => 47,
            0x00064E04 when name.SequenceEqual("Height") => 48,
            0x00055128 when name.SequenceEqual("Width") => 49,
            0x00054368 when name.SequenceEqual("Depth") => 50,
            0x000545B2 when name.SequenceEqual("Color") => 51,
            0x00055575 when name.SequenceEqual("Shape") => 52,
            0x000640A4 when name.SequenceEqual("Format") => 53,
            0x00084327 when name.SequenceEqual("Encoding") => 54,
            0x00074425 when name.SequenceEqual("Culture") => 55,
            0x00064A75 when name.SequenceEqual("Locale") => 56,
            0x000853C5 when name.SequenceEqual("Timezone") => 57,
            0x00074599 when name.SequenceEqual("Country") => 58,
            0x000654FE when name.SequenceEqual("Region") => 59,
            0x00065524 when name.SequenceEqual("Street") => 61,
            0x000A56A5 when name.SequenceEqual("PostalCode") => 62,
            0x00055695 when name.SequenceEqual("Phone") => 63,
            _ => -1,
        };

    // The compiler's own dispatch over the same key set
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
            "Title" => 16,
            "Summary" => 17,
            "Body" => 18,
            "Author" => 19,
            "Editor" => 20,
            "Reviewer" => 21,
            "Approver" => 22,
            "Category" => 23,
            "Tag" => 24,
            "Label" => 25,
            "Priority" => 26,
            "Severity" => 27,
            "Source" => 28,
            "Target" => 29,
            "Origin" => 30,
            "Destination" => 31,
            "Latitude" => 32,
            "Longitude" => 33,
            "Altitude" => 34,
            "Distance" => 35,
            "Duration" => 36,
            "Interval" => 37,
            "Timeout" => 38,
            "Retry" => 39,
            "Attempt" => 40,
            "Sequence" => 41,
            "Position" => 42,
            "Offset" => 43,
            "Length" => 44,
            "Capacity" => 45,
            "Size" => 46,
            "Weight" => 47,
            "Height" => 48,
            "Width" => 49,
            "Depth" => 50,
            "Color" => 51,
            "Shape" => 52,
            "Format" => 53,
            "Encoding" => 54,
            "Culture" => 55,
            "Locale" => 56,
            "Timezone" => 57,
            "Country" => 58,
            "Region" => 59,
            "City" => 60,
            "Street" => 61,
            "PostalCode" => 62,
            "Phone" => 63,
            _ => -1,
        };

    public static void Verify()
    {
        var keys = StringSwitchKeys.Large64();
        var table = new SampledNameTable<int>(StringSwitchKeys.ToDictionary(keys));
        string[][] sources = [keys, StringSwitchKeys.ToMiss(StringSwitchKeys.Large64())];
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

// Scale: 128 keys
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class StringSwitch128Benchmark
{
    private const int Count = 128;

    private string[] probes = default!;

    private SampledNameTable<int> table = default!;

    [Params(ProbeSet.Hit, ProbeSet.Miss)]
    public ProbeSet Probe { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var keys = StringSwitchKeys.Large128();
        table = new SampledNameTable<int>(StringSwitchKeys.ToDictionary(keys));
        probes = StringSwitchKeys.ToProbes(Probe == ProbeSet.Hit ? keys : StringSwitchKeys.ToMiss(StringSwitchKeys.Large128()));
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
    // (128 keys collapse to 125 distinct hashes for this key set)
    private static int ResolveSampledSwitch(ReadOnlySpan<char> name) =>
        StringSwitchSamplingHash.Calculate(name) switch
        {
            0x00024F24 when name.SequenceEqual("Id") => 0,
            0x000448B5 when name.SequenceEqual("Name") => 1,
            0x00044525 when name.SequenceEqual("Code") => 2,
            0x00045365 when name.SequenceEqual("Type") => 3,
            0x00065433 when name.SequenceEqual("Status") => 4,
            0x00064624 when name.SequenceEqual("Amount") => 5,
            0x00074475 when name.SequenceEqual("Balance") => 6,
            0x00084529 when name.SequenceEqual("Currency") => 7,
            0x00094434 when name.SequenceEqual("CreatedAt") => 8,
            0x00095234 when name.SequenceEqual("UpdatedAt") => 9,
            0x00094334 when name.SequenceEqual("DeletedAt") => 10,
            0x00074934 when name.SequenceEqual("OwnerId") => 11,
            0x00085684 when name.SequenceEqual("ParentId") => 12,
            0x0007515E when name.SequenceEqual("Version") => 13,
            0x00094F74 when name.SequenceEqual("IsEnabled") => 14,
            0x000B42FE when name.SequenceEqual("Description") => 15,
            0x00055325 when name.SequenceEqual("Title") => 16,
            0x000755A9 when name.SequenceEqual("Summary") => 17,
            0x00044439 when name.SequenceEqual("Body") => 18,
            0x00044439 when name.SequenceEqual("City") => 60,
            0x000647F2 when name.SequenceEqual("Author") => 19,
            0x00064232 when name.SequenceEqual("Editor") => 20,
            0x00085422 when name.SequenceEqual("Reviewer") => 21,
            0x00084782 when name.SequenceEqual("Approver") => 22,
            0x00084509 when name.SequenceEqual("Category") => 23,
            0x00035277 when name.SequenceEqual("Tag") => 24,
            0x00054A4C when name.SequenceEqual("Label") => 25,
            0x00085759 when name.SequenceEqual("Priority") => 26,
            0x00085459 when name.SequenceEqual("Severity") => 27,
            0x00065445 when name.SequenceEqual("Source") => 28,
            0x00065204 when name.SequenceEqual("Target") => 29,
            0x0006491E when name.SequenceEqual("Origin") => 30,
            0x000B428E when name.SequenceEqual("Destination") => 31,
            0x00084B25 when name.SequenceEqual("Latitude") => 32,
            0x00094AF5 when name.SequenceEqual("Longitude") => 33,
            0x00084625 when name.SequenceEqual("Altitude") => 34,
            0x00084275 when name.SequenceEqual("Distance") => 35,
            0x0008432E when name.SequenceEqual("Duration") => 36,
            0x00084E4C when name.SequenceEqual("Interval") => 37,
            0x00075224 when name.SequenceEqual("Timeout") => 38,
            0x00055539 when name.SequenceEqual("Retry") => 39,
            0x00074724 when name.SequenceEqual("Attempt") => 40,
            0x00085535 when name.SequenceEqual("Sequence") => 41,
            0x00085535 when name.SequenceEqual("Resource") => 125,
            0x0008572E when name.SequenceEqual("Position") => 42,
            0x00064844 when name.SequenceEqual("Offset") => 43,
            0x00064A18 when name.SequenceEqual("Length") => 44,
            0x00084549 when name.SequenceEqual("Capacity") => 45,
            0x000454C5 when name.SequenceEqual("Size") => 46,
            0x00065104 when name.SequenceEqual("Weight") => 47,
            0x00064E04 when name.SequenceEqual("Height") => 48,
            0x00055128 when name.SequenceEqual("Width") => 49,
            0x00054368 when name.SequenceEqual("Depth") => 50,
            0x000545B2 when name.SequenceEqual("Color") => 51,
            0x00055575 when name.SequenceEqual("Shape") => 52,
            0x000640A4 when name.SequenceEqual("Format") => 53,
            0x00084327 when name.SequenceEqual("Encoding") => 54,
            0x00074425 when name.SequenceEqual("Culture") => 55,
            0x00064A75 when name.SequenceEqual("Locale") => 56,
            0x000853C5 when name.SequenceEqual("Timezone") => 57,
            0x00074599 when name.SequenceEqual("Country") => 58,
            0x000654FE when name.SequenceEqual("Region") => 59,
            0x00065524 when name.SequenceEqual("Street") => 61,
            0x000A56A5 when name.SequenceEqual("PostalCode") => 62,
            0x00055695 when name.SequenceEqual("Phone") => 63,
            0x0005437C when name.SequenceEqual("Email") => 64,
            0x00075055 when name.SequenceEqual("Website") => 65,
            0x00074479 when name.SequenceEqual("Company") => 66,
            0x000A4334 when name.SequenceEqual("Department") => 67,
            0x0008435E when name.SequenceEqual("Division") => 68,
            0x00074B62 when name.SequenceEqual("Manager") => 69,
            0x00084395 when name.SequenceEqual("Employee") => 70,
            0x00065569 when name.SequenceEqual("Salary") => 71,
            0x00054493 when name.SequenceEqual("Bonus") => 72,
            0x00084284 when name.SequenceEqual("Discount") => 73,
            0x00075145 when name.SequenceEqual("TaxRate") => 74,
            0x0008559C when name.SequenceEqual("Subtotal") => 75,
            0x0005532C when name.SequenceEqual("Total") => 76,
            0x00085639 when name.SequenceEqual("Quantity") => 77,
            0x00095065 when name.SequenceEqual("UnitPrice") => 78,
            0x000355C5 when name.SequenceEqual("Sku") => 79,
            0x00074455 when name.SequenceEqual("Barcode") => 80,
            0x000655FC when name.SequenceEqual("Serial") => 81,
            0x00054528 when name.SequenceEqual("Batch") => 82,
            0x00034A84 when name.SequenceEqual("Lot") => 83,
            0x000951E5 when name.SequenceEqual("Warehouse") => 84,
            0x00055536 when name.SequenceEqual("Shelf") => 85,
            0x000344FE when name.SequenceEqual("Bin") => 86,
            0x00074452 when name.SequenceEqual("Carrier") => 87,
            0x000852D7 when name.SequenceEqual("Tracking") => 88,
            0x00075464 when name.SequenceEqual("Shipped") => 89,
            0x00094304 when name.SequenceEqual("Delivered") => 90,
            0x00085544 when name.SequenceEqual("Returned") => 91,
            0x00094534 when name.SequenceEqual("Cancelled") => 92,
            0x00085484 when name.SequenceEqual("Refunded") => 93,
            0x00074F95 when name.SequenceEqual("Invoice") => 94,
            0x00075424 when name.SequenceEqual("Receipt") => 95,
            0x000756A4 when name.SequenceEqual("Payment") => 96,
            0x00064BE4 when name.SequenceEqual("Method") => 97,
            0x000856E2 when name.SequenceEqual("Provider") => 98,
            0x00074129 when name.SequenceEqual("Gateway") => 99,
            0x000552DE when name.SequenceEqual("Token") => 100,
            0x00065454 when name.SequenceEqual("Secret") => 101,
            0x00095575 when name.SequenceEqual("Signature") => 102,
            0x000845DD when name.SequenceEqual("Checksum") => 103,
            0x00064224 when name.SequenceEqual("Digest") => 104,
            0x00054885 when name.SequenceEqual("Nonce") => 105,
            0x000455B4 when name.SequenceEqual("Salt") => 106,
            0x00044F58 when name.SequenceEqual("Hash") => 107,
            0x000645F2 when name.SequenceEqual("Cipher") => 108,
            0x0009464D when name.SequenceEqual("Algorithm") => 109,
            0x00074E55 when name.SequenceEqual("KeySize") => 110,
            0x000643E9 when name.SequenceEqual("Expiry") => 111,
            0x00084F24 when name.SequenceEqual("IssuedAt") => 112,
            0x00094835 when name.SequenceEqual("NotBefore") => 113,
            0x00084812 when name.SequenceEqual("NotAfter") => 114,
            0x00064E22 when name.SequenceEqual("Issuer") => 115,
            0x000755D4 when name.SequenceEqual("Subject") => 116,
            0x00084735 when name.SequenceEqual("Audience") => 117,
            0x00055595 when name.SequenceEqual("Scope") => 118,
            0x000454A5 when name.SequenceEqual("Role") => 119,
            0x000454A5 when name.SequenceEqual("Rule") => 122,
            0x000A575E when name.SequenceEqual("Permission") => 120,
            0x000656E9 when name.SequenceEqual("Policy") => 121,
            0x000945FE when name.SequenceEqual("Condition") => 123,
            0x00064324 when name.SequenceEqual("Effect") => 124,
            0x0007545E when name.SequenceEqual("Session") => 126,
            0x00075524 when name.SequenceEqual("Request") => 127,
            _ => -1,
        };

    // The compiler's own dispatch over the same key set
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
            "Title" => 16,
            "Summary" => 17,
            "Body" => 18,
            "Author" => 19,
            "Editor" => 20,
            "Reviewer" => 21,
            "Approver" => 22,
            "Category" => 23,
            "Tag" => 24,
            "Label" => 25,
            "Priority" => 26,
            "Severity" => 27,
            "Source" => 28,
            "Target" => 29,
            "Origin" => 30,
            "Destination" => 31,
            "Latitude" => 32,
            "Longitude" => 33,
            "Altitude" => 34,
            "Distance" => 35,
            "Duration" => 36,
            "Interval" => 37,
            "Timeout" => 38,
            "Retry" => 39,
            "Attempt" => 40,
            "Sequence" => 41,
            "Position" => 42,
            "Offset" => 43,
            "Length" => 44,
            "Capacity" => 45,
            "Size" => 46,
            "Weight" => 47,
            "Height" => 48,
            "Width" => 49,
            "Depth" => 50,
            "Color" => 51,
            "Shape" => 52,
            "Format" => 53,
            "Encoding" => 54,
            "Culture" => 55,
            "Locale" => 56,
            "Timezone" => 57,
            "Country" => 58,
            "Region" => 59,
            "City" => 60,
            "Street" => 61,
            "PostalCode" => 62,
            "Phone" => 63,
            "Email" => 64,
            "Website" => 65,
            "Company" => 66,
            "Department" => 67,
            "Division" => 68,
            "Manager" => 69,
            "Employee" => 70,
            "Salary" => 71,
            "Bonus" => 72,
            "Discount" => 73,
            "TaxRate" => 74,
            "Subtotal" => 75,
            "Total" => 76,
            "Quantity" => 77,
            "UnitPrice" => 78,
            "Sku" => 79,
            "Barcode" => 80,
            "Serial" => 81,
            "Batch" => 82,
            "Lot" => 83,
            "Warehouse" => 84,
            "Shelf" => 85,
            "Bin" => 86,
            "Carrier" => 87,
            "Tracking" => 88,
            "Shipped" => 89,
            "Delivered" => 90,
            "Returned" => 91,
            "Cancelled" => 92,
            "Refunded" => 93,
            "Invoice" => 94,
            "Receipt" => 95,
            "Payment" => 96,
            "Method" => 97,
            "Provider" => 98,
            "Gateway" => 99,
            "Token" => 100,
            "Secret" => 101,
            "Signature" => 102,
            "Checksum" => 103,
            "Digest" => 104,
            "Nonce" => 105,
            "Salt" => 106,
            "Hash" => 107,
            "Cipher" => 108,
            "Algorithm" => 109,
            "KeySize" => 110,
            "Expiry" => 111,
            "IssuedAt" => 112,
            "NotBefore" => 113,
            "NotAfter" => 114,
            "Issuer" => 115,
            "Subject" => 116,
            "Audience" => 117,
            "Scope" => 118,
            "Role" => 119,
            "Permission" => 120,
            "Policy" => 121,
            "Rule" => 122,
            "Condition" => 123,
            "Effect" => 124,
            "Resource" => 125,
            "Session" => 126,
            "Request" => 127,
            _ => -1,
        };

    public static void Verify()
    {
        var keys = StringSwitchKeys.Large128();
        var table = new SampledNameTable<int>(StringSwitchKeys.ToDictionary(keys));
        string[][] sources = [keys, StringSwitchKeys.ToMiss(StringSwitchKeys.Large128())];
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
