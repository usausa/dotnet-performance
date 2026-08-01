namespace PerformancePatterns.Benchmarks.Lab;

using System.IO.Pipelines;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// 検証キュー⑤: System.IO.Pipelines
// 問い: PipeWriter/PipeReader を介したデータ受け渡しの固定コストはどれくらいか
// (同スレッド・同期完了の 4KB × 16 チャンク転送で MemoryStream と比較)。
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class PipelinesBenchmark
{
    private const int ChunkSize = 4096;

    private const int ChunkCount = 16;

    private byte[] chunk = default!;

    [GlobalSetup]
    public void Setup()
    {
        chunk = new byte[ChunkSize];
        for (var i = 0; i < chunk.Length; i++)
        {
            chunk[i] = (byte)i;
        }
    }

    [Benchmark(Baseline = true)]
    public long MemoryStreamPump()
    {
        using var stream = new MemoryStream();
        for (var i = 0; i < ChunkCount; i++)
        {
            stream.Write(chunk, 0, chunk.Length);
        }

        stream.Position = 0;
        var total = 0L;
        var buffer = new byte[ChunkSize];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            total += read;
        }

        return total;
    }

    [Benchmark]
    public async Task<long> PipePump()
    {
        var pipe = new Pipe();

        // 既定 PauseWriterThreshold(64KB)に達すると FlushAsync が読み手の消費を待つため、
        // 書き手と読み手は必ず並行させる(「書き切ってから読む」逐次構造はデッドロックする)
        var writerTask = Task.Run(async () =>
        {
            for (var i = 0; i < ChunkCount; i++)
            {
                var span = pipe.Writer.GetSpan(ChunkSize);
                chunk.CopyTo(span);
                pipe.Writer.Advance(ChunkSize);
                await pipe.Writer.FlushAsync().ConfigureAwait(false);
            }

            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
        });

        var total = 0L;
        while (true)
        {
            var result = await pipe.Reader.ReadAsync().ConfigureAwait(false);
            total += result.Buffer.Length;
            pipe.Reader.AdvanceTo(result.Buffer.End);
            if (result.IsCompleted)
            {
                break;
            }
        }

        await pipe.Reader.CompleteAsync().ConfigureAwait(false);
        await writerTask.ConfigureAwait(false);
        return total;
    }
}
