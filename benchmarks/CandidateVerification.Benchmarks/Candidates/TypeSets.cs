namespace CandidateVerification.Benchmarks.Candidates;

using System.Text;
using System.Text.RegularExpressions;

// Shared probe sets for the Type-keyed lookup benchmarks (C-03 / C-12)
public static class TypeSets
{
    public static readonly Type[] Hit =
    [
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(float), typeof(double), typeof(decimal), typeof(bool),
        typeof(char), typeof(string), typeof(object), typeof(DateTime),
        typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid), typeof(Uri),
        typeof(Version), typeof(int[]), typeof(string[]), typeof(List<int>),
        typeof(Dictionary<int, int>), typeof(HashSet<int>), typeof(Queue<int>), typeof(Stack<int>),
        typeof(Task), typeof(ValueTask), typeof(StringBuilder), typeof(Exception)
    ];

    public static readonly Type[] Miss =
    [
        typeof(Random), typeof(Regex), typeof(Array), typeof(Enum),
        typeof(Delegate), typeof(Attribute), typeof(Convert), typeof(GC)
    ];
}
