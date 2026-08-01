namespace PerformancePatterns.Dsp;

/// <summary>
/// DSP-03: 購読者を不変配列で保持し、発火は <see cref="Volatile"/> 読み + foreach で行う。
/// マルチキャストデリゲート(<c>+=</c>)が購読者 2 個以上で急激に劣化するのを回避する。
/// 購読変更は copy-on-write のため、発火側にロックは不要。
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
        // スナップショットを 1 回読むだけ。発火中の購読変更にも影響されない
        foreach (var handler in Volatile.Read(ref handlers))
        {
            handler(value);
        }
    }
}
