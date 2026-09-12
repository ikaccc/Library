namespace Library.Lending.Application.Common;

public sealed record TimeRange(DateTimeOffset? From, DateTimeOffset? To)
{
    public static TimeRange AllTime { get; } = new(null, null);

    public bool IsWellOrdered => From is null || To is null || From < To;
}
