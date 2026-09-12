using FluentValidation;
using Library.Lending.Application.Abstractions.Analytics;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Books;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Analytics;

public sealed record GetAlsoBorrowedBooksQuery(Guid BookId, int Top) : IQuery<Result<IReadOnlyList<AlsoBorrowedBook>>>;

internal sealed class GetAlsoBorrowedBooksValidator : AbstractValidator<GetAlsoBorrowedBooksQuery>
{
    public GetAlsoBorrowedBooksValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Top).InclusiveBetween(1, Ranking.MaxTop);
    }
}

internal sealed class GetAlsoBorrowedBooksHandler(IBookRepository books, IAnalyticsQueries analytics)
    : IQueryHandler<GetAlsoBorrowedBooksQuery, Result<IReadOnlyList<AlsoBorrowedBook>>>
{
    public async Task<Result<IReadOnlyList<AlsoBorrowedBook>>> HandleAsync(GetAlsoBorrowedBooksQuery query, CancellationToken cancellationToken)
    {
        if (!await books.ExistsAsync(query.BookId, cancellationToken))
        {
            return BookErrors.NotFound(query.BookId);
        }

        var alsoBorrowed = await analytics.GetAlsoBorrowedBooksAsync(query.BookId, query.Top, cancellationToken);

        return Result.Success(alsoBorrowed);
    }
}
