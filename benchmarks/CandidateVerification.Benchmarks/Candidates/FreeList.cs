namespace CandidateVerification.Benchmarks.Candidates;

// C-02 candidate: tombstone-based handler list (remove = null slot, read = span over the live array)
public sealed class FreeList<T>
    where T : class
{
#if NET9_0_OR_GREATER
    private readonly Lock gate = new();
#else
    private readonly object gate = new();
#endif

    private T?[] values;

    private int lastIndex = -1;

    public FreeList(int initialCapacity)
    {
        values = new T?[initialCapacity];
    }

    // Lock-free read: the publish loop iterates the live array and skips nulls
    public ReadOnlySpan<T?> AsSpan() => new(values, 0, lastIndex + 1);

    public void Add(T item)
    {
        lock (gate)
        {
            var index = Array.IndexOf(values, null, 0, lastIndex + 1);
            if (index >= 0)
            {
                values[index] = item;
                return;
            }

            if (lastIndex + 1 == values.Length)
            {
                var next = new T?[values.Length + (values.Length >> 1)];
                values.AsSpan().CopyTo(next);
                values = next;
            }

            lastIndex++;
            values[lastIndex] = item;
        }
    }

    public bool Remove(T item)
    {
        lock (gate)
        {
            var index = Array.IndexOf(values, item, 0, lastIndex + 1);
            if (index < 0)
            {
                return false;
            }

            values[index] = null;
            if (index == lastIndex)
            {
                while ((lastIndex >= 0) && (values[lastIndex] is null))
                {
                    lastIndex--;
                }
            }

            return true;
        }
    }
}
