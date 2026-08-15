namespace CandidateVerification.Benchmarks.Candidates;

using System.Collections.Concurrent;

// C-01 baseline: single shared ConcurrentQueue (plain BUF-07 shape)
public static class QueueOnlyPool<T>
    where T : class, new()
{
    private static readonly ConcurrentQueue<T> Items = new();

    public static T Rent() => Items.TryDequeue(out var value) ? value : new T();

    public static void Return(T value) => Items.Enqueue(value);
}

// C-01 variant: ThreadStatic slot only (sufficient when rent and return happen on the same thread, no nesting)
public static class ThreadStaticOnlyPool<T>
    where T : class, new()
{
    [ThreadStatic]
    private static T? item;

    public static T Rent()
    {
        var value = item;
        if (value is not null)
        {
            item = null;
            return value;
        }

        return new T();
    }

    public static void Return(T value) => item = value;
}

// C-01 variant: single CAS fast slot in front of the queue (VYaml ScalarPool shape)
public static class TwoTierPool<T>
    where T : class, new()
{
    private static readonly ConcurrentQueue<T> Items = new();

    private static T? fastItem;

    public static T Rent()
    {
        var value = fastItem;
        if ((value is not null) && (Interlocked.CompareExchange(ref fastItem, null, value) == value))
        {
            return value;
        }

        return Items.TryDequeue(out value) ? value : new T();
    }

    public static void Return(T value)
    {
        if ((fastItem is null) && (Interlocked.CompareExchange(ref fastItem, value, null) is null))
        {
            return;
        }

        Items.Enqueue(value);
    }
}

// C-01 candidate: ThreadStatic slot -> CAS fast slot -> ConcurrentQueue (VitalRouter ContextPool shape)
public static class ThreeTierPool<T>
    where T : class, new()
{
    private static readonly ConcurrentQueue<T> Items = new();

    [ThreadStatic]
    private static T? threadStaticItem;

    private static T? fastItem;

    public static T Rent()
    {
        var value = threadStaticItem;
        if (value is not null)
        {
            threadStaticItem = null;
            return value;
        }

        value = fastItem;
        if ((value is not null) && (Interlocked.CompareExchange(ref fastItem, null, value) == value))
        {
            return value;
        }

        return Items.TryDequeue(out value) ? value : new T();
    }

    public static void Return(T value)
    {
        if (threadStaticItem is null)
        {
            threadStaticItem = value;
            return;
        }

        if ((fastItem is null) && (Interlocked.CompareExchange(ref fastItem, value, null) is null))
        {
            return;
        }

        Items.Enqueue(value);
    }
}
