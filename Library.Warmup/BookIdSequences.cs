namespace Library.Warmup;

public static class BookIdSequences
{
    public static IEnumerable<int> OddBookIds(int from = 0, int to = 100)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(from);
        ArgumentOutOfRangeException.ThrowIfLessThan(to, from);

        return Enumerate(int.IsOddInteger(from) ? from : from + 1, to);

        static IEnumerable<int> Enumerate(int first, int last)
        {
            for (var id = first; id <= last; id += 2)
            {
                yield return id;
            }
        }
    }
}
