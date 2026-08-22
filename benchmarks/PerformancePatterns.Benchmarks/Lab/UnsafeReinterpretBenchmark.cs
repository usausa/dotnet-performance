namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 7-7: Unsafe.BitCast (.NET 8+) as the safe successor to Unsafe.As<TFrom, TTo>(ref v).
// The catalog uses Unsafe.As in the body of JIT-03 and SEQ-02 but only lists BitCast in the API table, which
// puts the unsafe form in front of readers and the checked form out of sight.
// Question A: does BitCast cost anything over Unsafe.As for a concrete value type pair?
// Question B: does it still cost nothing inside a generic method, where its size check cannot be folded until
//             the instantiation is known (this is the JIT-03 shape)?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class BitCastBenchmark
{
    private const int Count = 1024;

    private float[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new float[Count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = (i * 1.5f) + 0.25f;
        }
    }

    [Benchmark(Baseline = true)]
    public long UnsafeAsReinterpret()
    {
        var span = values.AsSpan();
        var total = 0L;
        for (var i = 0; i < span.Length; i++)
        {
            var value = span[i];
            total += Unsafe.As<float, int>(ref value);
        }

        return total;
    }

    [Benchmark]
    public long UnsafeBitCastReinterpret()
    {
        var span = values.AsSpan();
        var total = 0L;
        for (var i = 0; i < span.Length; i++)
        {
            total += Unsafe.BitCast<float, int>(span[i]);
        }

        return total;
    }

    [Benchmark]
    public long BitConverterReinterpret()
    {
        var span = values.AsSpan();
        var total = 0L;
        for (var i = 0; i < span.Length; i++)
        {
            total += BitConverter.SingleToInt32Bits(span[i]);
        }

        return total;
    }

    // The JIT-03 shape: reinterpret inside a generic method
    [Benchmark]
    public long GenericUnsafeAs()
    {
        var total = 0L;
        for (var i = 0; i < Count; i++)
        {
            total += ConvertWithAs<int>(i);
        }

        return total;
    }

    [Benchmark]
    public long GenericBitCast()
    {
        var total = 0L;
        for (var i = 0; i < Count; i++)
        {
            total += ConvertWithBitCast<int>(i);
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new BitCastBenchmark();
        benchmark.Setup();

        var expected = benchmark.UnsafeAsReinterpret();
        if ((benchmark.UnsafeBitCastReinterpret() != expected) || (benchmark.BitConverterReinterpret() != expected))
        {
            throw new InvalidOperationException("Verify failed. BitCast scalar.");
        }

        if (benchmark.GenericUnsafeAs() != benchmark.GenericBitCast())
        {
            throw new InvalidOperationException("Verify failed. BitCast generic.");
        }
    }

    private static T ConvertWithAs<T>(int value)
        where T : struct
        => Unsafe.As<int, T>(ref value);

    private static T ConvertWithBitCast<T>(int value)
        where T : struct
        => Unsafe.BitCast<int, T>(value);
}

// Study queue 7-8: reinterpreting a span, and the two traps the catalog recommends Cast without mentioning.
// BIT-04 and R-09 both tell the reader to prefer MemoryMarshal.Cast over fixed, calling it zero cost, but the
// catalog never states that Cast performs no alignment check (DataMisalignedException territory on Arm) and
// that it silently truncates when the element sizes differ.
// Question: is Cast actually free against the element-at-a-time alternatives, and how do the alternatives read?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class SpanReinterpretBenchmark
{
    private const int IntCount = 1024;

    private byte[] bytes = default!;

    [GlobalSetup]
    public void Setup()
    {
        bytes = new byte[IntCount * sizeof(int)];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i & 0xFF);
        }
    }

    [Benchmark(Baseline = true)]
    public long MemoryMarshalCast()
    {
        var span = MemoryMarshal.Cast<byte, int>(bytes);
        var total = 0L;
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public long UnsafeReadUnaligned()
    {
        ref var first = ref MemoryMarshal.GetArrayDataReference(bytes);
        var total = 0L;
        for (var i = 0; i < IntCount; i++)
        {
            total += Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref first, i * sizeof(int)));
        }

        return total;
    }

    [Benchmark]
    public long BinaryPrimitivesRead()
    {
        var span = bytes.AsSpan();
        var total = 0L;
        for (var i = 0; i < IntCount; i++)
        {
            total += BinaryPrimitives.ReadInt32LittleEndian(span.Slice(i * sizeof(int), sizeof(int)));
        }

        return total;
    }

    public static void Verify()
    {
        var benchmark = new SpanReinterpretBenchmark();
        benchmark.Setup();

        var expected = benchmark.MemoryMarshalCast();
        if ((benchmark.UnsafeReadUnaligned() != expected) || (benchmark.BinaryPrimitivesRead() != expected))
        {
            throw new InvalidOperationException("Verify failed. SpanReinterpret sum.");
        }

        // Trap 1: widening truncates the tail instead of throwing. 10 bytes reinterpreted as int gives 2 elements
        // and the last 2 bytes silently disappear.
        var ragged = MemoryMarshal.Cast<byte, int>(new byte[10]);
        if (ragged.Length != 2)
        {
            throw new InvalidOperationException($"Verify failed. SpanReinterpret truncation. length=[{ragged.Length}]");
        }

        // Trap 2: narrowing multiplies the length, which is where the int overflow guard lives
        var widened = MemoryMarshal.Cast<int, byte>(new int[3]);
        if (widened.Length != 12)
        {
            throw new InvalidOperationException($"Verify failed. SpanReinterpret widening. length=[{widened.Length}]");
        }
    }
}

// Study queue 7-11: ref identity and ref arithmetic (Unsafe.AreSame / Unsafe.ByteOffset / MemoryExtensions.Overlaps).
// None of these appear anywhere in the catalog, yet they are the only way to answer "is this the same buffer"
// and "which index is this ref" once code is written against refs rather than indexes.
// Question A: is recovering the index from a ref cheaper than simply carrying one (R-02 says manual ref walking
//             loses, so the expectation here is that carrying the index also wins)?
// Question B: what does an aliasing guard cost, comparing Unsafe.AreSame on the first elements against Overlaps?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RefIdentityBenchmark
{
    private const int Count = 1024;

    private int[] values = default!;

    private int[] other = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[Count];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i & 0xFF;
        }

        other = new int[Count];
    }

    // --- Question A ---

    [Benchmark(Baseline = true)]
    public long IndexCarried()
    {
        var span = values.AsSpan();
        var total = 0L;
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i] * (long)i;
        }

        return total;
    }

    [Benchmark]
    public long IndexRecoveredFromRef()
    {
        var span = values.AsSpan();
        ref var first = ref MemoryMarshal.GetReference(span);
        var total = 0L;
        foreach (ref var item in span)
        {
            var index = (long)(Unsafe.ByteOffset(ref first, ref item) / sizeof(int));
            total += item * index;
        }

        return total;
    }

    // --- Question B ---

    [Benchmark]
    public int AliasCheckWithAreSame()
    {
        var hits = 0;
        for (var i = 0; i < Count; i++)
        {
            var left = values.AsSpan();
            var right = ((i & 1) == 0 ? values : other).AsSpan();
            if (Unsafe.AreSame(ref MemoryMarshal.GetReference(left), ref MemoryMarshal.GetReference(right)))
            {
                hits++;
            }
        }

        return hits;
    }

    [Benchmark]
    public int AliasCheckWithOverlaps()
    {
        var hits = 0;
        for (var i = 0; i < Count; i++)
        {
            var left = values.AsSpan();
            var right = ((i & 1) == 0 ? values : other).AsSpan();
            if (left.Overlaps(right))
            {
                hits++;
            }
        }

        return hits;
    }

    public static void Verify()
    {
        var benchmark = new RefIdentityBenchmark();
        benchmark.Setup();

        if (benchmark.IndexCarried() != benchmark.IndexRecoveredFromRef())
        {
            throw new InvalidOperationException("Verify failed. RefIdentity index recovery.");
        }

        if (benchmark.AliasCheckWithAreSame() != benchmark.AliasCheckWithOverlaps())
        {
            throw new InvalidOperationException("Verify failed. RefIdentity alias check.");
        }
    }
}

// Study queue 7-12: Unsafe.Unbox<T>, the only way to mutate a boxed value type without producing a new box.
// STK-05 covers avoiding boxing in the first place; this covers the case where the box already exists and is
// handed to you (an object field, a dictionary of object, an interop boundary).
// Question: how much does the rebox cost, and does the in place form stay allocation free?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class UnboxInPlaceBenchmark
{
    private const int Count = 256;

    private object[] boxes = default!;

    [GlobalSetup]
    public void Setup()
    {
        boxes = new object[Count];
        for (var i = 0; i < boxes.Length; i++)
        {
            boxes[i] = new BoxedCounter { Count = i, Total = i * 2 };
        }
    }

    // Unbox, mutate the copy, box the result again: one allocation per update
    [Benchmark(Baseline = true)]
    public long UnboxCopyRebox()
    {
        var total = 0L;
        for (var i = 0; i < boxes.Length; i++)
        {
            var value = (BoxedCounter)boxes[i];
            value.Count++;
            value.Total += 2;
            boxes[i] = value;
            total += value.Count;
        }

        return total;
    }

    // Take a ref straight into the existing box: no allocation, and the identity of the box is preserved
    [Benchmark]
    public long UnsafeUnboxInPlace()
    {
        var total = 0L;
        for (var i = 0; i < boxes.Length; i++)
        {
            ref var value = ref Unsafe.Unbox<BoxedCounter>(boxes[i]);
            value.Count++;
            value.Total += 2;
            total += value.Count;
        }

        return total;
    }

    public static void Verify()
    {
        var copyBenchmark = new UnboxInPlaceBenchmark();
        copyBenchmark.Setup();
        var expected = copyBenchmark.UnboxCopyRebox();

        var inPlaceBenchmark = new UnboxInPlaceBenchmark();
        inPlaceBenchmark.Setup();
        if (inPlaceBenchmark.UnsafeUnboxInPlace() != expected)
        {
            throw new InvalidOperationException("Verify failed. UnboxInPlace sum.");
        }

        // The in place form must not have replaced the box instances
        var probe = new UnboxInPlaceBenchmark();
        probe.Setup();
        var original = probe.boxes[0];
        probe.UnsafeUnboxInPlace();
        if (!ReferenceEquals(original, probe.boxes[0]))
        {
            throw new InvalidOperationException("Verify failed. UnboxInPlace replaced the box.");
        }
    }
}

internal struct BoxedCounter
{
    public long Count;

    public long Total;
}
