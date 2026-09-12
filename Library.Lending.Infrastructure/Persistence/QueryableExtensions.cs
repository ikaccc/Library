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
        var totalCount = await query.CountAsync(cancellationToken);
        var items = totalCount == 0
            ? []
            : await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}

internal static class LikePatterns
{
    public const string EscapeCharacter = "\\";

    public static string Contains(string value) =>
        "%" + value.Replace("\\", "\\\\", StringComparison.Ordinal)
                   .Replace("%", "\\%", StringComparison.Ordinal)
                   .Replace("_", "\\_", StringComparison.Ordinal) + "%";
}
