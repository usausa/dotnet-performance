namespace CandidateVerification.Benchmarks.Candidates;

using System.Buffers;

// C-10 candidate: builds a multi-segment ReadOnlySequence over ArrayPool chunks.
// Segment objects are reused across Reset cycles; backing arrays are returned to the pool.
public sealed class ReusableSequenceBuilder
{
    private readonly List<SequenceSegment> segments = [];

    private int count;

    public void Add(byte[] buffer, int length)
    {
        SequenceSegment segment;
        if (count < segments.Count)
        {
            segment = segments[count];
        }
        else
        {
            segment = new SequenceSegment();
            segments.Add(segment);
        }

        count++;

        segment.SetMemory(buffer, length);
        if (count > 1)
        {
            segments[count - 2].LinkNext(segment);
        }
    }

    public ReadOnlySequence<byte> Build()
    {
        if (count == 0)
        {
            return ReadOnlySequence<byte>.Empty;
        }

        var first = segments[0];
        var last = segments[count - 1];
        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    public void Reset()
    {
        for (var i = 0; i < count; i++)
        {
            segments[i].Release();
        }

        count = 0;
    }

    private sealed class SequenceSegment : ReadOnlySequenceSegment<byte>
    {
        private byte[]? buffer;

        public void SetMemory(byte[] rented, int length)
        {
            buffer = rented;
            Memory = new ReadOnlyMemory<byte>(rented, 0, length);
            RunningIndex = 0;
            Next = null;
        }

        public void LinkNext(SequenceSegment next)
        {
            Next = next;
            next.RunningIndex = RunningIndex + Memory.Length;
        }

        public void Release()
        {
            var toReturn = buffer;
            if (toReturn is not null)
            {
                buffer = null;
                ArrayPool<byte>.Shared.Return(toReturn);
            }

            Memory = default;
            Next = null;
        }
    }
}
