namespace PerformancePatterns.Benchmarks.Lab;

using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー③: byte 列の int 化定数比較
// 問い: 4 バイトの ASCII トークン(HTTP メソッド等)の判定は、
// string 化 / SequenceEqual("..."u8) / uint 定数 1 比較のどれが速いか。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class TokenMatchBenchmark
{
    // static readonly は Tier1 で JIT 定数扱いになる(手書き 16 進定数のミスも防げる)
    private static readonly uint GetToken = BinaryPrimitives.ReadUInt32LittleEndian("GET "u8);

    private static readonly uint PostToken = BinaryPrimitives.ReadUInt32LittleEndian("POST"u8);

    private static readonly uint PutToken = BinaryPrimitives.ReadUInt32LittleEndian("PUT "u8);

    private static readonly uint HeadToken = BinaryPrimitives.ReadUInt32LittleEndian("HEAD"u8);

    private byte[][] probes = default!;

    [GlobalSetup]
    public void Setup()
    {
        // 実行時生成(データセクションの u8 リテラル参照をそのまま渡さない)
        var methods = new[] { "GET ", "POST", "PUT ", "HEAD" };
        probes = new byte[64][];
        for (var i = 0; i < probes.Length; i++)
        {
            probes[i] = Encoding.ASCII.GetBytes(new string(methods[i % methods.Length].AsSpan()));
        }
    }

    [Benchmark(Baseline = true)]
    public int StringSwitch()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            total += MatchString(probe);
        }

        return total;
    }

    [Benchmark]
    public int SequenceEqualChain()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            total += MatchSequenceEqual(probe);
        }

        return total;
    }

    [Benchmark]
    public int UIntConstantCompare()
    {
        var total = 0;
        foreach (var probe in probes)
        {
            total += MatchUInt(probe);
        }

        return total;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MatchString(ReadOnlySpan<byte> span)
        => Encoding.ASCII.GetString(span) switch
        {
            "GET " => 1,
            "POST" => 2,
            "PUT " => 3,
            "HEAD" => 4,
            _ => 0,
        };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MatchSequenceEqual(ReadOnlySpan<byte> span)
    {
        if (span.SequenceEqual("GET "u8))
        {
            return 1;
        }

        if (span.SequenceEqual("POST"u8))
        {
            return 2;
        }

        if (span.SequenceEqual("PUT "u8))
        {
            return 3;
        }

        if (span.SequenceEqual("HEAD"u8))
        {
            return 4;
        }

        return 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MatchUInt(ReadOnlySpan<byte> span)
    {
        if (span.Length != 4)
        {
            return 0;
        }

        var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
        if (value == GetToken)
        {
            return 1;
        }

        if (value == PostToken)
        {
            return 2;
        }

        if (value == PutToken)
        {
            return 3;
        }

        if (value == HeadToken)
        {
            return 4;
        }

        return 0;
    }
}
