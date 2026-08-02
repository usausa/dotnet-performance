namespace PerformancePatterns.Benchmarks.Typ;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Typ;

// TYP-02 example: comparing dictionary lookups with a 16-byte struct key across comparers
// (using a struct without IEquatable with the default comparer boxes on every Equals call)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BitwiseComparerBenchmark
{
    private const int Keys = 16;

    private Dictionary<PlainKey, int> plainDefault = default!;

    private Dictionary<EquatableKey, int> equatableDefault = default!;

    private Dictionary<PlainKey, int> plainBitwise = default!;

    private PlainKey[] plainProbes = default!;

    private EquatableKey[] equatableProbes = default!;

    [GlobalSetup]
    public void Setup()
    {
        plainProbes = new PlainKey[Keys];
        equatableProbes = new EquatableKey[Keys];
        for (var i = 0; i < Keys; i++)
        {
            plainProbes[i] = new PlainKey(i, i * 31L);
            equatableProbes[i] = new EquatableKey(i, i * 31L);
        }

        var indexes = Enumerable.Range(0, Keys).ToArray();
        plainDefault = indexes.ToDictionary(i => plainProbes[i], i => i);
        plainBitwise = indexes.ToDictionary(i => plainProbes[i], i => i, BitwiseComparer<PlainKey>.Instance);
        equatableDefault = indexes.ToDictionary(i => equatableProbes[i], i => i);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = Keys)]
    public int DefaultComparerPlain()
    {
        var total = 0;
        foreach (var probe in plainProbes)
        {
            plainDefault.TryGetValue(probe, out var value);
            total += value;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Keys)]
    public int DefaultComparerEquatable()
    {
        var total = 0;
        foreach (var probe in equatableProbes)
        {
            equatableDefault.TryGetValue(probe, out var value);
            total += value;
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = Keys)]
    public int BitwiseComparerPlain()
    {
        var total = 0;
        foreach (var probe in plainProbes)
        {
            plainBitwise.TryGetValue(probe, out var value);
            total += value;
        }

        return total;
    }
}

// 16-byte struct that does not implement IEquatable (the default comparer takes the boxing path)
public readonly struct PlainKey(long a, long b)
{
    public long A { get; } = a;

    public long B { get; } = b;
}

// Struct with the same layout that does implement IEquatable
public readonly struct EquatableKey(long a, long b) : IEquatable<EquatableKey>
{
    public long A { get; } = a;

    public long B { get; } = b;

    public bool Equals(EquatableKey other) => (A == other.A) && (B == other.B);

    public override bool Equals(object? obj) => obj is EquatableKey other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(A, B);

    public static bool operator ==(EquatableKey left, EquatableKey right) => left.Equals(right);

    public static bool operator !=(EquatableKey left, EquatableKey right) => !left.Equals(right);
}
