namespace PerformancePatterns.Tests.Dsp;

using PerformancePatterns.Dsp;

using Xunit;

public sealed class HandlerListTest
{
    [Fact]
    public void PublishCallsAllHandlersInOrder()
    {
        var log = new List<string>();
        var list = new HandlerList<int>();
        list.Add(x => log.Add($"a{x}"));
        list.Add(x => log.Add($"b{x}"));
        list.Add(x => log.Add($"c{x}"));

        list.Publish(1);

        Assert.Equal(3, list.Count);
        Assert.Equal(["a1", "b1", "c1"], log);
    }

    [Fact]
    public void RemoveStopsNotification()
    {
        var count = 0;
        var list = new HandlerList<int>();
        void Handler(int x) => count++;

        list.Add(Handler);
        list.Publish(1);
        Assert.Equal(1, count);

        Assert.True(list.Remove(Handler));
        list.Publish(1);
        Assert.Equal(1, count);
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void RemoveUnknownHandlerReturnsFalse()
    {
        var list = new HandlerList<int>();
        list.Add(static _ => { });

        Assert.False(list.Remove(static _ => { }));
        Assert.Equal(1, list.Count);
    }

    [Fact]
    public void RemoveKeepsRemainingOrder()
    {
        var log = new List<string>();
        var list = new HandlerList<int>();
        void First(int x) => log.Add("first");
        void Second(int x) => log.Add("second");
        void Third(int x) => log.Add("third");

        list.Add(First);
        list.Add(Second);
        list.Add(Third);
        Assert.True(list.Remove(Second));

        list.Publish(0);

        Assert.Equal(["first", "third"], log);
    }

    [Fact]
    public void PublishOnEmptyIsNoOp()
    {
        var list = new HandlerList<int>();
        list.Publish(1);

        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void SnapshotIsolatesPublishFromMutation()
    {
        var list = new HandlerList<int>();
        var count = 0;
        list.Add(_ =>
        {
            count++;
            // 発火中に購読を追加しても、この発火のスナップショットには影響しない
            list.Add(static _ => { });
        });

        list.Publish(0);

        Assert.Equal(1, count);
        Assert.Equal(2, list.Count);
    }
}
