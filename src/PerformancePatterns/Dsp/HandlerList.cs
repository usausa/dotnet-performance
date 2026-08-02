namespace PerformancePatterns.Dsp;

/// <summary>
/// DSP-03: Holds subscribers in an immutable array; raising reads it via <see cref="Volatile"/> and iterates with foreach.
/// Avoids the sharp degradation a multicast delegate (<c>+=</c>) suffers once there are two or more subscribers.
/// Subscription changes are copy-on-write, so the raising side needs no lock.
/// </summary>
public sealed class HandlerList<T>
{
#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    private Action<T>[] handlers = [];

    public int Count => Volatile.Read(ref handlers).Length;

    public void Add(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (sync)
        {
            var current = handlers;
            var next = new Action<T>[current.Length + 1];
            current.AsSpan().CopyTo(next);
            next[^1] = handler;
            Volatile.Write(ref handlers, next);
        }
    }

    public bool Remove(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        lock (sync)
        {
            var current = handlers;
            var index = Array.IndexOf(current, handler);
            if (index < 0)
            {
                return false;
            }

            var next = new Action<T>[current.Length - 1];
            current.AsSpan(0, index).CopyTo(next);
            current.AsSpan(index + 1).CopyTo(next.AsSpan(index));
            Volatile.Write(ref handlers, next);
            return true;
        }
    }

    public void Publish(T value)
    {
        // Read the snapshot once; subscription changes during the raise cannot affect it
        foreach (var handler in Volatile.Read(ref handlers))
        {
            handler(value);
        }
    }
}
