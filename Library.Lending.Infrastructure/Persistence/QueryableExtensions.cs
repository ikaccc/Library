using Library.Lending.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Library.Lending.Infrastructure.Persistence;

internal static class QueryableExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new PageRow<T>(item, query.Count()))
            .ToListAsync(cancellationToken);

        var totalCount = rows.Count > 0
            ? rows[0].TotalCount
            : await query.CountAsync(cancellationToken);

        return new PagedResult<T>(rows.Select(row => row.Item).ToList(), page, pageSize, totalCount);
    }

    private sealed record PageRow<T>(T Item, int TotalCount);
}

internal static class LikePatterns
{
    public const string EscapeCharacter = "\\";

    public static string Contains(string value) =>
        "%" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
                   .Replace("%", "\\%", StringComparison.Ordinal)
                   .Replace("_", "\\_", StringComparison.Ordinal) + "%";
}
