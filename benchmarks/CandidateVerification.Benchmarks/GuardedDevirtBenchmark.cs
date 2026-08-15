namespace CandidateVerification.Benchmarks;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// C-05 (DSP-02 update candidate): interface dispatch vs manual guarded devirtualization
// (ReferenceEquals check against known singletons, deterministic even under AOT) vs sealed direct call ceiling
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class GuardedDevirtBenchmark
{
    private const int PairCount = 256;

    private readonly Int64KeyEncoding concreteEncoding = Int64KeyEncoding.Instance;

    private IKeyEncodingLike encoding = default!;

    private byte[] left = default!;

    private byte[] right = default!;

    [GlobalSetup]
    public void Setup()
    {
        encoding = CreateEncoding(0);
        left = new byte[PairCount * 8];
        right = new byte[PairCount * 8];
        var seed = 98765u;
        for (var i = 0; i < PairCount; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(left.AsSpan(i * 8, 8), (long)NextRandom(ref seed) << 16);
            BinaryPrimitives.WriteInt64LittleEndian(right.AsSpan(i * 8, 8), (long)NextRandom(ref seed) << 16);
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = PairCount)]
    public int InterfaceDispatch()
    {
        var total = 0;
        for (var i = 0; i < PairCount; i++)
        {
            total += encoding.Compare(left.AsSpan(i * 8, 8), right.AsSpan(i * 8, 8));
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = PairCount)]
    public int GuardedDevirt()
    {
        var total = 0;
        for (var i = 0; i < PairCount; i++)
        {
            total += KeyCompareHelper.Compare(encoding, left.AsSpan(i * 8, 8), right.AsSpan(i * 8, 8));
        }

        return total;
    }

    [Benchmark(OperationsPerInvoke = PairCount)]
    public int SealedDirect()
    {
        var total = 0;
        for (var i = 0; i < PairCount; i++)
        {
            total += concreteEncoding.Compare(left.AsSpan(i * 8, 8), right.AsSpan(i * 8, 8));
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new GuardedDevirtBenchmark();
        benchmark.Setup();

        var viaInterface = benchmark.InterfaceDispatch();
        var viaGuard = benchmark.GuardedDevirt();
        var viaSealed = benchmark.SealedDirect();
        if ((viaInterface != viaGuard) || (viaInterface != viaSealed))
        {
            throw new InvalidOperationException($"Compare variants disagree: {viaInterface} / {viaGuard} / {viaSealed}.");
        }

        // The fallback path must also agree
        var ascii = KeyCompareHelper.Compare(AsciiKeyEncoding.Instance, "abc"u8, "abd"u8);
        if (ascii >= 0)
        {
            throw new InvalidOperationException("Ascii guarded compare is wrong.");
        }
    }

    // Opaque factory: the concrete type must stay unknown to the compiler so the field keeps its interface type
    private static IKeyEncodingLike CreateEncoding(int kind)
        => kind == 0 ? Int64KeyEncoding.Instance : AsciiKeyEncoding.Instance;

    private static uint NextRandom(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }
}

public interface IKeyEncodingLike
{
    int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);
}

public sealed class Int64KeyEncoding : IKeyEncodingLike
{
    public static readonly Int64KeyEncoding Instance = new();

    private Int64KeyEncoding()
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var na = BinaryPrimitives.ReadInt64LittleEndian(a);
        var nb = BinaryPrimitives.ReadInt64LittleEndian(b);
        return (na > nb ? 1 : 0) - (na < nb ? 1 : 0);
    }
}

public sealed class AsciiKeyEncoding : IKeyEncodingLike
{
    public static readonly AsciiKeyEncoding Instance = new();

    private AsciiKeyEncoding()
    {
    }

    public int Compare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b) => a.SequenceCompareTo(b);
}

public static class KeyCompareHelper
{
    // Manual guarded devirtualization: deterministic (works under AOT), unlike tiered-PGO GDV
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(IKeyEncodingLike encoding, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        if (ReferenceEquals(encoding, Int64KeyEncoding.Instance))
        {
            var na = BinaryPrimitives.ReadInt64LittleEndian(a);
            var nb = BinaryPrimitives.ReadInt64LittleEndian(b);
            return (na > nb ? 1 : 0) - (na < nb ? 1 : 0);
        }

        if (ReferenceEquals(encoding, AsciiKeyEncoding.Instance))
        {
            return a.SequenceCompareTo(b);
        }

        return encoding.Compare(a, b);
    }
}
