namespace Library.Lending.Application.Common;

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}

public static class Ranking
{
    public const int DefaultTop = 10;
    public const int MaxTop = 100;
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasNextPage => Page < TotalPages;

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new(Items.Select(selector).ToList(), Page, PageSize, TotalCount);
}
