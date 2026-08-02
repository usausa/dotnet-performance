namespace PerformancePatterns.Benchmarks.Lab;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using PerformancePatterns.Seq;

// 検証キュー⑤: ref フィールドによる ref struct 設計(C# 11)
// 問い: カーソルを「Span + インデックス」でなく「ref T + 終端 ref」で保持すると速くなるか。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RefFieldCursorBenchmark
{
    private int[] values = default!;

    [GlobalSetup]
    public void Setup()
    {
        values = new int[1024];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = i;
        }
    }

    [Benchmark(Baseline = true)]
    public int SumSpanIndex()
    {
        var span = values.AsSpan();
        var total = 0;
        for (var i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        return total;
    }

    [Benchmark]
    public int SumSpanCursor()
    {
        var reader = new SpanCursor<int>(values);
        var total = 0;
        while (reader.Remaining > 0)
        {
            total += reader.Read();
        }

        return total;
    }

    [Benchmark]
    public int SumRefFieldCursor()
    {
        var cursor = new RefFieldCursor<int>(values);
        var total = 0;
        while (cursor.TryRead(out var value))
        {
            total += value;
        }

        return total;
    }
}

// ref フィールド(C# 11)によるカーソル: 位置を ref そのもので保持する
internal ref struct RefFieldCursor<T>
{
    private readonly ref T end;

    private ref T current;

    public RefFieldCursor(ReadOnlySpan<T> span)
    {
        current = ref MemoryMarshal.GetReference(span);
        end = ref Unsafe.Add(ref current, span.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead(out T value)
    {
        if (Unsafe.IsAddressLessThan(ref current, ref end))
        {
            value = current;
            current = ref Unsafe.Add(ref current, 1);
            return true;
        }

        value = default!;
        return false;
    }
}

// Minimal Span + index cursor used only to measure the rejected "read one element at a time" shape (R-12).
internal ref struct SpanCursor<T>
    where T : unmanaged
{
    private readonly ReadOnlySpan<T> source;

    private int position;

    public SpanCursor(ReadOnlySpan<T> source)
    {
        this.source = source;
    }

    public readonly int Remaining => source.Length - position;

    public T Read() => source[position++];
}
