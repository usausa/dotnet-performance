namespace PerformancePatterns.Benchmarks.Lab;

using System.Threading.Channels;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

// Study queue 5: System.Threading.Channels
// Question: what is the per-element cost as a producer-consumer queue, and what impact do the
// SingleReader/SingleWriter options and bounded channels have?
[Config(typeof(BenchmarkConfig))]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ChannelsBenchmark
{
    private const int N = 10_000;

    [Benchmark(Baseline = true, OperationsPerInvoke = N)]
    public Task<long> UnboundedDefault()
        => Pump(Channel.CreateUnbounded<int>());

    [Benchmark(OperationsPerInvoke = N)]
    public Task<long> UnboundedSingleReaderWriter()
        => Pump(Channel.CreateUnbounded<int>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        }));

    [Benchmark(OperationsPerInvoke = N)]
    public Task<long> Bounded()
        => Pump(Channel.CreateBounded<int>(new BoundedChannelOptions(128)
        {
            SingleReader = true,
            SingleWriter = true,
        }));

    private static async Task<long> Pump(Channel<int> channel)
    {
        var writerTask = Task.Run(async () =>
        {
            for (var i = 0; i < N; i++)
            {
                await channel.Writer.WriteAsync(i).ConfigureAwait(false);
            }

            channel.Writer.Complete();
        });

        var total = 0L;
        await foreach (var value in channel.Reader.ReadAllAsync().ConfigureAwait(false))
        {
            total += value;
        }

        await writerTask.ConfigureAwait(false);
        return total;
    }
}
