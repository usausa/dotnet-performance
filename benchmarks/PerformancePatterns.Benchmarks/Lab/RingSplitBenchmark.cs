namespace PerformancePatterns.Benchmarks.Lab;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// SEQ-05 検証: ストリーミング受信の行分割
// 素朴な「毎回全域再走査 + 行ごとに前方詰め」と、「増分探索 + 遅延コンパクション」を比較する。
// (リングの折り返し 2 セグメント処理は未測定 — フラットバッファ形で増分探索の効果のみを測る)
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class RingSplitBenchmark
{
    private const byte Delimiter = (byte)'\n';

    private const int LineLength = 2048;

    private const int LineCount = 16;

    private const int ChunkSize = 256;

    private byte[] source = default!;

    private byte[] buffer = default!;

    [GlobalSetup]
    public void Setup()
    {
        // 2 KB 行 × 16 行(行が多数チャンクにまたがる = 再走査コストが顕在化する形)
        source = new byte[LineLength * LineCount];
        for (var i = 0; i < LineCount; i++)
        {
            var line = source.AsSpan(i * LineLength, LineLength);
            line.Fill((byte)('a' + (i % 26)));
            line[^1] = Delimiter;
        }

        buffer = new byte[LineLength * 2];
    }

    [Benchmark(Baseline = true)]
    public long NaiveRescanCompact()
    {
        var count = 0;
        var total = 0L;
        var offset = 0;
        while (offset < source.Length)
        {
            var chunkLength = Math.Min(ChunkSize, source.Length - offset);
            source.AsSpan(offset, chunkLength).CopyTo(buffer.AsSpan(count));
            count += chunkLength;
            offset += chunkLength;

            // 毎回先頭から全域を再走査する
            int index;
            while ((index = buffer.AsSpan(0, count).IndexOf(Delimiter)) >= 0)
            {
                total += buffer[0] + (long)index;

                // 行ごとに残りを前方へ詰める
                buffer.AsSpan(index + 1, count - index - 1).CopyTo(buffer);
                count -= index + 1;
            }
        }

        return total;
    }

    [Benchmark]
    public long IncrementalDeferredCompact()
    {
        var start = 0;
        var count = 0;
        var search = 0;
        var total = 0L;
        var offset = 0;
        while (offset < source.Length)
        {
            var chunkLength = Math.Min(ChunkSize, source.Length - offset);

            // 空きが足りないときだけ前方へ詰める(遅延コンパクション)
            if (count + chunkLength > buffer.Length)
            {
                buffer.AsSpan(start, count - start).CopyTo(buffer);
                count -= start;
                search -= start;
                start = 0;
            }

            source.AsSpan(offset, chunkLength).CopyTo(buffer.AsSpan(count));
            count += chunkLength;
            offset += chunkLength;

            // 前回走査済みの位置から先だけを見る(増分探索)
            while (true)
            {
                var span = buffer.AsSpan(search, count - search);
                var index = span.IndexOf(Delimiter);
                if (index < 0)
                {
                    search = count;
                    break;
                }

                var lineStart = start;
                var lineEnd = search + index;
                total += buffer[lineStart] + (long)(lineEnd - lineStart);

                start = lineEnd + 1;
                search = start;
            }
        }

        return total;
    }
}
