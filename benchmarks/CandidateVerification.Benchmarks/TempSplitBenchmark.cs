namespace CandidateVerification.Benchmarks;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

public enum TokenDataShape
{
    Fix,
    Wide,
}

// C-11 (JIT-04 update candidate): passing hot locals as out args to a NoInlining cold call
// makes them address-exposed for the whole method (all-or-nothing per variable) and pins them to the stack.
// temp-split isolates the exposure into cold-path temporaries; value/tokenSize stay enregistered.
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TempSplitBenchmark
{
    private const int TokenCount = 10_000;

    private byte[] data = default!;

    [Params(TokenDataShape.Fix, TokenDataShape.Wide)]
    public TokenDataShape Shape { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        data = BuildData(Shape);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = TokenCount)]
    public long OutParamShared()
    {
        var span = data.AsSpan();
        var position = 0;
        var sum = 0L;
        while (position < span.Length)
        {
            if (!TryReadShared(span[position..], out var value, out var tokenSize))
            {
                break;
            }

            sum += value;
            position += tokenSize;   // serial dependence chain: token size feeds the next token's address
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = TokenCount)]
    public long OutParamSplit()
    {
        var span = data.AsSpan();
        var position = 0;
        var sum = 0L;
        while (position < span.Length)
        {
            if (!TryReadSplit(span[position..], out var value, out var tokenSize))
            {
                break;
            }

            sum += value;
            position += tokenSize;
        }

        return sum;
    }

    public static void Verify()
    {
        foreach (var shape in new[] { TokenDataShape.Fix, TokenDataShape.Wide })
        {
            var benchmark = new TempSplitBenchmark { Shape = shape };
            benchmark.Setup();

            var shared = benchmark.OutParamShared();
            var split = benchmark.OutParamSplit();
            var reference = DecodeReference(benchmark.data);
            if ((shared != split) || (shared != reference))
            {
                throw new InvalidOperationException($"Decode results differ for {shape}: {shared} / {split} / {reference}.");
            }
        }
    }

    // Format: 0x00-0x7F = value in the header byte (1 byte) / 0xC8 = int32 (5 bytes) / 0xC9 = int64 (9 bytes, cold)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadShared(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        var header = source[0];
        if (header <= 0x7F)
        {
            value = header;
            tokenSize = 1;
            return true;
        }

        if (header == 0xC8)
        {
            value = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(1, 4));
            tokenSize = 5;
            return true;
        }

        if (header == 0xC9)
        {
            // Hot locals value/tokenSize get address-exposed here even though this branch almost never runs
            return ReadNineByteToken(source, out value, out tokenSize);
        }

        value = 0;
        tokenSize = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool TryReadSplit(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        var header = source[0];
        if (header <= 0x7F)
        {
            value = header;
            tokenSize = 1;
            return true;
        }

        if (header == 0xC8)
        {
            value = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(1, 4));
            tokenSize = 5;
            return true;
        }

        if (header == 0xC9)
        {
            // temp-split: address exposure is confined to nineValue/nineSize; the extra cost is 2 moves on the cold path
            var nineResult = ReadNineByteToken(source, out var nineValue, out var nineSize);
            value = nineValue;
            tokenSize = nineSize;
            return nineResult;
        }

        value = 0;
        tokenSize = 0;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ReadNineByteToken(ReadOnlySpan<byte> source, out int value, out int tokenSize)
    {
        value = (int)BinaryPrimitives.ReadInt64LittleEndian(source.Slice(1, 8));
        tokenSize = 9;
        return true;
    }

    private static long DecodeReference(ReadOnlySpan<byte> data)
    {
        var position = 0;
        var sum = 0L;
        while (position < data.Length)
        {
            var header = data[position];
            if (header <= 0x7F)
            {
                sum += header;
                position += 1;
            }
            else if (header == 0xC8)
            {
                sum += BinaryPrimitives.ReadInt32LittleEndian(data.Slice(position + 1, 4));
                position += 5;
            }
            else
            {
                sum += (int)BinaryPrimitives.ReadInt64LittleEndian(data.Slice(position + 1, 8));
                position += 9;
            }
        }

        return sum;
    }

    private static byte[] BuildData(TokenDataShape shape)
    {
        var bytes = new List<byte>(TokenCount * 3);
        var seed = 424242u;
        Span<byte> scratch = stackalloc byte[8];
        for (var i = 0; i < TokenCount; i++)
        {
            if (shape == TokenDataShape.Fix)
            {
                bytes.Add((byte)(NextRandom(ref seed) & 0x7F));
                continue;
            }

            var roll = NextRandom(ref seed) & 63u;
            if (roll == 0)
            {
                // Rare wide token: keeps the cold path realistic without dominating the loop
                bytes.Add(0xC9);
                BinaryPrimitives.WriteInt64LittleEndian(scratch, (int)NextRandom(ref seed));
                foreach (var b in scratch)
                {
                    bytes.Add(b);
                }
            }
            else if (roll < 24)
            {
                bytes.Add(0xC8);
                BinaryPrimitives.WriteInt32LittleEndian(scratch, (int)(NextRandom(ref seed) & 0xFFFFFF));
                foreach (var b in scratch[..4])
                {
                    bytes.Add(b);
                }
            }
            else
            {
                bytes.Add((byte)(NextRandom(ref seed) & 0x7F));
            }
        }

        return [.. bytes];
    }

    private static uint NextRandom(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return seed;
    }
}
