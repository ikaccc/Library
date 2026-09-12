using FluentValidation;
using Library.Lending.Application.Abstractions.Messaging;
using Library.Lending.Application.Abstractions.Persistence;
using Library.Lending.Application.Common;
using Library.Lending.Domain.Common;

namespace Library.Lending.Application.Books;

public sealed record ListBooksQuery(string? Search, int Page, int PageSize) : IQuery<Result<PagedResult<BookDto>>>;

internal sealed class ListBooksValidator : AbstractValidator<ListBooksQuery>
{
    public ListBooksValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, Paging.MaxPageSize);
    }
}

internal sealed class ListBooksHandler(IBookRepository books) : IQueryHandler<ListBooksQuery, Result<PagedResult<BookDto>>>
{
    public async Task<Result<PagedResult<BookDto>>> HandleAsync(ListBooksQuery query, CancellationToken cancellationToken)
    {
        var page = await books.ListAsync(query.Search, query.Page, query.PageSize, cancellationToken);

        return page.Map(book => book.ToDto());
    }
}
