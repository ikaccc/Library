namespace Library.Lending.Domain.Loans;

public readonly record struct ReadingPace
{
    public const double MinimumDays = 1d;

    private ReadingPace(int pageCount, double days)
    {
        PageCount = pageCount;
        Days = days;
    }

    public int PageCount { get; }

    public double Days { get; }

    public double PagesPerDay => PageCount / Days;

    public static ReadingPace ForLoan(int pageCount, DateTimeOffset borrowedAt, DateTimeOffset returnedAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageCount);
        if (returnedAt < borrowedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(returnedAt), returnedAt, "A loan cannot be returned before it was borrowed.");
        }

        var days = Math.Max(MinimumDays, (returnedAt - borrowedAt).TotalDays);
        return new ReadingPace(pageCount, days);
    }

    public static double? Overall(IReadOnlyCollection<ReadingPace> paces)
    {
        ArgumentNullException.ThrowIfNull(paces);

        if (paces.Count == 0)
        {
            return null;
        }

        return paces.Sum(pace => pace.PageCount) / paces.Sum(pace => pace.Days);
    }
}
