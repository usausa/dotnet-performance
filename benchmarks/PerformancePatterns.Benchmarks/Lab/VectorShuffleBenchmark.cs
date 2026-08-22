namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 7-9: fixed width intrinsics, the case VEC-01 defers on.
// VEC-01 concludes "default to the width agnostic Vector<T>, drop to Vector128/256 only when the algorithm
// requires a specific lane arrangement" but never measures that second case, so the catalog has no numbers for
// the shape it tells the reader to reach for. A byte shuffle is exactly that shape: Vector<T> cannot express it.
// The task is endianness reversal of uint values, which is a lane permutation and nothing else.
// Question A: what does the byte shuffle buy over BinaryPrimitives.ReverseEndianness in a loop?
// Question B: does the portable Vector128.Shuffle (which normalizes indices) lose to the raw Ssse3.Shuffle?
// Question C: how close does the width agnostic Vector<T> arithmetic form get without a shuffle at all?
// Note: the element count is deliberately not a multiple of the vector width so the scalar tail is exercised.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class VectorShuffleBenchmark
{
    private const int Count = 1021;

    private static readonly Vector128<byte> ReverseMask = Vector128.Create(
        (byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12);

    private uint[] source = default!;

    private uint[] destination = default!;

    [GlobalSetup]
    public void Setup()
    {
        source = new uint[Count];
        destination = new uint[Count];
        var state = 12345U;
        for (var i = 0; i < source.Length; i++)
        {
            state = (state * 1664525U) + 1013904223U;
            source[i] = state;
        }
    }

    [Benchmark(Baseline = true)]
    public uint ScalarReverse()
    {
        var src = source.AsSpan();
        var dst = destination.AsSpan();
        for (var i = 0; i < src.Length; i++)
        {
            dst[i] = BinaryPrimitives.ReverseEndianness(src[i]);
        }

        return dst[^1];
    }

    [Benchmark]
    public uint Vector128ShuffleReverse()
    {
        var src = source.AsSpan();
        var dst = destination.AsSpan();
        var i = 0;
        if (Vector128.IsHardwareAccelerated)
        {
            ref var srcHead = ref MemoryMarshal.GetReference(src);
            ref var dstHead = ref MemoryMarshal.GetReference(dst);
            var lanes = Vector128<uint>.Count;
            for (; i <= src.Length - lanes; i += lanes)
            {
                var loaded = Vector128.LoadUnsafe(ref srcHead, (nuint)i);
                Vector128.Shuffle(loaded.AsByte(), ReverseMask).AsUInt32().StoreUnsafe(ref dstHead, (nuint)i);
            }
        }

        for (; i < src.Length; i++)
        {
            dst[i] = BinaryPrimitives.ReverseEndianness(src[i]);
        }

        return dst[^1];
    }

    [Benchmark]
    public uint Ssse3ShuffleReverse()
    {
        var src = source.AsSpan();
        var dst = destination.AsSpan();
        var i = 0;
        if (Ssse3.IsSupported)
        {
            ref var srcHead = ref MemoryMarshal.GetReference(src);
            ref var dstHead = ref MemoryMarshal.GetReference(dst);
            var lanes = Vector128<uint>.Count;
            for (; i <= src.Length - lanes; i += lanes)
            {
                var loaded = Vector128.LoadUnsafe(ref srcHead, (nuint)i);
                Ssse3.Shuffle(loaded.AsByte(), ReverseMask).AsUInt32().StoreUnsafe(ref dstHead, (nuint)i);
            }
        }

        for (; i < src.Length; i++)
        {
            dst[i] = BinaryPrimitives.ReverseEndianness(src[i]);
        }

        return dst[^1];
    }

    // Width agnostic form: no shuffle exists for Vector<T>, so the permutation has to be built from shifts and masks
    [Benchmark]
    public uint VectorArithmeticReverse()
    {
        var src = source.AsSpan();
        var dst = destination.AsSpan();
        var i = 0;
        if (Vector.IsHardwareAccelerated)
        {
            var lanes = Vector<uint>.Count;
            var byte0 = new Vector<uint>(0x000000FFU);
            var byte1 = new Vector<uint>(0x0000FF00U);
            var byte2 = new Vector<uint>(0x00FF0000U);
            var byte3 = new Vector<uint>(0xFF000000U);
            for (; i <= src.Length - lanes; i += lanes)
            {
                var loaded = new Vector<uint>(src.Slice(i, lanes));
                var reversed =
                    Vector.ShiftLeft(loaded & byte0, 24) |
                    Vector.ShiftLeft(loaded & byte1, 8) |
                    Vector.ShiftRightLogical(loaded & byte2, 8) |
                    Vector.ShiftRightLogical(loaded & byte3, 24);
                reversed.CopyTo(dst.Slice(i, lanes));
            }
        }

        for (; i < src.Length; i++)
        {
            dst[i] = BinaryPrimitives.ReverseEndianness(src[i]);
        }

        return dst[^1];
    }

    public static void Verify()
    {
        var benchmark = new VectorShuffleBenchmark();
        benchmark.Setup();

        benchmark.ScalarReverse();
        var expected = benchmark.destination.ToArray();

        var variants = new (string Name, Func<uint> Run)[]
        {
            ("Vector128Shuffle", benchmark.Vector128ShuffleReverse),
            ("Ssse3Shuffle", benchmark.Ssse3ShuffleReverse),
            ("VectorArithmetic", benchmark.VectorArithmeticReverse)
        };

        foreach (var (name, run) in variants)
        {
            Array.Clear(benchmark.destination);
            run();
            if (!benchmark.destination.AsSpan().SequenceEqual(expected))
            {
                throw new InvalidOperationException($"Verify failed. VectorShuffle. name=[{name}]");
            }
        }
    }
}
