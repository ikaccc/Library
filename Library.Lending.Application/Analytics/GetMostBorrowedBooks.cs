using FluentValidation;
using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Analytics;

public sealed record GetMostBorrowedBooksQuery(TimeRange Range, int Top) : IQuery<Result<IReadOnlyList<MostBorrowedBook>>>;

internal sealed class GetMostBorrowedBooksValidator : AbstractValidator<GetMostBorrowedBooksQuery>
{
    public GetMostBorrowedBooksValidator()
    {
        RuleFor(x => x.Range).NotNull().BeWellOrdered();
        RuleFor(x => x.Top).InclusiveBetween(1, Ranking.MaxTop);
    }
}

internal sealed class GetMostBorrowedBooksHandler(IAnalyticsQueries analytics)
    : IQueryHandler<GetMostBorrowedBooksQuery, Result<IReadOnlyList<MostBorrowedBook>>>
{
    public async Task<Result<IReadOnlyList<MostBorrowedBook>>> HandleAsync(GetMostBorrowedBooksQuery query, CancellationToken cancellationToken)
    {
        var books = await analytics.GetMostBorrowedBooksAsync(query.Range, query.Top, cancellationToken);

        return Result.Success(books);
    }
}
